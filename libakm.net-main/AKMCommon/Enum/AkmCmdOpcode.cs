/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

namespace AKMCommon.Enum
{
	/// <summary>
	/// AKM Operation codes for setting state machine and triggering AKM workflow events
	/// </summary>
	public enum AkmCmdOpCode
	{
		/// <summary>
		/// Return current state
		/// </summary>
		Return = 0,
		/// <summary>
		/// Set AKM state to sending data
		/// </summary>
		SetSendEvent = 1,
		/// <summary>
		/// Set AKM state to setting new key
		/// </summary>
		SetKey = 2,
		/// <summary>
		/// Set AKM state to reset key
		/// </summary>
		ResetKey = 3,
		/// <summary>
		/// Set AKM state to change active key index
		/// </summary>
		MoveKey = 4,
		/// <summary>
		/// Set AKM state to use new keys
		/// </summary>
		UseKeys = 5,
		/// <summary>
		/// Set AKM state to retry decryption
		/// </summary>
		RetryDec = 6,
		/// <summary>
		/// Set AKM state to set new timer value
		/// </summary>
		SetTimer = 7,
		/// <summary>
		/// Set AKM state to reset timer
		/// </summary>
		ResetTimer = 8
	}
}
