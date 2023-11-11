/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

using AKMCommon.Enum;
using System;

namespace AKMCommon.Struct
{
	/// <summary>
	/// AKM Command structure storing information about required operation, parameters and pointer to data
	/// </summary>
	public struct AkmCommand
	{
		/// <summary>
		/// Action that needs to be done
		/// </summary>
		public AkmCmdOpCode opcode;
		/// <summary>
		/// indexes used in key updates
		/// </summary>
		public int p1, p2;
		/// <summary>
		/// pointer to unmanaged memory for data update
		/// </summary>
		public IntPtr data;
	}
}
