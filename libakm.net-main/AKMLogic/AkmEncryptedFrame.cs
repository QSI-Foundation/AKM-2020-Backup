/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

using AKMCommon.Enum;
using AKMCommon.Error;
using AKMInterface;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace AKMLogic
{
	/// <summary>
	/// Default AKM Endcrypted frame implementation
	/// </summary>
	public class AkmEncryptedFrame : IEncryptedFrame
	{
		private readonly ICryptography _crypto;
		private readonly ILogger _logger;
		private byte[] _frameBytes;
		/// <inheritdoc/>
		public short RelationshipId { get; }

		/// <summary>
		/// Constructor accepting ICryptography implementation, ILogger implementation and relationship Id value
		/// </summary>
		/// <param name="cryptographyProvider">Object implementing ICryptography interface</param>
		/// <param name="logger">Object implementing ILogger interface</param>
		/// <param name="relationshipId">Relationship Id numeric value</param>
		public AkmEncryptedFrame(ICryptography cryptographyProvider, ILogger logger, short relationshipId)
		{
			_crypto = cryptographyProvider;
			_logger = logger;
			RelationshipId = relationshipId;
		}

		/// <summary>
		/// Constructor 
		/// </summary>
		/// <param name="cryptographyProvider">Object implementing ICryptography interface</param>
		/// <param name="logger">Object implementing ILogger interface</param>
		/// <param name="encryptedData">byte array holding encrypted part of AKM frame</param>
		/// <param name="relationshipId">Relationship Id numeric value</param>
		public AkmEncryptedFrame(ICryptography cryptographyProvider, ILogger logger, byte[] encryptedData, short relationshipId)
		{
			_crypto = cryptographyProvider;
			_logger = logger;
			_frameBytes = encryptedData;
			RelationshipId = relationshipId;
		}
		/// <inheritdoc/>
		public byte[] GetEncryptedData()
		{
			return _frameBytes.Skip(AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.RelationshipId_Length).ToArray();
		}

		/// <inheritdoc/>
		public byte[] GetTransmissionData()
		{
			return _frameBytes;
		}
		/// <inheritdoc/>
		public IDecryptedFrame Decrypt(IKey key)
		{
			try
			{
				var decryptedMessage = _crypto.Decrypt(_frameBytes.AsSpan(AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.RelationshipId_Length).ToArray(), key.KeyAsBytes);

				var result = new AkmDecryptedFrame(_crypto, _logger, RelationshipId);
				result.SetData(decryptedMessage);

				if (!result.CheckHash())
				{
					throw new AkmError(AkmStatus.FatalError);
				}
				return result;
			}
			catch
			{
				return null;
			}
		}

		/// <inheritdoc/>
		public void SetEncryptedData(byte[] data)
		{
			var _relIdBytes = BitConverter.GetBytes(RelationshipId);
			_frameBytes = new byte[data.Length + 2];
			Array.Copy(_relIdBytes, 0, _frameBytes, AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.RelationshipId_Index, AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.RelationshipId_Length);
			Array.Copy(data, 0, _frameBytes, AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.RelationshipId_Length, data.Length);
		}

		/// <summary>
		/// Sets AKM frame length in the header
		/// </summary>
		public void SetFrameLength()
		{
			var currentLength = _frameBytes.LongLength - AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.RelationshipId_Length; //actual data length
			var newFrameBytes = new byte[_frameBytes.Length + 8]; //add a bit for int64 value

			Array.Copy(_frameBytes,
						AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.RelationshipId_Index,
						newFrameBytes,
						AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.RelationshipId_Index,
						AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.RelationshipId_Length
						);

			Array.Copy(BitConverter.GetBytes(currentLength),
						0,
						newFrameBytes,
						AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.RelationshipId_Index + AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.RelationshipId_Length,
						8
						);

			Array.Copy(_frameBytes,
						AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.RelationshipId_Index + AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.RelationshipId_Length,
						newFrameBytes,
						AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.RelationshipId_Index + AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.RelationshipId_Length + 8,
						currentLength
						);
			_frameBytes = newFrameBytes;
		}
	}
}
