/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

using AKMInterface;
using System;

namespace AKMLogic
{
	/// <summary>
	/// Default implementation of IKeyFactory
	/// </summary>
	public class AkmKeyFactory : IKeyFactory
	{
		private const int DEF_KEY_LENGTH = 32;
		private readonly int _keyLength;

		/// <summary>
		/// Default constructor
		/// </summary>
		public AkmKeyFactory()
		{
			_keyLength = DEF_KEY_LENGTH;
		}
		/// <summary>
		/// Constructor allowing setting custom key length 
		/// </summary>
		/// <param name="keyLength">Key length in bytes</param>
		public AkmKeyFactory(int keyLength)
		{
			_keyLength = keyLength;
		}

		/// <inheritdoc/>
		public IKey Create(IntPtr pKeyBytes, int keyLength)
		{
			var keyBytes = new byte[keyLength];

			System.Runtime.InteropServices.Marshal.Copy(pKeyBytes, keyBytes, 0, keyLength);
			return Create(keyBytes);
		}

		/// <inheritdoc/>
		public IKey Create(byte[] pKeyBytes)
		{
			var key = new AkmKey(_keyLength);
			key.SetKey(pKeyBytes);
			return key;

		}

		/// <inheritdoc/>
		public IKey Create(string pKeyString)
		{
			var key = new AkmKey(_keyLength);
			key.SetKeyFromBase64String(pKeyString);
			return key;
		}
	}

}
