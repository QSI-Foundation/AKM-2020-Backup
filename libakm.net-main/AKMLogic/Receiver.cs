/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

using AKMCommon.Struct;
using AKMInterface;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace AKMLogic
{
	/// <summary>
	/// Provides functionality for receiving AKM Frames over network stream and extracts transmitted data 
	/// </summary>
	public sealed class Receiver
	{
		private const int BUFFER_SIZE = 1024;
		private readonly NetworkStream _stream;
		private Thread _thread;
		private readonly ILogger _logger;
		private readonly ICryptography _crypto;
		private AkmRelationship _akmRelationship;
		private readonly CancellationToken? _cancellationToken;
		private readonly Socket _socket;

		/// <summary>
		/// Node Number in Relationship
		/// </summary>
		public int NodeNumber { get; set; }

		/// <summary>
		/// This event will be fired when data received in AKM Frame is ready for client
		/// </summary>
		public event EventHandler<AkmDataReceivedEventArgs> DataReceived;

		/// <summary>
		/// Simplified contructor that uses default ICryptography implementation provided by AKMCrypto class.
		/// </summary>
		/// <param name="socket">Socket from which data is received</param>
		/// <param name="token">Token for cancelling operation when client shuts down</param>
		/// <param name="logger">ILogger implementation</param>
		/// <param name="cryptography">cryptography implementation</param>
		public Receiver(Socket socket, CancellationToken token, ILogger logger, ICryptography cryptography)
		{
			_socket = socket;
			_stream = new NetworkStream(socket);
			_logger = logger;
			_crypto = cryptography;
			_cancellationToken = token;
		}

		/// <summary>
		/// Simplified contructor that uses default ICryptography implementation provided by AKMCrypto class.
		/// </summary>
		/// <param name="socket">Socket from which data is received</param>
		/// <param name="token">Token for cancelling operation when client shuts down</param>
		/// <param name="logger">ILogger implementation</param>
		public Receiver(Socket socket, CancellationToken token, ILogger logger) : this(socket, token, logger, new AkmCrypto())
		{

		}

		/// <summary>
		/// Start new thread responsible for checking network stream for new data and processing it
		/// </summary>
		public void StartReceiving()
		{
			if (_stream != null)
			{
				_thread = new Thread(Run);
				_thread.Start(_cancellationToken);
			}
		}
		/// <summary>
		/// Starts the receiver process with option to pass CancelationToken
		/// </summary>
		/// <param name="token"></param>
		public void StartReceiving(CancellationToken token)
		{
			if (_stream != null)
			{
				_thread = new Thread(Run);
				_thread.Start(token);
			}
		}

		private bool IsConnectionAlive()
		{
			if (_socket != null)
			{
				var socketError = _socket.Poll(1, SelectMode.SelectError);
				return !socketError;
			}
			return false;
		}


		private void Run(object token)
		{
			var frameCounter = 0;
			CancellationToken? ct = (CancellationToken)token;
			try
			{
				byte[] inputBuffer = new byte[BUFFER_SIZE];
				var frame = new List<byte>();
				int readbytes = 0;
				long dataCollected = 0;
				while (!(ct?.IsCancellationRequested ?? false) && IsConnectionAlive())
				{
					try
					{

						readbytes = 0;
						dataCollected = 0;
						while (!((ct?.IsCancellationRequested ?? false) || _stream.DataAvailable))
						{
							Thread.Sleep(5);
						}
						if (ct?.IsCancellationRequested ?? false)
							break;

						//let's try to get the frame size first - 2 bytes for RelationshipId and 8 bytes for ulong with data size
						var header = new byte[10];
						short relationShipId = 0;
						long dataSize = 0;
						var size = _stream.Read(header, 0, 10);
						int bufferReadSize = 0;
						relationShipId = BitConverter.ToInt16(header.Take(2).ToArray());
						dataSize = BitConverter.ToInt64(header.TakeLast(8).ToArray());
						_akmRelationship = AkmSetup.AkmRelationships[relationShipId];
						short _SrcAddr = 0;
						short _TrgAddr = 0;
						AKMCommon.Enum.AkmEvent _AkmEvent;

						frame.AddRange(header.Take(2));

						while (dataCollected < dataSize)
						{
							bufferReadSize = (int)Math.Min((dataSize - dataCollected), BUFFER_SIZE);

							if ((readbytes = _socket.Receive(inputBuffer, 0, bufferReadSize, SocketFlags.None)) > 0)
							{
								frame.AddRange(inputBuffer.Take(readbytes));
								dataCollected += Convert.ToInt64(readbytes);

								if (dataCollected == dataSize)
								{
									frameCounter++;
									//_logger.LogDebug("Data Received event fired");
									DataReceived(null, new AkmDataReceivedEventArgs
									{
										FrameData = ExtractContentFromAkmFrame(frame.ToArray(), relationShipId, out _SrcAddr, out _TrgAddr, out _AkmEvent),
										RelationshipId = relationShipId,
										SrcAddr = _SrcAddr,
										TrgAddr = _TrgAddr,
										AkmEvent = _AkmEvent
									});
									frame.Clear();
								}

								Array.Clear(inputBuffer, 0, BUFFER_SIZE);
							}
							else //wait for the rest of the data to appear in the stream
							{
								Thread.Sleep(5);
							}
						}
					}
					catch (IOException ex)
					{
						_logger.LogError($"IO Error in Receiver: {ex.Message}");
						if (_stream != null)
						{
							_stream.Close();
						}

					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"General Error in Receiver: {ex.Message}");
			}
			finally
			{
				if (_stream != null)
					_stream.Close();
			}
		}

		private byte[] ExtractContentFromAkmFrame(byte[] akmFrame, short relationshipId, out short SrcAddr, out short TrgAddr, out AKMCommon.Enum.AkmEvent FrameEvent)
		{
			SrcAddr = 0;
			TrgAddr = 0;
			FrameEvent = AKMCommon.Enum.AkmEvent.None;
			try
			{
				var encFrame = new AkmEncryptedFrame(_crypto, _logger, akmFrame, 1);
				_akmRelationship = AkmSetup.AkmRelationships[relationshipId];

				var akmStatus = _akmRelationship.ProcessFrame(encFrame, out IDecryptedFrame decFrame);
				//_logger.LogDebug($"Frame processing status: {akmStatus} ");
				if (_akmRelationship.IsConfigurationUpdated)
					AkmSetup.UpdateAkmConfiguration(relationshipId);
				SrcAddr = decFrame.GetSourceAddressAsShort();
				TrgAddr = decFrame.GetTargetAddressAsShort();
				FrameEvent = decFrame.FrameEvent;
				return decFrame?.GetContentBytes();
			}
			catch (Exception ex)
			{
				_logger.LogError("Error in AKM Frame Receive Processing: " + ex.Message);
				return null;
			}
		}
	}
}
