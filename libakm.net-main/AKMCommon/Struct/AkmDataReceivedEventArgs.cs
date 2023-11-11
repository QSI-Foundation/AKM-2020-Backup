/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

using System;

namespace AKMCommon.Struct
{
	/// <summary>
	/// Event arguments for data received event. 
	/// </summary>
	public class AkmDataReceivedEventArgs : EventArgs
	{
		/// <summary>
		/// Byte array with transmission content
		/// </summary>
		public byte[] FrameData { get; set; }
		/// <summary>
		/// Relationship Id value 
		/// </summary>
		public short RelationshipId { get; set; }
		/// <summary>
		/// Relationship Node Number
		/// </summary>
		public short SrcAddr { get; set; }
		public short TrgAddr { get; set; }
		public AKMCommon.Enum.AkmEvent AkmEvent { get; set; }
	}
}
