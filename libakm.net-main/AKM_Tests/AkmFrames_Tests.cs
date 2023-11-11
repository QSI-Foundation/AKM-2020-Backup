/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

using AKMCommon.Enum;
using AKMInterface;
using AKMLogic;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AKM_Tests
{
	public class AkmFrames_Tests
	{
		readonly Mock<ICryptography> _mockCrypto = new Mock<ICryptography>();
		readonly Mock<ILogger> _mockLogger = new Mock<ILogger>();

		private readonly byte[] _keyBytes = Encoding.UTF8.GetBytes("z$C&F)H@McQfTjWaZr4u7x!A%D*G-Kat");

		private readonly int _keySize = 256;
		private readonly int _blockSize = 128;
		private readonly PaddingMode _padding = PaddingMode.PKCS7;

		private byte[] CalculateHash(byte[] data)
		{
			using var sha = SHA256.Create();
			return sha.ComputeHash(data);
		}
		private byte[] Encrypt(byte[] dataToEncrypt, byte[] key)
		{
			using var rij = new RijndaelManaged
			{
				BlockSize = _blockSize,
				Padding = _padding,
				Key = key
			};
			rij.GenerateIV();

			ICryptoTransform encryptor = rij.CreateEncryptor(rij.Key, rij.IV);

			using var memoryStream = new MemoryStream();
			using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
			{
				cryptoStream.Write(rij.IV, 0, rij.IV.Length);
				cryptoStream.Write(dataToEncrypt, 0, dataToEncrypt.Length);
			}

			return memoryStream.ToArray();
		}

		private byte[] Decrypt(byte[] dataToDecrypt, byte[] key)
		{
			using var aes = Aes.Create();
			aes.KeySize = _keySize;
			aes.BlockSize = _blockSize;
			aes.Padding = _padding;
			aes.Key = key;
			var iv = new byte[16];
			Array.Copy(dataToDecrypt, 0, iv, 0, iv.Length);
			aes.IV = iv;

			ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

			using var memoryStream = new MemoryStream();
			using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Write))
			{
				cryptoStream.Write(dataToDecrypt, iv.Length, dataToDecrypt.Length - iv.Length);
			}

			return memoryStream.ToArray();
		}
		[SetUp]
		public void Setup()
		{
			_mockCrypto.Setup(x => x.Encrypt(It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns((byte[] d, byte[] k) => Encrypt(d, _keyBytes));
			_mockCrypto.Setup(x => x.Decrypt(It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns((byte[] d, byte[] k) => Decrypt(d, _keyBytes));
			_mockCrypto.Setup(x => x.CalculateHash(It.IsAny<byte[]>())).Returns((byte[] d) => CalculateHash(d));
			_mockCrypto.Setup(x => x.HashLength).Returns(32);
			AkmSetup.Logger = _mockLogger.Object;
		}

		[Test]
		public void DecryptedFramesReturnsProperFrameEvent()
		{
			var df = new AkmDecryptedFrame(_mockCrypto.Object, _mockLogger.Object, 1);

			AkmEvent e = AkmEvent.CannotDecrypt;
			df.SetFrameEvent(e);
			Assert.AreEqual(AkmEvent.CannotDecrypt, df.FrameEvent);

			e = AkmEvent.None;
			df.SetFrameEvent(e);
			Assert.AreEqual((byte)255, (byte)df.FrameEvent);

			e = AkmEvent.RecvSE;
			df.SetFrameEvent(e);
			Assert.AreEqual(AkmEvent.RecvSE, df.FrameEvent);

			e = AkmEvent.RecvSEC;
			df.SetFrameEvent(e);
			Assert.AreEqual(AkmEvent.RecvSEC, df.FrameEvent);

			e = AkmEvent.RecvSEF;
			df.SetFrameEvent(e);
			Assert.AreEqual(AkmEvent.RecvSEF, df.FrameEvent);

			e = AkmEvent.RecvSEI;
			df.SetFrameEvent(e);
			Assert.AreEqual(AkmEvent.RecvSEI, df.FrameEvent);

			e = AkmEvent.TimeOut;
			df.SetFrameEvent(e);
			Assert.AreEqual(AkmEvent.TimeOut, df.FrameEvent);
		}

		[Test]
		public void DecryptedFrameReturnsProperRelationshipId()
		{
			short relId = 1;
			var df = new AkmDecryptedFrame(_mockCrypto.Object, _mockLogger.Object, relId);

			Assert.AreEqual(1, df.RelationshipId);
		}
		[Test]
		public void EmptyDecryptedFrameReturnsEmptyHash()
		{
			var df = new AkmDecryptedFrame(_mockCrypto.Object, _mockLogger.Object, 1);
			Assert.IsTrue(string.IsNullOrEmpty(df.GetContentHashAsString()));
			Assert.IsNull(df.GetContentHashAsByteArray());
		}

		[Test]
		public void DecryptedFrameRetrunsProperHash()
		{
			var content = "Test Content Message";
			var akmEvent = AkmEvent.RecvSE;
			short tad = 1;
			short sad = 5;
			short rId = 1;

			List<byte> frameBytes = new List<byte>();
			frameBytes.AddRange(BitConverter.GetBytes(rId));
			frameBytes.AddRange(BitConverter.GetBytes(sad));
			frameBytes.AddRange(BitConverter.GetBytes(tad));
			frameBytes.Add((byte)akmEvent);
			frameBytes.AddRange(Encoding.UTF8.GetBytes(content));

			var df = new AkmDecryptedFrame(_mockCrypto.Object, _mockLogger.Object, rId);
			df.SetSourceAddress(sad);
			df.SetTargetAddress(tad);
			df.SetFrameEvent(AkmEvent.RecvSE);
			df.SetContent(Encoding.UTF8.GetBytes("Test Content Message"));

			df.SetContentHash();

			var hash = _mockCrypto.Object.CalculateHash(frameBytes.ToArray());
			var hashString = Encoding.UTF8.GetString(hash);

			Assert.AreEqual(hash, df.GetContentHashAsByteArray());
			Assert.AreEqual(hashString, df.GetContentHashAsString());
		}


		[Test]
		public void AkmDecryptedFrameSetDataGivesTheSameResultAsElements()
		{
			var content = "Test Content Message";
			var akmEvent = AkmEvent.RecvSE;
			short tad = 1;
			short sad = 5;
			short rId = 1;

			List<byte> frameBytes = new List<byte>();
			frameBytes.AddRange(BitConverter.GetBytes(rId));
			frameBytes.AddRange(BitConverter.GetBytes(sad));
			frameBytes.AddRange(BitConverter.GetBytes(tad));
			frameBytes.Add((byte)akmEvent);
			frameBytes.AddRange(Encoding.UTF8.GetBytes(content));

			var df1 = new AkmDecryptedFrame(_mockCrypto.Object, _mockLogger.Object, 1);
			df1.SetSourceAddress(5);
			df1.SetTargetAddress(1);
			df1.SetFrameEvent(AkmEvent.RecvSE);
			df1.SetContent(Encoding.UTF8.GetBytes("Test Content Message"));

			df1.SetContentHash();

			var df2 = new AkmDecryptedFrame(_mockCrypto.Object, _mockLogger.Object, 1);
			df2.SetData(frameBytes.Skip(AkmSetup.AkmAppCfg[rId].FrameSchema.RelationshipId_Length).ToArray());

			df2.SetContentHash();

			Assert.AreEqual(df1.GetFrameBytes(), df2.GetFrameBytes());
			Assert.AreEqual(df1.GetContentBytes(), df1.GetContentBytes());
			Assert.AreEqual(df1.GetContentHashAsByteArray(), df2.GetContentHashAsByteArray());
			Assert.AreEqual(df1.GetContentHashAsString(), df2.GetContentHashAsString());
			Assert.AreEqual(df1.GetSourceAddressAsByteArray(), df2.GetSourceAddressAsByteArray());
			Assert.AreEqual(df1.GetSourceAddressAsShort(), df2.GetSourceAddressAsShort());
			Assert.AreEqual(df1.GetTargetAddressAsByteArray(), df2.GetTargetAddressAsByteArray());
			Assert.AreEqual(df1.GetTargetAddressAsShort(), df2.GetTargetAddressAsShort());

		}

	}
}
