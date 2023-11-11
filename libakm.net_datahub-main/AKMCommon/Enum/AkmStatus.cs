/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

namespace AKMCommon.Enum
{
	/// <summary>
	/// AKM Frame processing results returned from C library
	/// </summary>
	public enum AkmStatus
	{
		/// <summary>
		/// Successful frame processing
		/// </summary>
		Success = 0,
		/// <summary>
		/// Insufficient memory to complete operation
		/// </summary>
		NoMemory = 1,
		/// <summary>
		/// Source frame address not registered in current Relationship group
		/// </summary>
		UnknownSource = 2,
		/// <summary>
		/// Other error breaking regular workflow
		/// </summary>
		FatalError = 3
	}
}
