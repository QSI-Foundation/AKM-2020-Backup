using System;
using System.Collections.Generic;
using System.Text;

namespace AKMCommon.Enum
{
	public static class AkmFrameDefinition
	{
		public const int RELATIONSHIP_ID_START_IDX = 0;
		public const int RELATIONSHIP_ID_LENGTH = 2;

		public const int SOURCE_ADDRESS_START_IDX = 2;
		public const int SOURCE_ADDRESS_LENGTH = 2;

		public const int TARGET_ADDRESS_START_IDX = 4;
		public const int TARGET_ADDRESS_LENGTH = 2;

		public const int AKM_EVENT_START_IDX = 6;
		public const int AKM_EVENT_LENGTH = 1;

		public const int AKM_CONTENT_START_IDX = 7;
	}
}
