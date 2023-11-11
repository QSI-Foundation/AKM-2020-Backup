/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

using AKMCommon.Enum;

namespace AKMInterface
{
	/// <summary>
	/// Functional requirements for AKM decrypted frame object
	/// </summary>
	public interface IDecryptedFrame
	{
		/// <summary>
		/// Relationshyip Id value for current Relationship group
		/// </summary>
		short RelationshipId { get; }
		/// <summary>
		/// Encrypts current frame
		/// </summary>
		/// <param name="key">IKey with encryption key value</param>
		/// <returns>IEncrypted frame created from decrypted frame data and provided encrypteion key value</returns>
		IEncryptedFrame Encrypt(IKey key);
		/// <summary>
		/// AKM event set for this frame
		/// </summary>
		AkmEvent FrameEvent { get; }

		/// <summary>
		/// Sets frame's sender address
		/// </summary>
		/// <param name="sourceAddress">sender's address as numeric short value</param>
		void SetSourceAddress(short sourceAddress);
		/// <summary>
		/// Sets frame's sender address
		/// </summary>
		/// <param name="sourceAddress">sender's address as byte array</param>
		void SetSourceAddress(byte[] sourceAddress);
		/// <summary>
		/// Sets frame's target address
		/// </summary>
		/// <param name="targetAddress">target address as numeric short value</param>
		void SetTargetAddress(short targetAddress);
		/// <summary>
		/// Sets frame's target address
		/// </summary>
		/// <param name="targetAddress">target address as byte array</param>
		void SetTargetAddress(byte[] targetAddress);

		/// <summary>
		/// Sets flag for given AKM event 
		/// </summary>
		/// <param name="akmEvent">AKM event that will be sent with this frame</param>
		void SetFrameEvent(AkmEvent akmEvent);
		/// <summary>
		/// Sets the transmission data
		/// </summary>
		/// <param name="content">byte array holding infomration that will be sent inside AKM frame</param>
		void SetContent(byte[] content);
		/// <summary>
		/// Update frame with hash value of transmitted content
		/// </summary>
		void SetContentHash();
		/// <summary>
		/// Sets whole frame content that will be encrypted for transmission
		/// </summary>
		/// <param name="frameData"></param>
		void SetData(byte[] frameData);

		/// <summary>
		/// Gets frame's source address in a form of byte array
		/// </summary>
		/// <returns>byte array with frame's source address</returns>
		byte[] GetSourceAddressAsByteArray();

		/// <summary>
		/// Gets frame's source address as a numeric short value
		/// </summary>
		/// <returns>short value with frame's source address</returns>
		short GetSourceAddressAsShort();
		/// <summary>
		/// Gets frame's targer address in a form of byte array
		/// </summary>
		/// <returns>byte array with frame's target address</returns>
		byte[] GetTargetAddressAsByteArray();
		/// <summary>
		/// Gets frame's source target as a numeric short value
		/// </summary>
		/// <returns>short value with frame's target address</returns>
		short GetTargetAddressAsShort();
		/// <summary>
		/// Extracts data package from AKM frame
		/// </summary>
		/// <returns>byte array with data transmitted in AKM frame</returns>
		byte[] GetContentBytes();
		/// <summary>
		/// Gets hash value for frame content in a form of a string
		/// </summary>
		/// <returns>string value with hash calculated for frame's content</returns>
		string GetContentHashAsString();
		/// <summary>
		/// Gets hash value for frame content in a form of a byte array
		/// </summary>
		/// <returns>byte array with hash calculated for frame's content</returns>
		byte[] GetContentHashAsByteArray();
	}
}
