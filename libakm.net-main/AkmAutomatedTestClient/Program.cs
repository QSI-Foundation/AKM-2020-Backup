/*	
 * Copyright(C) <2020>  OlympusSky Technologies S.A.
 *
 * libAKM.NET is an implementation of AKM Key Management System
 * written in C# programming language.
 *
 * This file is part of libAKM.NET Project and can not be copied
 * and/or distributed without the express permission of
 * OlympusSky Technologies S.A.
 * 
 */

using AKMCommon.Enum;
using AKMCommon.Struct;
using AKMLogic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace AkmAutomatedTestClient
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
				.WriteTo.File($"C:\\akmLog\\akmAutomatedTestClientLog{DateTime.Today:yyyy_MM_dd}.txt")
				.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
				.CreateLogger();

			var services = new ServiceCollection().AddLogging(builder =>
			{
				builder.SetMinimumLevel(LogLevel.Information);
				builder.AddSerilog(Log.Logger);
			});

			var serviceProvider = services.BuildServiceProvider();
			_logger = serviceProvider.GetService<ILogger<Program>>();

			var builder = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);


			IConfigurationRoot configuration = builder.Build();

			var testSettingsSection = configuration.GetSection("AutomatedTestingSettings");
			var testSettings = testSettingsSection.Get<AutomatedTestingSettings>();

			AkmSetup.Logger = _logger;
			AkmSetup.ForceFileConfig = testSettings.AlwaysUseFileConfig;

			var akmConfig = AkmSetup.AkmAppCfg.FirstOrDefault().Value;
			if (akmConfig == null)
			{
				_logger.LogError("Cannot create AKM application config.");
				return;
			}

			Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
			{
				e.Cancel = true;
				Environment.Exit(0);
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

			var frameCount = 0;
			var rng = new Random(DateTime.Now.Millisecond);

			while (!cts.Token.IsCancellationRequested && isConnected)
			{
				var message = $"Sample message number {++frameCount}";
				var textBytes = new List<byte>();
				textBytes.AddRange(BitConverter.GetBytes(frameCount));
				textBytes.AddRange(Encoding.UTF8.GetBytes(message));
				byte[] messageBytes = textBytes.ToArray();

				if (testSettings.UseRandomDataSize)
				{
					var addedSize = rng.Next(testSettings.MinDataPackageSize, testSettings.MaxDataPackageSize);
					Array.Resize<byte>(ref messageBytes, messageBytes.Length + addedSize);
					var extraPackage = new byte[addedSize];
					var frameCounterBytes = BitConverter.GetBytes(frameCount);
					for (int i = 0; i < addedSize - 4; i += 4)
					{
						Array.Copy(frameCounterBytes, 0, extraPackage, i, 4);
					}
					Array.Copy(extraPackage, 0, messageBytes, messageBytes.Length - addedSize, addedSize);
				}

				if (!sender.IsActive) //check
				{
					isConnected = ConnectToService(cts, out socket);
					sender.SetSocket(socket);

					//reset receiver
					receiver = new Receiver(socket, cts.Token, _logger);
					receiver.DataReceived += Receiver_DataReceived;
					receiver.StartReceiving();
				}

				if (testSettings.ForcedEventCode > 0 && frameCount % testSettings.ForcedEventFrameCount == 0)
				{
					_logger.LogDebug($"---=== EVENT ===---");
					sender.SendData(messageBytes, 1, (AkmEvent)testSettings.ForcedEventCode);
				}
				else
				{
					sender.SendData(messageBytes, 1);
				}

				_logger.LogDebug($"sent message: {message}");

				if (testSettings.DebugPauseCounter > 0 && frameCount % testSettings.DebugPauseCounter == 0)
				{
					Console.WriteLine("debug pause, check log");
					Console.ReadLine();
				}

				if (testSettings.ForceShutdown && testSettings.ShutdownAfterFrameCount == frameCount)
					break;

				if (testSettings.MessageSendDelayValue > 0)
				{
					Thread.Sleep(testSettings.MessageSendDelayValue);
				}
			}

			_logger.LogDebug("END");

			Console.ReadLine();
			cts.Cancel();
			socket.Dispose();
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
				string message;
				int frameNumber;
				if (e?.FrameData != null && e?.FrameData.Length > 0)
				{
					frameNumber = BitConverter.ToInt32(e.FrameData.AsSpan(0, 4));
					var textBytes = Encoding.UTF8.GetBytes($"Sample message number {frameNumber}");
					Array.Copy(e?.FrameData, 4, textBytes, 0, textBytes.Length);
					message = Encoding.UTF8.GetString(textBytes);

					//check for random size extra data package
					if (e?.FrameData.Length > textBytes.Length + 4)
					{
						var extraPackage = e?.FrameData.Skip(textBytes.Length + 4).ToArray();
						var extraBuffor = new byte[4];
						for (int i = 0; i < extraPackage.Length - 4; i += 4)
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

					_logger.LogDebug($"Decrypted Frame data: {frameNumber} - {message}");
				}
			}
			else
			{
				_logger.LogWarning("Data received event fired with empty frame.");
			}
		}
	}
}
