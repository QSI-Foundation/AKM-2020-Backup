namespace AKMWorkerService
{
	/// <summary>
	/// Helper class used for automated tests setup
	/// </summary>
	public class AutomatedTestingSettings
	{
		/// <summary>
		/// If true then AKM configuration will always be taken from configuration file
		/// </summary>
		public bool AlwaysUseFileConfig { get; set; }

		/// <summary>
		/// Used to indicate that test frames should contain random sized data package
		/// </summary>
		public bool UseRandomDataSize { get; set; }
		/// <summary>
		/// Minimum value of added data package size
		/// </summary>
		public int MinDataPackageSize { get; set; }
		/// <summary>
		/// Maximum value of added data package size
		/// </summary>
		public int MaxDataPackageSize { get; set; }

		/// <summary>
		/// Used to force application termination after given number of frames
		/// </summary>
		public bool ForceShutdown { get; set; }
		/// <summary>
		/// Forced shutdown frame counter
		/// </summary>
		public int ShutdownAfterFrameCount { get; set; }
		
		/// <summary>
		/// Value used for forcing certain AKM Event
		/// </summary>
		public int ForcedEventCode { get; set; }
		/// <summary>
		/// Used for setting how often should the forced AKM event me triggered 
		/// </summary>
		public int ForcedEventFrameCount { get; set; }		
		/// <summary>
		/// Used for forcing a processing pause after every n frames
		/// </summary>
		public int DebugPauseCounter { get; set; }
		/// <summary>
		/// Used to force a delay (in miliseconds) in frame sending
		/// </summary>
		public int MessageSendDelayValue { get; set; }
	}
}
