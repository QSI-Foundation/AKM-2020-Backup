/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

using AKMInterface;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace AKMLogic
{
	/// <summary>
	/// Provides client with Sender objects based on provided RelationshipID value
	/// </summary>
	public class AkmSenderManager
	{
		private static readonly object _lockSenders = new object();
		private static readonly Dictionary<short, Dictionary<short, Sender>> _senders = new Dictionary<short, Dictionary<short, Sender>>();
		private static ICryptography defaultCryptography;

		/// <summary>
		/// Sets default cryptography provider based on given ICryptography implementation. Uses default AkmCrypto class for null parameter
		/// </summary>
		/// <param name="crypto">ICryptography implementation</param>
		public static void SetDefaultCryptography(ICryptography crypto)
		{
			defaultCryptography = crypto ?? new AkmCrypto();
		}

		/// <summary>
		/// Returns Sender object with configuration based on first entry in configuration file
		/// </summary>
		/// <returns>Sender object</returns>
		public static Sender GetDefaultSender(CancellationToken token)
		{
			Sender result = null;
			try
			{
				lock (_lockSenders)
				{
					var akmAppCfg = AkmSetup.AkmAppCfg.Values.FirstOrDefault();
					if (akmAppCfg != null && _senders.ContainsKey(akmAppCfg.RelationshipId))
					{
						result = _senders[akmAppCfg.RelationshipId].FirstOrDefault().Value;
						result.SetCanellationToken(token);
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error while creating default sender: " + ex.Message);
				throw;
			}

			return result;
		}

		/// <summary>
		/// Returns Sender object with configuration created based on RelationshipId value
		/// </summary>
		/// <param name="relatioshipId">Identifies AKM Relationship</param>
		/// <param name="targetAddress">Identifies target to select proper sender for this AKM relationship </param>
		/// <returns>Sender object</returns>
		public static Sender GetSender(short relatioshipId, short targetAddress)
		{
			Sender result = null;
			try
			{
				lock (_lockSenders)
				{
					if (_senders.ContainsKey(targetAddress) && _senders[relatioshipId].ContainsKey(targetAddress))
					{
						result = _senders[relatioshipId][targetAddress];
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error while returning sender for relationship: {relatioshipId}, address {targetAddress}: " + ex.Message);
				throw;
			}

			return result;
		}

		/// <summary>
		/// Returns a Sender object collection for given Relationship
		/// </summary>
		/// <param name="relationshipId">Relationship Identifier</param>
		/// <returns>IDictionary collection or null if given Relationship Id is not registered</returns>
		public static IDictionary<short, Sender> GetRelationshipSenders(short relationshipId)
		{
			if (_senders.ContainsKey(relationshipId))
				return _senders[relationshipId];
			else
				return null;
		}

		/// <summary>
		/// Add new sender object to specific Relationship 
		/// </summary>
		/// <param name="relatioshipId">Relationship identifier</param>
		/// <param name="senderAddress">Numeric value for target node</param>
		/// <param name="socket">Socket object for connected node</param>
		/// <param name="logger">ILogger implementation</param>
		public static void AddSender(short relatioshipId, short senderAddress, Socket socket, ILogger logger)
		{
			if (!_senders.ContainsKey(relatioshipId))
				_senders.Add(relatioshipId, new Dictionary<short, Sender>());
			if (!_senders[relatioshipId].ContainsKey(senderAddress))
			{
				var s = new Sender(relatioshipId);
				s.SetLogger(logger);
				s.SetSocket(socket);
				s.SetCryptography(defaultCryptography);
				_senders[relatioshipId].Add(senderAddress, s);
			}
		}

	}
}
