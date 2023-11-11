using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AKMLogic
{
	public class AkmReceiverManager
	{
		private static object _lockReceivers = new object();
		private static IDictionary<int, Receiver> _receivers;

		private static void LoadReceivers()
		{
			foreach (var appcfg in AkmSetup.AkmAppCfg.Values)
			{
				//_receivers.Add(appcfg.RelationshipId, new Receiver(appcfg.RelationshipId));
			}
		}

		public static Receiver GetDefaultReceiver()
		{
			lock (_lockReceivers)
			{
				if (_receivers == null)
				{
					LoadReceivers();
				}
			}

			Receiver result = null;
			var akmAppCfg = AkmSetup.AkmAppCfg.Values.FirstOrDefault();
			if (akmAppCfg != null && _receivers.ContainsKey(akmAppCfg.RelationshipId))
			{
				result = _receivers[akmAppCfg.RelationshipId];
			}

			return result;
		}

		public static Receiver GetReceiver(int relationshipId)
		{
			lock (_lockReceivers)
			{
				if (_receivers == null)
				{
					LoadReceivers();
				}
			}

			Receiver result = null;
			if (_receivers.ContainsKey(relationshipId))
			{
				result = _receivers[relationshipId];
			}

			return result;
		}
	}
}
