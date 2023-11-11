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
	/// <inheritdoc/>
	public class Worker : BackgroundService
	{
		private readonly IList<AkmRelationshipPool> _relationshipPools;

		private readonly IList<Thread> _workerThreads;
		private readonly ILogger<Worker> _logger;

		private static CancellationToken ct;
		private static CancellationTokenSource cts;

		private static readonly object _lockNumber = new object();
		private static IDictionary<short, short> _relationshipNumbers;

		private readonly ICryptography _cryptography;

		private static AkmAppConfig cfg;

		/// <summary>
		/// Worker process constructor
		/// </summary>
		/// <param name="logger">ILogger implementation</param>
		/// <param name="crypto">ICryptography implementation that will be used in encryption and decryption</param>
		/// <param name="config">IConfiguration object</param>
		public Worker(ILogger<Worker> logger, ICryptography crypto, IConfiguration config)
		{
			_logger = logger;
			_cryptography = crypto;
			_relationshipPools = new List<AkmRelationshipPool>();
			_workerThreads = new List<Thread>();
			AkmSenderManager.SetDefaultCryptography(_cryptography);
			cts = new CancellationTokenSource();
			ct = cts.Token;

			_relationshipNumbers = new Dictionary<short, short>();

			AkmSetup.Logger = _logger;

			var testSettingsSection = config.GetSection("AutomatedTestingSettings");
			if (testSettingsSection != null)
			{
				var testSettings = testSettingsSection.Get<AutomatedTestingSettings>();
				if (testSettings!= null)
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
			foreach (var thr in _workerThreads)
			{
				cts.Cancel();
				if (thr.IsAlive)
					thr.Join(1000);
			}
			foreach (var rel in _relationshipPools)
			{
				foreach (var cli in rel.RelationshipNodesClients.Values)
				{
					cli.Close();
					cli.Dispose();
				}
			}
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

				_logger.LogDebug($"Starting new Relationship listening thread. { akmAppConfig.RelationshipId}");
				thread.Start(akmAppConfig);
			}

			while (!cancellationToken.IsCancellationRequested)
			{
				await Task.Delay(100);
			}

			await this.StopAsync(cancellationToken);
		}

		private void StartListeningServer(object akmAppConfig)
		{
			cfg = akmAppConfig as AkmAppConfig;
			if (!IPAddress.TryParse(cfg.IPAddress, out IPAddress ipAddr))
				ipAddr = IPAddress.Loopback;

			var server = new TcpListener(new IPEndPoint(ipAddr, cfg.CommunicationPort));

			server.Start();
			while (Thread.CurrentThread.ThreadState != ThreadState.StopRequested && !ct.IsCancellationRequested)
			{
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

				var receiver = new Receiver(_client.Client, ct, _logger);
				receiver.DataReceived += OnDataReceived;
				receiver.NodeNumber = nodeNumber;
				
				AkmSenderManager.AddSender(cfg.RelationshipId, nodeNumber, _client.Client, _logger);

				receiver.StartReceiving(ct);

				if (ct.IsCancellationRequested)
					break;
			}
		}


		private void OnDataReceived(object sender, AkmDataReceivedEventArgs e)
		{
			ProcessBuffer(e.FrameData, e.RelationshipId, e.SrcAddr, e.TrgAddr, e.AkmEvent);
		}

		private void ProcessBuffer(byte[] buffer, short relationshipId, short SrcAddr, short TrgAddr, AKMCommon.Enum.AkmEvent AkmEvent)
		{
			try
			{
				var akmPool = _relationshipPools.FirstOrDefault(x => x.RelationshipID == relationshipId);
				if (akmPool == null)
				{
					_logger.LogError($"Frame's RelationshipId is out of registered scope {relationshipId}");
					return;						
				}


				var message = " EMPTY FRAME ";
				int frameNumber = -1;
				if (buffer != null && buffer.Length > 0)
				{
					frameNumber = BitConverter.ToInt32(buffer.AsSpan(0, 4));
					var textBytes = Encoding.UTF8.GetBytes($"Sample message number {frameNumber}");
					Array.Copy(buffer, 4, textBytes, 0, textBytes.Length);
					message = Encoding.UTF8.GetString(textBytes);

					//check for random size extra data package
					if (buffer.Length > textBytes.Length+4)
					{
						var extraPackage = buffer.Skip(textBytes.Length + 4).ToArray();
						var extraBuffor = new byte[4];
						for (int i = 0; i < extraPackage.Length-4; i+=4)
						{
							Array.Copy(extraPackage, i, extraBuffor, 0, 4);
							var bufferValue = BitConverter.ToInt32(extraBuffor);
							if (bufferValue != frameNumber)
							{
								_logger.LogError($"Invalid byte sequence in additional data package. Expected: {frameNumber}, actual: {bufferValue}");
								break;
							}
						}
					}

				}
				else
				{
					_logger.LogWarning("Data received event processing with empty frame.");
				}

				if (akmPool.NodesFrameCounters.ContainsKey(SrcAddr))
				{
					if (akmPool.NodesFrameCounters[SrcAddr] +1 != frameNumber)
					{
						_logger.LogError($"Frame Counter error from Node {SrcAddr}. Expected {akmPool.NodesFrameCounters[SrcAddr] + 1 }, Received {frameNumber}.");
						akmPool.NodesFrameCounters[SrcAddr] = frameNumber;
					}
					else
					{
						akmPool.NodesFrameCounters[SrcAddr]++;
					}
				}
				else
				{
					if (frameNumber ==1)
						akmPool.NodesFrameCounters.Add(SrcAddr, 1);
					else
					{
						_logger.LogError($"Frame's RelationshipId is out of registered scope {relationshipId}");
						return;
					}
				}

				var senders = AkmSenderManager.GetRelationshipSenders(relationshipId);
				var upperCaseBytes = Encoding.UTF8.GetBytes(message.ToUpper());
				var lowerCaseBytes = Encoding.UTF8.GetBytes(message.ToLower());
				foreach (var sender in senders)
				{
					Array.Copy(upperCaseBytes, 0, buffer, 4, upperCaseBytes.Length);
					sender.Value.SendData(buffer, SrcAddr);
					Array.Copy(lowerCaseBytes, 0, buffer, 4, lowerCaseBytes.Length);
					sender.Value.SendData(buffer, TrgAddr, SrcAddr, AkmEvent);
				}

				_logger.LogDebug($"Decrypted Frame data: {frameNumber} - {message}");
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
	}
}
