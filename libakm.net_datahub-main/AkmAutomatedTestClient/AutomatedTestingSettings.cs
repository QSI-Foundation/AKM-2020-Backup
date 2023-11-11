namespace AkmAutomatedTestClient
{
	public class AutomatedTestingSettings
	{
		public bool AlwaysUseFileConfig { get; set; }

		public bool UseRandomDataSize { get; set; }
		public int MinDataPackageSize { get; set; }
		public int MaxDataPackageSize { get; set; }

		public bool ForceShutdown { get; set; }
		public int ShutdownAfterFrameCount { get; set; }

		public int ForcedEventCode { get; set; }
		public int ForcedEventFrameCount { get; set; }
		public int DebugPauseCounter { get; set; }
		public int MessageSendDelayValue { get; set; }
	}
}
