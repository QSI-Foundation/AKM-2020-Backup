/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

using System;

namespace AKMCommon.Struct
{
	/// <summary>
	/// Structure storing AKM required configuration
	/// </summary>
	public struct AkmConfiguration
	{
		/// <summary>
		/// AKM configuration parameters structure
		/// </summary>
		public AkmConfigParams cfgParams;
		/// <summary>
		/// Pointer to PDV array
		/// </summary>
		public IntPtr pdv;
		/// <summary>
		/// Pointer to node addresses array
		/// </summary>
		public IntPtr nodeAddresses;
		/// <summary>
		/// Pointer to self address value
		/// </summary>
		public IntPtr selfNodeAddress;
	}
}
