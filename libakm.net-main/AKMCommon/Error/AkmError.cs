/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

using AKMCommon.Enum;
using System;

namespace AKMCommon.Error
{
	/// <summary>
	/// AKM error class that allows storing AKM Status value
	/// </summary>
	public class AkmError : Exception
	{
		/// <summary>
		/// AKM Status assigned with error
		/// </summary>
		public readonly AkmStatus status;

		/// <summary>
		/// New AKM Error with given AKM Status value
		/// </summary>
		/// <param name="status">AKM Status enum value</param>
		public AkmError(AkmStatus status) : this(status, string.Empty)
		{

		}
		/// <summary>
		/// New AKM Error with given AKM Status value and custom exception message
		/// </summary>
		/// <param name="status">>AKM Status enum value</param>
		/// <param name="message">Custom exception message</param>
		public AkmError(AkmStatus status, string message) : base(message)
		{
			this.status = status;
		}
	}
}
