/*	
 *  Copyright (C) <2020>  OlympusSky Technologies S.A.
 *
 *	libAKMC is an implementation of AKM Key Management System
 *  written in C programming language.
 *
 *  This file is part of libAKMC Project and can not be copied
 *  and/or distributed without  the express permission of
 *  OlympusSky Technologies S.A.
 *
 */
 
using System.Collections.Generic;
using System.Net.Sockets;

namespace AKMWorkerService
{
	internal class AkmRelationshipPool
	{
		public short RelationshipID { get; set; }
		public IDictionary<int, TcpClient> RelationshipNodesClients { get; set; }
		public IDictionary<int, int> NodesFrameCounters { get; set; }
		public int NodesCount { get; set; }

		public AkmRelationshipPool()
		{
			RelationshipNodesClients = new Dictionary<int, TcpClient>();
			NodesFrameCounters = new Dictionary<int, int>();
		}
	}
}
