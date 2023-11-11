/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

using AKMCommon.Error;
using AKMCommon.Struct;
using AKMInterface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace AKMLogic
{
	/// <summary>
	/// Provides information about defined AKM configuration 
	/// </summary>
	public class AkmSetup
	{
		private static readonly AkmCLibCall _akmCLibCaller = new AkmCLibCall();
		private static readonly AkmKeyFactory _akmKeyFactory = new AkmKeyFactory();
		private static IDictionary<int, AkmAppConfig> _akmAppConfigs;
		private static IDictionary<int, AkmConfiguration> _akmConfigs;
		private static IDictionary<int, AkmRelationship> _akmRelationships;

		/// <summary>
		/// Determines if configuration values should be taken only from config file
		/// </summary>
		public static bool ForceFileConfig { get; set; }

		/// <summary>
		/// Returns environment variable name for storing current configuration based on relationshipId and self address value
		/// </summary>
		/// <param name="relationshipId">RelationshipId value</param>
		/// <param name="selfAddress">Self Node Address value</param>
		/// <returns></returns>
		private static string EnvVariableName(short relationshipId, short selfAddress)
		{
			return $"AKMConfig_{Process.GetCurrentProcess().ProcessName}_{relationshipId}_{selfAddress}";
		}

		/// <summary>
		/// ILogger implementation
		/// </summary>
		public static ILogger Logger { get; set; }

		/// <summary>
		/// Collection of AKM application config structures selected by RelationshipId value
		/// </summary>
		public static IDictionary<int, AkmAppConfig> AkmAppCfg
		{
			get
			{
				if (_akmAppConfigs == null)
				{
					LoadAkmAppConfig();
				}

				return _akmAppConfigs;
			}
		}

		/// <summary>
		/// Collection of AKM Relationship objects responsible for processing AKM Frames
		/// </summary>
		public static IDictionary<int, AkmRelationship> AkmRelationships
		{
			get
			{
				if (_akmRelationships == null)
				{
					CreateAkmRelationships();
				}

				return _akmRelationships;
			}
		}

		private static IDictionary<int, AkmConfiguration> AkmConfigs
		{
			get
			{
				if (_akmConfigs == null)
				{
					CreateAkmConfigs();
				}

				return _akmConfigs;
			}
		}

		private static void LoadAkmAppConfig()
		{
			if (_akmAppConfigs == null)
			{
				_akmAppConfigs = new Dictionary<int, AkmAppConfig>();
			}

			var builder = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.AddEnvironmentVariables();

			IConfigurationRoot configuration = builder.Build();

			var checkPool = int.Parse(configuration.GetSection("AkmCheckPool")?.Value ?? "-1");
			var configSection = configuration.GetSection("AkmAppConfigs");
			AkmAppConfig[] configs = configSection.Get<AkmAppConfig[]>();

			foreach (var cfg in configs)
			{
				if (!_akmAppConfigs.ContainsKey(cfg.RelationshipId))
				{
					_akmAppConfigs.Add(cfg.RelationshipId, cfg);

					if (!TryLoadConfigFromEnvironment(cfg.RelationshipId, cfg.SelfAddressValue))
						SaveConfigAsEnvVariable(cfg.RelationshipId, cfg.SelfAddressValue);
				}
				else
					throw new AkmError(AKMCommon.Enum.AkmStatus.FatalError);
			}

			if (!IsConfigValid(_akmAppConfigs, checkPool))
			{

				throw new AkmError(AKMCommon.Enum.AkmStatus.FatalError, "Error while loading AKM configuration.");
			}


		}

		private static void CreateAkmConfigs()
		{
			if (_akmConfigs == null)
			{
				_akmConfigs = new Dictionary<int, AkmConfiguration>(AkmAppCfg.Count);
			}
			foreach (var relId in AkmAppCfg.Keys)
			{
				AkmConfigs.Add(relId, CreateAkmConfig(AkmAppCfg[relId]));
			}
		}

		private static void CreateAkmRelationships()
		{
			if (_akmRelationships == null)
			{
				_akmRelationships = new Dictionary<int, AkmRelationship>(AkmAppCfg.Count);
			}
			foreach (var relId in AkmAppCfg.Keys)
			{
				AkmRelationships.Add(relId, CreateAkmRelationship(relId));
			}
		}

		private static AkmConfiguration CreateAkmConfig(AkmAppConfig appCfg)
		{
			AkmConfiguration akmConfig = new AkmConfiguration
			{
				cfgParams = new AkmConfigParams()
			};

			byte[] selfAddress = BitConverter.GetBytes(appCfg.SelfAddressValue);
			byte[] expandedNodes = new byte[appCfg.NodesAddresses.Length * sizeof(short)];

			for (int i = 0; i < appCfg.NodesAddresses.Length; i++)
			{
				Array.Copy(BitConverter.GetBytes(appCfg.NodesAddresses[i]), 0, expandedNodes, i * (sizeof(short)), sizeof(short));
			}

			//I know it looks bad, but for now we need to make sure PDV is the same across nodes and eventuall generation method is unknown
			if (appCfg.PDV == null || appCfg.PDV.Length == 0)
			{
				appCfg.PDV = new byte[128];

				for (byte i = 0; i < 128; i++)
				{
					appCfg.PDV[i] = i;
				}
			}


			IntPtr umPointerNode = Marshal.AllocHGlobal(expandedNodes.Length);
			IntPtr umPointerSelf = Marshal.AllocHGlobal(selfAddress.Length);
			IntPtr umPointerPdv = Marshal.AllocHGlobal(appCfg.PDV.Length);

			Marshal.Copy(expandedNodes, 0, umPointerNode, expandedNodes.Length);
			Marshal.Copy(selfAddress, 0, umPointerSelf, selfAddress.Length);
			Marshal.Copy(appCfg.PDV, 0, umPointerPdv, appCfg.PDV.Length);

			akmConfig.nodeAddresses = umPointerNode;
			akmConfig.selfNodeAddress = umPointerSelf;
			akmConfig.pdv = umPointerPdv;
			akmConfig.cfgParams.SK = appCfg.DefaultKeySize;//key size (bytes)
			akmConfig.cfgParams.SRNA = appCfg.FrameSchema.SourceAddress_Length; //node address size (bytes)
			akmConfig.cfgParams.N = (ushort)appCfg.NodesAddresses.Length;
			akmConfig.cfgParams.NNRT = appCfg.AkmConfigParameters.NNRT;
			akmConfig.cfgParams.NSET = appCfg.AkmConfigParameters.NSET;
			akmConfig.cfgParams.FBSET = appCfg.AkmConfigParameters.FBSET;
			akmConfig.cfgParams.FSSET = appCfg.AkmConfigParameters.FSSET;
			//setup seed values
			akmConfig.cfgParams.CSS = appCfg.AkmConfigParameters.CSS;
			akmConfig.cfgParams.EFSS = appCfg.AkmConfigParameters.EFSS;
			akmConfig.cfgParams.FSS = appCfg.AkmConfigParameters.FSS;
			akmConfig.cfgParams.NFSS = appCfg.AkmConfigParameters.NFSS;
			akmConfig.cfgParams.NSFSS = appCfg.AkmConfigParameters.NSFSS;
			akmConfig.cfgParams.NSS = appCfg.AkmConfigParameters.NSS;
			akmConfig.cfgParams.SFSS = appCfg.AkmConfigParameters.SFSS;


			return akmConfig;
		}
		private static IKey[] CreateInitialKeys(int relationshipId)
		{
			if (AkmAppCfg[relationshipId].InitialKeys == null || AkmAppCfg[relationshipId].InitialKeys.Length == 0)
			{
				var key1 = new AkmKey(32); key1.SetKeyFromString("6v9y$B&E)H+MbQeThWmZq4t7w!z%C*F-");
				var key2 = new AkmKey(32); key2.SetKeyFromString("z$C&F)H@McQfTjWnZr4u7x!A%D*G-KaN");
				var key3 = new AkmKey(32); key3.SetKeyFromString("6v9y$B&E)H+MbQeMhWmZq4t7w!z%C*Fo");
				var key4 = new AkmKey(32); key4.SetKeyFromString("z$C&F)H@McQfTjWaZr4u7x!A%D*G-Kat");

				return new IKey[] { key1, key2, key3, key4 };
			}
			else
			{
				var keys = new List<IKey>(AkmAppCfg[relationshipId].InitialKeys.Length);
				foreach (var k in AkmAppCfg[relationshipId].InitialKeys)
				{
					var key = new AkmKey(AkmAppCfg[relationshipId].DefaultKeySize);
					key.SetKeyFromBase64String(k.InitialKey);
					keys.Add(key);
				}
				return keys.ToArray();
			}
		}
		private static AkmRelationship CreateAkmRelationship(int relationshipId)
		{
			var initialKeys = CreateInitialKeys(relationshipId);
			var akmConfig = AkmConfigs[relationshipId];

			return new AkmRelationship(Logger, _akmCLibCaller, _akmKeyFactory, initialKeys, ref akmConfig);
		}

		internal static void UpdateAkmConfiguration(short relationshipId)
		{
			if (AkmRelationships.ContainsKey(relationshipId) && AkmAppCfg.ContainsKey(relationshipId))
			{
				var akmAppConfig = AkmAppCfg[relationshipId];
				var relationship = AkmRelationships[relationshipId];

				byte[] currentPdv;
				byte[] currentNodes = new byte[akmAppConfig.NodesAddresses.Length * sizeof(short)]; //this needs to be set to proper size for gonfig read
				byte[] selfAddress = new byte[akmAppConfig.FrameSchema.SourceAddress_Length];//this needs to be set to proper size for gonfig read


				var configParams = relationship.GetCurrentAKMConfig(out currentPdv, out currentNodes, out selfAddress);
				var akmKeys = relationship.GetKeys();

				//for dynamic relationships you may wish to store current nodes and self address as well
				akmAppConfig.AkmConfigParameters = configParams;
				akmAppConfig.PDV = currentPdv;
				akmAppConfig.InitialKeys = new AkmConfigKeyString[akmKeys.Length];

				for (int i = 0; i < akmAppConfig.InitialKeys.Length; i++)
				{
					akmAppConfig.InitialKeys[i] = new AkmConfigKeyString { InitialKey = akmKeys[i].KeyAsBase64String };
				}

				var jsonString = JsonConvert.SerializeObject(AkmAppCfg);

				Environment.SetEnvironmentVariable(EnvVariableName(relationshipId, akmAppConfig.SelfAddressValue), jsonString, EnvironmentVariableTarget.User);

				Logger.LogDebug("Saved all AKM app config as env variable");
			}
			else
			{
				Logger.LogWarning($"Cannot find required Relationship #{relationshipId}");
			}
		}

		internal static void SaveConfigAsEnvVariable(short relationshipId, short selfAddressId)
		{
			var jsonString = JsonConvert.SerializeObject(AkmAppCfg);
			Environment.SetEnvironmentVariable(EnvVariableName(relationshipId, selfAddressId), jsonString, EnvironmentVariableTarget.User);
			Logger.LogDebug("Saved all AKM app config as env variable");
		}

		private static bool TryLoadConfigFromEnvironment(short relationshipId, short selfAddressId)
		{
			if (ForceFileConfig)
				return false;

			var result = false;
			string configJson = Environment.GetEnvironmentVariable(EnvVariableName(relationshipId, selfAddressId), EnvironmentVariableTarget.User);
			if (!string.IsNullOrEmpty(configJson))
			{
				_akmAppConfigs = JsonConvert.DeserializeObject<IDictionary<int, AkmAppConfig>>(configJson);
				result = true;
			}
			Logger.LogDebug($"Env config load result:{result}");
			return result;

		}

		private static bool IsConfigValid(IDictionary<int, AkmAppConfig> configs, int checkPool)
		{
			bool isValid = true;

			foreach (var cfg in configs.Values)
			{
				foreach (var k in cfg.InitialKeys)
				{
					isValid &= (Convert.FromBase64String(k.InitialKey).Length == cfg.DefaultKeySize);
				}

				isValid &= (cfg.FrameSchema.RelationshipId_Index + cfg.FrameSchema.RelationshipId_Length) <= cfg.FrameSchema.SourceAddress_Index;
				isValid &= (cfg.FrameSchema.SourceAddress_Index + cfg.FrameSchema.SourceAddress_Length) <= cfg.FrameSchema.TargetAddress_Index;
				isValid &= (cfg.FrameSchema.TargetAddress_Index + cfg.FrameSchema.TargetAddress_Length) <= cfg.FrameSchema.AkmEvent_Index;
				isValid &= (cfg.FrameSchema.AkmEvent_Index + cfg.FrameSchema.AkmEvent_Length) <= cfg.FrameSchema.AkmDataStart_Index;
			}
			return isValid;
		}
	}
}