/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

using AKMLogic;
using NUnit.Framework;
using System.Text;

namespace AKM_Tests
{
	public class AkmCrypo_Tests
	{
		private const string AES_KEY_STRING = "kXp2s5v8y/B?E(H+MbQeThVmYq3t6w9z";
		private readonly byte[] AES_KEY_BYTES = Encoding.UTF8.GetBytes(AES_KEY_STRING);
		[SetUp]
		public void Setup()
		{

		}

		[Test]
		public void AkmCryptoCreatedWithProperValues()
		{
			var akmCrypto = new AkmCrypto();
			Assert.AreEqual(256, akmCrypto.KeySize);
			Assert.AreEqual(128, akmCrypto.BlockSize);

			akmCrypto = new AkmCrypto(42);
			Assert.AreEqual(42, akmCrypto.KeySize);
			Assert.AreEqual(128, akmCrypto.BlockSize);

			akmCrypto = new AkmCrypto(123, 987);
			Assert.AreEqual(123, akmCrypto.KeySize);
			Assert.AreEqual(987, akmCrypto.BlockSize);
		}

		[Test]
		public void AkmCryptoEncryptionValidation()
		{
			var akmCrypto = new AkmCrypto();
			var message = "Clear Text Message";
			var messageBytes = Encoding.UTF8.GetBytes(message);

			var encryptedBytes = akmCrypto.Encrypt(messageBytes, AES_KEY_BYTES);
			var encryptedMessage = Encoding.UTF8.GetString(encryptedBytes);
			Assert.AreNotEqual(message, encryptedMessage);

			var decryptedBytes = akmCrypto.Decrypt(encryptedBytes, AES_KEY_BYTES);
			var decryptedMessage = Encoding.UTF8.GetString(decryptedBytes);

			Assert.AreEqual(message, decryptedMessage);
			Assert.AreEqual(messageBytes.Length, decryptedBytes.Length);
		}
	}
}
