/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

namespace AKMInterface
{
	/// <summary>
	/// Minimum set of functionality required by a cryptographic service provider
	/// </summary>
	public interface ICryptography
	{
		/// <summary>
		/// length of a hash value for used hash function
		/// </summary>
		int HashLength { get; }
		/// <summary>
		/// Calculate hash for given data
		/// </summary>
		/// <param name="data">byte array with data</param>
		/// <returns>byte array with hash value</returns>
		byte[] CalculateHash(byte[] data);
		/// <summary>
		/// Decrypt given data using provided key
		/// </summary>
		/// <param name="dataToDecrypt">byte array with encrypted data</param>
		/// <param name="key">byte array with key value for decryption</param>
		/// <returns>decrypted data as byte array</returns>
		byte[] Decrypt(byte[] dataToDecrypt, byte[] key);
		/// <summary>
		/// Encrypt given data using provided key
		/// </summary>
		/// <param name="dataToEncrypt">byte array with data for encryption</param>
		/// <param name="key">byte array with key value for encryption</param>
		/// <returns>encrypted data as byte array</returns>
		byte[] Encrypt(byte[] dataToEncrypt, byte[] key);
	}
}
