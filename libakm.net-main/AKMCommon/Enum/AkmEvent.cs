/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

namespace AKMCommon.Enum
{
	/// <summary>
	/// AKM events sent and received with each frame
	/// </summary>
	public enum AkmEvent
	{
		/// <summary>
		/// No action required
		/// </summary>
		None = -1,
		/// <summary>
		/// Session established
		/// </summary>
		RecvSE = 0,
		/// <summary>
		/// Initialize Session
		/// </summary>
		RecvSEI = 1,
		/// <summary>
		/// Session confirmed
		/// </summary>
		RecvSEC = 2,
		/// <summary>
		/// session finished
		/// </summary>
		RecvSEF = 3,
		/// <summary>
		/// Cannot decrypt
		/// </summary>
		CannotDecrypt = 4,
		/// <summary>
		/// Operation timeout
		/// </summary>
		TimeOut = 5,
		/// <summary>
		/// Event for forcing session initialization and key change
		/// </summary>
		LocalSEI = 6
	}
}
