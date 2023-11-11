/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

using AKMCommon.Enum;
using AKMCommon.Struct;
using System;
using System.Runtime.InteropServices;

namespace AKMInterface
{
	/// <summary>
	/// Default implementation interface for C AKM library calls
	/// </summary>
	public interface ICLibCalls
	{
		/// <summary>
		/// Intializes AKM state machine based on provided parameters
		/// </summary>
		/// <param name="ctx">AKM processing context reference</param>
		/// <param name="config">AKM configuration structure reference</param>
		/// <returns></returns>
		[DllImport("libakm.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern AkmStatus AKMInit(ref AkmProcessCtx ctx, ref AkmConfiguration config);
		/// <summary>
		/// Processes AKM frame
		/// </summary>
		/// <param name="ctx">reference to AKM processing context</param>
		[DllImport("libakm.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern void AKMProcess(ref AkmProcessCtx ctx);
		/// <summary>
		/// Frees up resources allocated for given relationship by unmanaged code
		/// </summary>
		/// <param name="relationship">pointer to AKM relationship</param>
		[DllImport("libakm.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern void AKMFree(IntPtr relationship);

		/// <summary>
		/// Returns curently used AKM configuration
		/// </summary>
		/// <param name="relationship"></param>
		/// <param name="config"></param>
		[DllImport("libakm.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern void AKMGetConfig(IntPtr relationship, ref AkmConfiguration config);
	}
}
