/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

using AKMCommon.Struct;
using AKMInterface;
using AKMLogic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AKMWorkerService
{
	internal class AkmFrameBuffer
	{
		internal byte[] FrameData { get; set; }
		internal short RelationshipId { get; set; }
		internal int SenderAddress { get; set; }
	}

	/// <inheritdoc/>
	public class Worker : BackgroundService
	{
		private readonly IList<AkmRelationshipPool> _relationshipPools;

		private readonly IList<Thread> _workerThreads;
		private readonly ILogger<Worker> _logger;
		//private CancellationToken _cancellationToken;
		private readonly IList<AkmFrameBuffer> _frameBuffers;

		private static CancellationToken ct;
		private static CancellationTokenSource cts;

		private static readonly object _lockNumber = new object();
		private static IDictionary<short, short> _relationshipNumbers;

		private readonly ICryptography _cryptography;

		/// <inheritdoc/>
		public Worker(ILogger<Worker> logger, ICryptography crypto, IConfiguration config)
		{
			_logger = logger;
			_cryptography = crypto;
			_relationshipPools = new List<AkmRelationshipPool>();
			_workerThreads = new List<Thread>();

			cts = new CancellationTokenSource();
			ct = cts.Token;

			AkmSenderManager.SetDefaultCryptography(_cryptography);
			AkmSetup.Logger = _logger;

			_relationshipNumbers = new Dictionary<short, short>();
			_frameBuffers = new List<AkmFrameBuffer>();

			var testSettingsSection = config.GetSection("AutomatedTestingSettings");
			if (testSettingsSection != null)
			{
				var testSettings = testSettingsSection.Get<AutomatedTestingSettings>();
				if (testSettings != null)
				{
					AkmSetup.ForceFileConfig = testSettings.AlwaysUseFileConfig;
				}
			}
		}

		/// <inheritdoc/>
		public override Task StartAsync(CancellationToken cancellationToken)
		{
			return base.StartAsync(cancellationToken);
		}

		/// <inheritdoc/>
		public override Task StopAsync(CancellationToken cancellationToken)
		{
			return base.StopAsync(cancellationToken);
		}

		/// <inheritdoc/>
		protected override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("AKM Worker service running at: {time}", DateTimeOffset.Now);
			ct = cancellationToken;
			foreach (var akmAppConfig in AkmSetup.AkmAppCfg.Values)
			{
				Thread thread = new Thread(StartListeningServer);
				_workerThreads.Add(thread);

				_logger.LogDebug("Starting new thread to process relationship");
				thread.Start(akmAppConfig);
			}

			while (!cancellationToken.IsCancellationRequested)
			{
				await Task.Delay(100);
			}

			foreach (var thr in _workerThreads)
			{
				cts.Cancel();
				if (thr.IsAlive)
					thr.Join(1000);
			}
			await this.StopAsync(cancellationToken);
		}

		private void StartListeningServer(object akmAppConfig)
		{
			AkmAppConfig cfg = akmAppConfig as AkmAppConfig;
			IPAddress ipAddr;
			if (!IPAddress.TryParse(cfg.IPAddress, out ipAddr))
				ipAddr = IPAddress.Loopback;

			var server = new TcpListener(new IPEndPoint(ipAddr, cfg.CommunicationPort));

			server.Start();
			while (Thread.CurrentThread.ThreadState != ThreadState.StopRequested && !ct.IsCancellationRequested)
			{
				Receiver receiver;
				TcpClient _client = server.AcceptTcpClient();
				var akmPool = _relationshipPools.FirstOrDefault(x => x.RelationshipID == cfg.RelationshipId);
				if (akmPool == null)
				{
					akmPool = new AkmRelationshipPool { NodesCount = cfg.NodesAddresses.Length, RelationshipID = cfg.RelationshipId };
					_relationshipPools.Add(akmPool);
				}

				var nodeNumber = GetNewNodeNumber(cfg.RelationshipId);
				akmPool.RelationshipNodesClients.Add(nodeNumber, _client);

				_logger.LogDebug("Got a connection");

				receiver = new Receiver(_client.Client, ct, _logger);
				receiver.RawDataReceived += OnRawDataReceived;
				receiver.NodeNumber = nodeNumber;


				AkmSenderManager.AddSender(cfg.RelationshipId, nodeNumber, _client.Client, _logger);

				receiver.StartReceiving(ct);

				if (ct.IsCancellationRequested)
					break;
			}
		}

		private void OnRawDataReceived(object sender, AkmDataReceivedEventArgs e)
		{
			if ((e?.FrameData.Length ?? 0) > 0)
			{
				var pool = _relationshipPools.FirstOrDefault(x => x.RelationshipID == e.RelationshipId);
				if (pool == null)
				{
					var newPool = new AkmRelationshipPool();
					newPool.RelationshipID = e.RelationshipId;
					_relationshipPools.Add(newPool);
				}

				//do the actual data processing
				ProcessBuffer(e.FrameData, e.RelationshipId, e.SenderAddress);
			}
		}

		private void ProcessBuffer(byte[] buffer, short relationshipId, int senderAddress)
		{
			try
			{
				var message = Encoding.UTF8.GetString(buffer);

				if (GetNodeCount(relationshipId) == AkmSetup.AkmAppCfg[relationshipId].NodesAddresses.Length)
				{
					var senders = AkmSenderManager.GetRelationshipSenders(relationshipId);
					//check if we have anything buffered for this relationship
					if (_frameBuffers.Any(x => x.RelationshipId == relationshipId))
					{
						foreach (var f in _frameBuffers.Where(x => x.RelationshipId == relationshipId))
						{
							foreach (var sender in senders)
							{
								if (sender.Key != f.SenderAddress)
									sender.Value.SendRawData(f.FrameData);
							}
						}
					}
					_frameBuffers.Clear();

					foreach (var sender in senders)
					{
						if (sender.Key != senderAddress)
							sender.Value.SendRawData(buffer);
					}
				}
				else
				{
					_frameBuffers.Add(new AkmFrameBuffer { FrameData = buffer, RelationshipId = relationshipId, SenderAddress = senderAddress });
				}
			}
			catch (Exception ex)
			{
				_logger.LogError("Error in Buffer Processing: " + ex.Message);
			}
		}

		private short GetNewNodeNumber(short relationshipId)
		{
			lock (_lockNumber)
			{
				if (_relationshipNumbers.ContainsKey(relationshipId))
				{
					_relationshipNumbers[relationshipId]++;
				}
				else
				{
					_relationshipNumbers.Add(relationshipId, 1);
				}

				return _relationshipNumbers[relationshipId];
			}
		}

		private int GetNodeCount(short relationshipId)
		{
			lock (_lockNumber)
			{
				if (_relationshipNumbers.ContainsKey(relationshipId))
				{
					return _relationshipNumbers[relationshipId];
				}
				else
				{
					return 0;
				}
			}
		}
	}
}
