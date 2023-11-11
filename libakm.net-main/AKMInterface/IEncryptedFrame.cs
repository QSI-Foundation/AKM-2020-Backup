/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

namespace AKMInterface
{
	/// <summary>
	/// AKM encrypted frame interface
	/// </summary>
	public interface IEncryptedFrame
	{
		/// <summary>
		/// Relationship identifier
		/// </summary>
		short RelationshipId { get; }
		/// <summary>
		/// Returns byte array storing encrypted part of AKM frame
		/// </summary>
		/// <returns>byte aray with encrypted content</returns>
		byte[] GetEncryptedData();
		/// <summary>
		/// Sets encrypted part of AKM frame
		/// </summary>
		/// <param name="data">byte array with encrypted AKM frame part</param>
		void SetEncryptedData(byte[] data);
		/// <summary>
		/// Method responsible for decrypting AKM frame content based on provided key
		/// </summary>
		/// <param name="key">decryption key</param>
		/// <returns>IDecrypted frame object</returns>
		IDecryptedFrame Decrypt(IKey key);
		/// <summary>
		/// Decrypts AKM frame and reutns full IDecryptedFrame object
		/// </summary>
		/// <returns>IDecrypted frame implementation</returns>
		byte[] GetTransmissionData();
		/// <summary>
		/// Sets AKM Frame length in header
		/// </summary>
		void SetFrameLength();
	}
}
