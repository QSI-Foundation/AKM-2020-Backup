/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

using AKMCommon.Enum;
using AKMInterface;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace AKMLogic
{
	/// <summary>
	/// Provides functionality for sending data on specified network stream using AKM Frames
	/// </summary>
	public sealed class Sender
	{
		private const int BUFFER_SIZE = 1024;

		private NetworkStream _stream;
		private readonly Thread _thread;
		private readonly IList<byte[]> _dataBuffer;
		private readonly object _locker = new object();
		private CancellationToken _cancellationToken;
		private ILogger _logger;
		private ICryptography _crypto;
		private readonly short _relationshipId;
		private AkmRelationship _akmRelationship;

		private Socket _socket;

		/// <summary>
		/// Determine if all required components are provided and defined for proper work
		/// </summary>
		public bool IsInitialized
		{
			get
			{
				return (_stream != null) && (_logger != null) && (_crypto != null);
			}
		}

		/// <summary>
		/// Returns value indicating if the sending process is still active
		/// </summary>
		public bool IsActive
		{
			get { return _thread != null && _thread.IsAlive && IsConnectionAlive; }
		}

		/// <summary>
		/// Returns valie indicating if the Relationship group is complete and active
		/// </summary>
		public bool IsRelationshipActive
		{
			get { return AkmRelationship?.IsActive ?? false; }
		}

		private AkmRelationship AkmRelationship
		{
			get
			{
				if (_akmRelationship == null)
				{
					_akmRelationship = AkmSetup.AkmRelationships[_relationshipId];
				}
				return _akmRelationship;
			}
		}

		internal Sender(short relationshipId)
		{
			_relationshipId = relationshipId;
			_dataBuffer = new List<byte[]>(1);
			_crypto = new AkmCrypto();
			_thread = new Thread(Run);
			_thread.Start();
		}

		/// <summary>
		/// Sets Socket object for network communication
		/// </summary>
		/// <param name="socket"></param>
		public void SetSocket(Socket socket)
		{
			_socket = socket;
			_stream = new NetworkStream(socket);
		}

		/// <summary>
		/// Sets cryptography provider
		/// </summary>
		/// <param name="cryptography">Object that implements the ICryptography interface</param>
		public void SetCryptography(ICryptography cryptography = null)
		{
			_crypto = cryptography ?? new AkmCrypto();
		}

		/// <summary>
		/// Sets logger object that will be used
		/// </summary>
		/// <param name="logger">Object that implements ILogger interface</param>
		public void SetLogger(ILogger logger)
		{
			_logger = logger;
		}

		internal void SetCanellationToken(CancellationToken token)
		{
			_cancellationToken = token;
		}

		private bool IsConnectionAlive
		{
			get
			{
				if (_socket != null)
				{
					var socketWriteable = _socket.Poll(1, SelectMode.SelectWrite);
					var socketError = _socket.Poll(1, SelectMode.SelectError);

					return socketWriteable && !socketError;
				}
				return false;
			}
		}

		/// <summary>
		/// Used for sending raw data package, no AKM processing
		/// </summary>
		/// <param name="rawData"></param>
		public void SendRawData(byte[] rawData)
		{
			lock (_locker)
			{
				_dataBuffer.Add(rawData);
			}
		}

		/// <summary>
		/// Starts sedner thread
		/// </summary>
		private void Run()
		{
			while (!_cancellationToken.IsCancellationRequested)
			{
				var chunkStart = 0;
				int chunkLength;

				while ((_dataBuffer.Count) == 0 && !_cancellationToken.IsCancellationRequested)
				{
					Thread.Sleep(5);
				}
				if (_cancellationToken.IsCancellationRequested)
					break;

				if (IsConnectionAlive && _stream != null && _stream.CanWrite)
				{
					lock (_locker)
					{
						byte[] data = _dataBuffer.FirstOrDefault();
						try
						{
							while (chunkStart < data.Length)
							{
								chunkLength = Math.Min(BUFFER_SIZE, data.Length - chunkStart);
								_socket.Send(data.AsSpan(chunkStart, chunkLength).ToArray());

								chunkStart += chunkLength;
							}
						}
						catch (SocketException sockEx)
						{
							_logger.LogError($"Unable to connect to remote address. {sockEx}");
							return;
						}
						catch (System.IO.IOException ioEx)
						{
							_logger.LogError($"Unable to connect to remote address. {ioEx}");
							return;
						}
						finally
						{
							_dataBuffer.Remove(data);
							data = null;
						}
					}
				}
				else
				{
					_logger.LogError("Unable to connect to remote address. Sender process will now close.");
					_stream.Dispose();
					if (_socket.Connected)
					{
						_socket.Shutdown(SocketShutdown.Both);
						_socket.Disconnect(true);
					}
					_socket.Dispose();
					return;

				}
				//Thread.Sleep(5);
			}
		}

		/// <summary>
		/// Prepares a decrypted AKM frame based on provided content and target address
		/// </summary>
		/// <param name="content">Byte array with data that needs to be sent</param>
		/// <param name="targetAddress">byte array with destination address value</param>
		/// <returns></returns>
		private IDecryptedFrame PrepareFrame(byte[] content, byte[] targetAddress)
		{
			var decFrame = new AkmDecryptedFrame(_crypto, _logger, 1);

			var messageFrame = new byte[content.Length + 5];
			var sourceAddr = BitConverter.GetBytes(AkmSetup.AkmAppCfg[_relationshipId].SelfAddressValue);

			Array.Copy(sourceAddr, 0, messageFrame, 0, sourceAddr.Length);
			Array.Copy(targetAddress, 0, messageFrame, sourceAddr.Length, targetAddress.Length);

			Array.Copy(content, 0, messageFrame, 5, content.Length);

			decFrame.SetData(messageFrame);
			return decFrame;
		}
	}
}
