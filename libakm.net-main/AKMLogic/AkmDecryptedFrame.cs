/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

using AKMCommon.Enum;
using AKMInterface;
using Microsoft.Extensions.Logging;
using System;
using System.Text;

namespace AKMLogic
{
	/// <summary>
	/// Default IDecryptedFrame implementation
	/// </summary>
	public class AkmDecryptedFrame : IDecryptedFrame
	{
		private byte[] _frameBytes;
		private readonly ICryptography _crypto;

		private readonly ILogger _logger;
		private short _relationshipId;
		private bool _hashAdded;

		/// <inheritdoc/>
		public short RelationshipId
		{
			get
			{
				return _relationshipId;
			}

			private set
			{
				_relationshipId = value;
				var relIdBytes = BitConverter.GetBytes(value);
				Array.Copy(relIdBytes, 0, _frameBytes, AkmSetup.AkmAppCfg[value].FrameSchema.RelationshipId_Index, sizeof(short));
			}
		}
		/// <inheritdoc/>
		public AkmEvent FrameEvent
		{
			get { return (AkmEvent)_frameBytes[AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.AkmEvent_Index]; }
		}

		/// <inheritdoc/>
		public AkmDecryptedFrame(ICryptography cryptographyProvider, ILogger logger, short relationshipId)
		{
			_crypto = cryptographyProvider;
			_logger = logger;
			_frameBytes = new byte[AkmSetup.AkmAppCfg[relationshipId].FrameSchema.AkmDataStart_Index]; //sets the minimun length of an empty frame
			RelationshipId = relationshipId;
		}
		/// <inheritdoc/>
		public IEncryptedFrame Encrypt(IKey key)
		{
			var result = new AkmEncryptedFrame(_crypto, _logger, RelationshipId);
			if (!_hashAdded)
			{
				SetContentHash();
			}

			result.SetEncryptedData(_crypto.Encrypt(GetFrameBytesForEncryption(), key.KeyAsBytes));

			return result;
		}
		/// <inheritdoc/>
		public void SetFrameEvent(AkmEvent akmEvent)
		{
			_frameBytes[AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.AkmEvent_Index] = (byte)akmEvent;
		}
		/// <inheritdoc/>
		public void SetContent(byte[] contentBytes)
		{
			var targetLength = contentBytes.Length + AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.AkmDataStart_Index;
			if (_frameBytes.Length != targetLength)
			{
				Array.Resize(ref _frameBytes, targetLength);
			}
			Array.Copy(contentBytes, 0, _frameBytes, AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.AkmDataStart_Index, contentBytes.Length);
			_hashAdded = false;
		}
		/// <inheritdoc/>
		public void SetData(byte[] frameData)
		{
			var targetLength = frameData.Length + AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.RelationshipId_Length;
			if (_frameBytes.Length != targetLength)
			{
				Array.Resize(ref _frameBytes, targetLength);
			}
			Array.Copy(frameData, 0, _frameBytes, AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.RelationshipId_Length, frameData.Length);
		}

		/// <inheritdoc/>
		public void SetSourceAddress(short soureceAddress)
		{
			var addressBytes = BitConverter.GetBytes(soureceAddress);
			SetSourceAddress(addressBytes);
		}
		/// <inheritdoc/>
		public void SetSourceAddress(byte[] sourceAddress)
		{
			Array.Copy(sourceAddress, 0, _frameBytes, AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.SourceAddress_Index, AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.SourceAddress_Length);
		}
		/// <inheritdoc/>
		public void SetTargetAddress(short targetAddress)
		{
			var addressBytes = BitConverter.GetBytes(targetAddress);
			SetTargetAddress(addressBytes);
		}
		/// <inheritdoc/>
		public void SetTargetAddress(byte[] targetAddress)
		{
			Array.Copy(targetAddress, 0, _frameBytes, AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.TargetAddress_Index, AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.TargetAddress_Length);
		}
		/// <inheritdoc/>
		public byte[] GetSourceAddressAsByteArray()
		{
			return _frameBytes.AsSpan(AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.SourceAddress_Index, AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.SourceAddress_Length).ToArray();
		}
		/// <inheritdoc/>
		public byte[] GetTargetAddressAsByteArray()
		{
			return _frameBytes.AsSpan(AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.TargetAddress_Index, AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.TargetAddress_Length).ToArray();
		}
		/// <inheritdoc/>
		public byte[] GetFrameBytes()
		{
			return _frameBytes;
		}
		/// <inheritdoc/>
		public byte[] GetFrameBytesForEncryption()
		{
			byte[] forEncryption = new byte[_frameBytes.Length - AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.RelationshipId_Length];
			Array.Copy(_frameBytes, AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.RelationshipId_Index + AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.RelationshipId_Length, forEncryption, 0, forEncryption.Length);
			return forEncryption;
		}
		/// <inheritdoc/>
		public byte[] GetContentBytes()
		{
			return _frameBytes.AsSpan(AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.AkmDataStart_Index, _frameBytes.Length - _crypto.HashLength - AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.AkmDataStart_Index).ToArray();
		}
		/// <inheritdoc/>
		public short GetSourceAddressAsShort()
		{
			return BitConverter.ToInt16(_frameBytes.AsSpan(AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.SourceAddress_Index, AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.SourceAddress_Length));
		}
		/// <inheritdoc/>
		public short GetTargetAddressAsShort()
		{
			return BitConverter.ToInt16(_frameBytes.AsSpan(AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.TargetAddress_Index, AkmSetup.AkmAppCfg[RelationshipId].FrameSchema.TargetAddress_Length));
		}
		/// <inheritdoc/>
		public void SetContentHash()
		{
			byte[] hashed;
			var oldLength = _frameBytes.Length;
			if (!_hashAdded)
			{
				hashed = _crypto.CalculateHash(_frameBytes);
				Array.Resize<byte>(ref _frameBytes, _frameBytes.Length + hashed.Length);
			}
			else
			{
				hashed = _crypto.CalculateHash(_frameBytes.AsSpan(0, _frameBytes.Length - _crypto.HashLength).ToArray());
			}

			Array.Copy(hashed, 0, _frameBytes, oldLength, hashed.Length);
			_hashAdded = true;
		}
		/// <inheritdoc/>
		public string GetContentHashAsString()
		{
			if (!_hashAdded) return string.Empty;

			return Encoding.UTF8.GetString(_frameBytes.AsSpan(_frameBytes.Length - _crypto.HashLength));

		}
		/// <inheritdoc/>
		public byte[] GetContentHashAsByteArray()
		{
			if (!_hashAdded) return null;
			return _frameBytes.AsSpan(_frameBytes.Length - _crypto.HashLength).ToArray();
		}
		/// <summary>
		/// Checks if hash stored in AKM frame is valid
		/// </summary>
		/// <returns>true if stored and calculated hash values are the same, false if there is a difference</returns>
		public bool CheckHash()
		{
			var dataToBeHashed = _frameBytes.AsSpan(0, _frameBytes.Length - _crypto.HashLength).ToArray();

			var hashString = Encoding.UTF8.GetString(_crypto.CalculateHash(dataToBeHashed));
			var frameString = Encoding.UTF8.GetString(_frameBytes.AsSpan(_frameBytes.Length - _crypto.HashLength));

			return hashString == frameString;

		}
	}
}
