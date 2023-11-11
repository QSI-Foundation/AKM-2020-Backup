/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

using System.Collections.Generic;
using System.Net.Sockets;

namespace AKMWorkerService
{
	internal class AkmRelationshipPool
	{
		public short RelationshipID { get; set; }
		public IDictionary<int, TcpClient> RelationshipNodesClients { get; set; }
		public int NodesCount { get; set; }

		public AkmRelationshipPool()
		{
			RelationshipNodesClients = new Dictionary<int, TcpClient>();
		}
	}
}