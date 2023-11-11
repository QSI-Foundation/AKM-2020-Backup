/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

namespace AKMCommon.Struct
{
	/// <summary>
	/// AKM parameters required for C-library initialization
	/// </summary>
	public struct AkmConfigParams
	{
		/// <summary>
		/// Size of Key
		/// </summary>
		public byte SK { get; set; }
		/// <summary>
		/// Size of Ring Node Addresses
		/// </summary>
		public byte SRNA { get; set; }
		/// <summary>
		/// Number of Nodes within AKM Relationship/Echo Ring
		/// </summary>
		public ushort N { get; set; }
		/// <summary>
		/// Current Session Seed
		/// </summary>
		public uint CSS { get; set; }
		/// <summary>
		/// Next Session Seed
		/// </summary>
		public uint NSS { get; set; }
		/// <summary>
		/// Fallback Session Seed
		/// </summary>
		public uint FSS { get; set; }
		/// <summary>
		/// Next Fallback Session Seed
		/// </summary>
		public uint NFSS { get; set; }
		/// <summary>
		/// Shadow Fallback Session Seed
		/// </summary>
		public uint SFSS { get; set; }
		/// <summary>
		/// Next Shadow Fallback Session Seed
		/// </summary>
		public uint NSFSS { get; set; }
		/// <summary>
		/// Emergency Failsafe Session Seed
		/// </summary>
		public uint EFSS { get; set; }
		/// <summary>
		/// Node Nonresponse Timeout
		/// </summary>
		public long NNRT { get; set; }
		/// <summary>
		/// Normal Session Establishment Timeout
		/// </summary>
		public long NSET { get; set; }
		/// <summary>
		/// Fallback Resynchronization Session Establishment Timeout
		/// </summary>
		public long FBSET { get; set; }
		/// <summary>
		/// Failsafe Resynchronization Session Establishment Timeout
		/// </summary>
		public long FSSET { get; set; }
	}
}
