/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

using AKMInterface;
using System;
using System.Text;

namespace AKMLogic
{
	/// <summary>
	/// Default IKey implementation
	/// </summary>
	public class AkmKey : IKey
	{
		private const int DEF_KEY_LENGTH = 32;
		/// <summary>
		/// Contructor allowing for custom key length
		/// </summary>
		/// <param name="keyLength">Encryption Key Length in bytes (not bits!)</param>
		public AkmKey(int keyLength)
		{
			KeyAsBytes = new byte[keyLength];
			KeyLength = keyLength;
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public AkmKey()
		{
			KeyAsBytes = new byte[DEF_KEY_LENGTH];
			KeyLength = DEF_KEY_LENGTH;
		}

		/// <summary>
		/// Gets or sets key length value
		/// </summary>
		public int KeyLength { get; set; }

		/// <summary>
		/// Current key as UTF8 encoded string
		/// </summary>
		public string KeyAsString => Encoding.UTF8.GetString(KeyAsBytes);
		/// <summary>
		/// Current key as Base64 string
		/// </summary>
		public string KeyAsBase64String => Convert.ToBase64String(KeyAsBytes);

		/// <summary>
		/// Current key as byte array
		/// </summary>
		public byte[] KeyAsBytes { get; private set; }

		/// <summary>
		/// Update current key value from byte array
		/// </summary>
		/// <param name="keyBytes">byte array with new key value</param>
		public void SetKey(byte[] keyBytes)
		{
			if (keyBytes == null || keyBytes.Length == 0 || keyBytes.Length != KeyLength)
			{
				throw new ArgumentException("Invalid array length. Can't be null and must be equal to defined Key Length.");
			}
			KeyAsBytes = keyBytes;
		}
		/// <summary>
		/// Update current key value from a string
		/// </summary>
		/// <param name="keyString">string with new key value</param>
		public void SetKeyFromString(string keyString)
		{
			if (string.IsNullOrEmpty(keyString))
			{
				throw new ArgumentException("Key cannot be empty.");
			}
			var tempKey = Encoding.UTF8.GetBytes(keyString);
			if (tempKey.Length != KeyLength)
			{
				throw new ArgumentException("Invalid new key length.");
			}

			KeyAsBytes = tempKey;
		}

		/// <summary>
		/// Update current key value from a Base64 encoded string
		/// </summary>
		/// <param name="keyString">Base64 encoded string with new key value</param>
		public void SetKeyFromBase64String(string keyString)
		{
			if (string.IsNullOrEmpty(keyString))
			{
				throw new ArgumentException("Key cannot be empty.");
			}
			var tempKey = Convert.FromBase64String(keyString);
			if (tempKey.Length != KeyLength)
			{
				throw new ArgumentException("Invalid new key length.");
			}

			KeyAsBytes = tempKey;
		}
	}
}
