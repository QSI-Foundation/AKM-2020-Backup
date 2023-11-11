/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

using AKMInterface;
using System;
using System.IO;
using System.Security.Cryptography;

namespace AKMLogic
{
	/// <summary>
	/// Default cryptographic service provider for encryption, decryption and hash value checks
	/// </summary>
	public class AkmCrypto : ICryptography, IDisposable
	{
		private SHA256 _sha256;
		/// <inheritDoc/>
		public int KeySize { get; } = 256;
		/// <inheritDoc/>
		public int BlockSize { get; } = 128;
		/// <inheritDoc/>
		public int HashLength { get; } = 32;

		/// <summary>
		/// Default constructor
		/// </summary>
		public AkmCrypto() : this(256, 128)
		{
		}

		/// <summary>
		/// Constructor allowing custom keysize value
		/// </summary>
		/// <param name="keySize">encryption key length in bits</param>
		public AkmCrypto(int keySize) : this(keySize, 128)
		{
		}
		/// <summary>
		/// Constructor allowing for custom key and block size
		/// </summary>
		/// <param name="keySize">encryption key length in bits</param>
		/// <param name="blockSize">encryption block size in bits</param>
		public AkmCrypto(int keySize, int blockSize)
		{
			KeySize = keySize;
			BlockSize = blockSize;
			_sha256 = SHA256.Create();
		}
		/// <inheritDoc/>
		public byte[] Encrypt(byte[] dataToEncrypt, byte[] key)
		{
#if SKIP_ENCRYPTION
                return dataToEncrypt;
#else


			using var rij = new RijndaelManaged
			{
				BlockSize = BlockSize,
				Padding = PaddingMode.PKCS7,
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
#endif
		}
		/// <inheritDoc/>
		public byte[] Decrypt(byte[] dataToDecrypt, byte[] key)
		{
#if SKIP_ENCRYPTION
                return dataToDecrypt;
#else
			using var aes = Aes.Create();
			aes.KeySize = KeySize;
			aes.BlockSize = BlockSize;
			aes.Padding = PaddingMode.PKCS7;
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
#endif
		}
		/// <inheritDoc/>
		public byte[] CalculateHash(byte[] data)
		{
			return _sha256.ComputeHash(data);
		}
		/// <inheritDoc/>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				_sha256?.Dispose();
				_sha256 = null;
			}
		}
		/// <summary>
		/// Destructor
		/// </summary>
		~AkmCrypto()
		{
			Dispose(false);
		}
		/// <inheritDoc/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}
