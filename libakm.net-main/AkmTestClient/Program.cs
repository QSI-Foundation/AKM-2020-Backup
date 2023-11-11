/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

using AKMCommon.Struct;
using AKMLogic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace AkmTestClient
{
	class Program
	{
		private const int COMMUNICATION_PORT = 8087;

		private static Microsoft.Extensions.Logging.ILogger _logger;

		static void Main(string[] args)
		{
			CancellationTokenSource cts = new CancellationTokenSource();

			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Debug()
				.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
				.Enrich.FromLogContext()
				.WriteTo.File($"C:\\akmLog\\akmTestClient{DateTime.Today:yyyy_MM_dd}.txt")
				.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
				.CreateLogger();

			var services = new ServiceCollection().AddLogging(builder =>
			{
				builder.SetMinimumLevel(LogLevel.Information);
				builder.AddSerilog(Log.Logger);
			});

			var serviceProvider = services.BuildServiceProvider();
			_logger = serviceProvider.GetService<ILogger<Program>>();

			AkmSetup.Logger = _logger;
			var akmConfig = AkmSetup.AkmAppCfg.FirstOrDefault().Value;
			if (akmConfig == null)
			{
				_logger.LogError("Cannot create AKM application config.");
				return;
			}

			Console.WriteLine("Press 'ctrl+c' to quit");
			Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
			{
				e.Cancel = true;
				cts.Cancel();
			};

			Socket socket;
			var isConnected = ConnectToService(cts, out socket);
			Sender sender = null;
			AkmSenderManager.AddSender(akmConfig.RelationshipId, akmConfig.SelfAddressValue, socket, _logger);
			try
			{
				sender = AkmSenderManager.GetDefaultSender(cts.Token);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error while creating default sender object. {ex.Message}");
				_logger.LogInformation("Application will now close.");

				socket.Close();
				return;
			}
			if (sender == null)
			{
				_logger.LogError($"Error while fetching default sender object.");
				_logger.LogInformation("Application will now close.");

				socket.Close();
				return;
			}

			var receiver = new Receiver(socket, cts.Token, _logger);
			receiver.DataReceived += Receiver_DataReceived;
			receiver.StartReceiving();

			var i = 0;
			var strQuit = "";

			while (!cts.Token.IsCancellationRequested && isConnected)
			{
				var message = $"Sample message number {++i}";
				byte[] messageBytes = Encoding.UTF8.GetBytes(message);

				if (!sender.IsActive) //check
				{
					isConnected = ConnectToService(cts, out socket);
					sender.SetSocket(socket);

					//reset receiver
					receiver = new Receiver(socket, cts.Token, _logger);
					receiver.DataReceived += Receiver_DataReceived;
					receiver.StartReceiving();
				}

#if FORCED_AKM_EVENT
                if (i % FORCED_EVENT_FRAME_COUNTER == 0)
                {
                    sender.SendData(messageBytes, 1, FORCED_AKM_EVENT);
                }
                else
#endif
				{
					sender.SendData(messageBytes, 1);
				}


				_logger.LogDebug($"sent message: {message}");
				Console.WriteLine("(type !q to quit)");

				strQuit = Console.ReadLine();
				if (!string.IsNullOrEmpty(strQuit) && strQuit.ToUpper() == "!Q")
				{
					cts.Cancel();
				}
			}

			socket.Dispose();

			_logger.LogDebug("END");
			Console.ReadLine();
		}

		private static bool ConnectToService(CancellationTokenSource cts, out Socket socket)
		{
			bool isConnected = false;

			socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

			_logger.LogDebug($"Connecting to port {COMMUNICATION_PORT}");
			while (!isConnected && !cts.IsCancellationRequested)
			{
				try
				{
					socket.Connect(new IPEndPoint(IPAddress.Loopback, COMMUNICATION_PORT));
					isConnected = true;
				}
				catch (Exception ex)
				{
					_logger.LogError($"Error while connecting to host: {ex.Message}, trying again in 5 seconds (press ctrl+c to break)");
					Thread.Sleep(5000);
				}
			}

			return isConnected;
		}

		private static void Receiver_DataReceived(object sender, AkmDataReceivedEventArgs e)
		{
			if ((e?.FrameData?.Length ?? 0) > 0)
			{
				var message = Encoding.UTF8.GetString(e.FrameData);
				_logger.LogDebug($"Decrypted Frame data: {message}");
			}
			else
			{
				_logger.LogWarning("Data received event fired with empty frame.");
			}
		}
	}
}
