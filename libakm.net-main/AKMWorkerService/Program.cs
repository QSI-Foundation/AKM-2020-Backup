/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

using AKMInterface;
using AKMLogic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System;
using System.IO;

namespace AKMWorkerService
{
	/// <summary>
	/// Service application
	/// </summary>
	public class Program
	{
		private static IServiceProvider _serviceProvider;
		private static IConfiguration _config;

		/// <summary>
		/// Entry point for Service application
		/// </summary>
		/// <param name="args">Optional run arguments</param>
		public static void Main(string[] args)
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

			IConfigurationRoot configuration = builder.Build();

			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Debug()
				.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
				.Enrich.FromLogContext()
				.WriteTo.File($"C:\\akmLog\\akmWorkerServiceLog{DateTime.Today:yyyy_MM_dd}.txt")
				.WriteTo.Console(outputTemplate:"[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
				.CreateLogger();

			try
			{
				Log.Information("Starting up the AKM Worker Service");
				CreateHostBuilder(args).Build().Run();
			}
			catch (OperationCanceledException)
			{
				Log.Information("Service running cancelled.");
			}
			catch (Exception ex)
			{
				Log.Fatal(ex, $"Error running AKM Worker Service: {ex.Message}");
			}
			finally
			{
				Log.CloseAndFlush();
			}
		}

		/// <summary>
		/// Create host process
		/// </summary>
		/// <param name="args">Optional run arguments</param>
		/// <returns>IHostBuilder object</returns>
		public static IHostBuilder CreateHostBuilder(string[] args)
		{
			return Host.CreateDefaultBuilder(args)
				.ConfigureServices((hostContext, services) =>
				{
					_config = hostContext.Configuration;
					services.AddHostedService<Worker>();
					services.AddSingleton<IConfiguration>(hostContext.Configuration);
					services.AddSingleton(typeof(ICLibCalls), typeof(AKMLogic.AkmCLibCall));
					services.AddSingleton(typeof(ICryptography), typeof(AKMLogic.AkmCrypto));
					services.AddScoped(typeof(IKey), _ => new AkmKey(int.Parse(_config.GetSection("AkmAppConfig:DefaultKeySize").Value)));
					services.AddSingleton(typeof(IKeyFactory), _ => new AkmKeyFactory(int.Parse(_config.GetSection("AkmAppConfig:DefaultKeySize").Value)));
					_serviceProvider = services.BuildServiceProvider();
				})
				.UseSerilog();
		}
	}
}
