/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

using System;

namespace AKMInterface
{
	/// <summary>
	/// Provides functionality to generate new IKey objects 
	/// </summary>
	public interface IKeyFactory
	{
		/// <summary>
		/// Creates new IKey object from given memory address
		/// </summary>
		/// <param name="pKeyBytes">Pointer to memory address with new key value</param>
		/// <param name="keyLength">Length of memory area that should be used for creating new key</param>
		/// <returns>IKey object</returns>
		IKey Create(IntPtr pKeyBytes, int keyLength);
		/// <summary>
		/// Creates new IKey object from byte array
		/// </summary>
		/// <param name="pKeyBytes">Byte array with new key data</param>
		/// <returns>IKey object</returns>
		IKey Create(byte[] pKeyBytes);
		/// <summary>
		/// Creates new IKey object from a string 
		/// </summary>
		/// <param name="pKeyString">string with new key value</param>
		/// <returns>IKey object</returns>
		IKey Create(string pKeyString);
	}
}
