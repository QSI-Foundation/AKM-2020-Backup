/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */
namespace AKMInterface
{
	/// <summary>
	/// Cryptographic key used in AKM process
	/// </summary>
	public interface IKey
	{
		/// <summary>
		/// Key length in bytes
		/// </summary>
		int KeyLength { get; set; }
		/// <summary>
		/// Key bytes encoded as Base64 String
		/// </summary>
		string KeyAsBase64String { get; }
		/// <summary>
		/// Key as a byte array
		/// </summary>
		byte[] KeyAsBytes { get; }

		/// <summary>
		/// Sets new key value 
		/// </summary>
		/// <param name="keyBytes">byte array with new key</param>
		void SetKey(byte[] keyBytes);
		/// <summary>
		/// Sets new key value
		/// </summary>
		/// <param name="keyString">string value with new key</param>
		void SetKeyFromBase64String(string keyString);
	}
}
