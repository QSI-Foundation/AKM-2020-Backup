/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

namespace AKMCommon.Struct
{
	/// <summary>
	/// AKM application configuration structure
	/// </summary>
	public sealed class AkmAppConfig
	{
		/// <summary>
		/// Communication port for service listening
		/// </summary>
		public int CommunicationPort { get; set; }
		/// <summary>
		/// IP address used for server endpoint
		/// </summary>
		public string IPAddress { get; set; }

		/// <summary>
		/// Relatinship group identifier
		/// </summary>
		public short RelationshipId { get; set; }
		/// <summary>
		/// Nodes own numeric address value
		/// </summary>
		public byte SelfAddressValue { get; set; }
		/// <summary>
		/// Default size of cryptographic key length in bytes
		/// </summary>
		public byte DefaultKeySize { get; set; }
		/// <summary>
		/// AKM Frame schema definition
		/// </summary>
		public AkmConfigFrameSchema FrameSchema { get; set; }
		/// <summary>
		/// AKM specific configuration settings
		/// </summary>
		public AkmConfigParams AkmConfigParameters { get; set; }
		/// <summary>
		/// Optional initial cryptographic keys
		/// </summary>
		public AkmConfigKeyString[] InitialKeys { get; set; }
		/// <summary>
		/// Array for numeric addresses of all nodes in this Relationship group
		/// </summary>
		public byte[] NodesAddresses { get; set; }

		/// <summary>
		/// PDV array used in new key generation
		/// </summary>
		public byte[] PDV { get; set; }

	}
	/// <summary>
	/// AKM Frame schema definition regarding lengths and starting indexes of each required part
	/// </summary>
	public sealed class AkmConfigFrameSchema
	{
		/// <summary>
		/// starting index of relationship Id value in whole frame byte array
		/// </summary>
		public byte RelationshipId_Index { get; set; }
		/// <summary>
		/// length of Relationship Id section in frame byte arra
		/// </summary>
		public byte RelationshipId_Length { get; set; }
		/// <summary>
		/// starting index of source address value in whole frame byte array
		/// </summary>
		public byte SourceAddress_Index { get; set; }
		/// <summary>
		/// length of Source Address section in frame byte arra
		/// </summary>
		public byte SourceAddress_Length { get; set; }
		/// <summary>
		/// starting index of target address value in whole frame byte array
		/// </summary>
		public byte TargetAddress_Index { get; set; }
		/// <summary>
		/// length of Target address section in frame byte arra
		/// </summary>
		public byte TargetAddress_Length { get; set; }
		/// <summary>
		/// starting index of AKM Event value in whole frame byte array
		/// </summary>
		public byte AkmEvent_Index { get; set; }
		/// <summary>
		/// length of AKM Event section in frame byte arra
		/// </summary>
		public byte AkmEvent_Length { get; set; }
		/// <summary>
		/// starting index of data packeg value in whole frame byte array
		/// </summary>
		public byte AkmDataStart_Index { get; set; }
	}

	/// <summary>
	/// AKM initial key string
	/// </summary>
	public sealed class AkmConfigKeyString
	{
		/// <summary>
		/// Initial key value defined in application config file
		/// </summary>
		public string InitialKey { get; set; }
	}
}
