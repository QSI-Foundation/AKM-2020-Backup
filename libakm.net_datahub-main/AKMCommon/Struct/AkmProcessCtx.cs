/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

using AKMCommon.Enum;
using System;

namespace AKMCommon.Struct
{
	/// <summary>
	/// AKM Structure storing current processing context
	/// </summary>
	public struct AkmProcessCtx
	{
		/// <summary>
		/// Pointer to relationship data memory area
		/// </summary>
		public IntPtr relationship;
		/// <summary>
		/// Current AKM event
		/// </summary>
		public AkmEvent akmEvent;
		/// <summary>
		/// Pointer to source address memory area
		/// </summary>
		public IntPtr srcAddr;
		/// <summary>
		/// Time counter
		/// </summary>
		public long time_ms;
		/// <summary>
		/// AKM command that will be executed
		/// </summary>
		public AkmCommand cmd;
	}
}
