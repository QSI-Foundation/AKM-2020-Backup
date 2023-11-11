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
	/// <inheritdoc/>
	public class Program
	{
		private static IServiceProvider _serviceProvider;
		private static IConfiguration _config;

		/// <inheritdoc/>
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
				.WriteTo.File($"C:\\akmLog\\akmWorkerServiceLog{DateTime.Today.ToString("yyyy_MM_dd")}.txt")
				.WriteTo.Console()
				.CreateLogger();

			try
			{
				Log.Information("Starting up the AKM Worker Service");
				CreateHostBuilder(args).Build().Run();
			}
			catch (Exception ex)
			{
				Log.Fatal(ex, "Error running AKM Worker Service: {Message}");
				return;
			}
			finally
			{
				Log.CloseAndFlush();
			}
		}

		/// <inheritdoc/>
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
