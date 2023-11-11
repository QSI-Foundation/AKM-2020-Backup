/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

using AKMCommon.Enum;
using AKMCommon.Error;
using AKMCommon.Struct;
using AKMInterface;
using Microsoft.Extensions.Logging;
using System;
using System.Runtime.InteropServices;
using System.Timers;

namespace AKMLogic
{
	/// <summary>
	/// Main processing class
	/// </summary>
	public class AkmRelationship : IDisposable
	{
		/// <summary>
		/// Assumed key count
		/// </summary>
		public const int KEY_COUNT = 4;
		/// <summary>
		/// PDV structure length
		/// </summary>
		public const int PDV_Size = 128;

		private IntPtr _relationship = IntPtr.Zero;
		private readonly IKeyFactory _keyFactory;
		private readonly int _keyLength;
		private readonly IKey[] _keys;
		private int _encKeyIdx, _decKeyIdx;
		private Timer _timer;
		private AkmEvent? _currentAkmEvent;
		private AkmConfiguration _akmConfig;
		private readonly ILogger _logger;
		private bool _isConfigurationUpdated;

		private readonly object _procLock = new object();
		private readonly object _confLock = new object();


		/// <summary>
		/// Returns collection of currently used encryption keys
		/// </summary>
		/// <returns>IKey array with currently active keys</returns>
		public IKey[] GetKeys()
		{
			return _keys;
		}

		/// <summary>
		/// Gets current active AKM Event
		/// </summary>
		private AkmEvent? CurrentAkmEvent
		{
			get
			{
				lock (_procLock)
				{
					return _currentAkmEvent;
				}
			}
			set
			{
				_currentAkmEvent = value;
			}
		}

		/// <summary>
		/// Flag set when processing AKM frame results in changing current AKM configuratin
		/// </summary>
		public bool IsConfigurationUpdated
		{
			get
			{
				lock (_confLock)
				{
					return _isConfigurationUpdated;
				}
			}
			set
			{
				lock (_confLock)
				{
					_isConfigurationUpdated = value;
				}
			}
		}

		/// <summary>
		/// AkmRelationship constructor
		/// </summary>
		/// <param name="logger">ILogger interface implementation</param>
		/// <param name="cLibCaller">IClibCalls interface implementation</param>
		/// <param name="keyFactory">IKEyFactory implementation</param>
		/// <param name="initialKeys">Array of initial keys</param>
		/// <param name="config">reference to AkmConfiguration structure</param>
		public AkmRelationship(ILogger logger, ICLibCalls cLibCaller, IKeyFactory keyFactory, IKey[] initialKeys, ref AkmConfiguration config)
		{
			if (cLibCaller == null)
			{
				throw new ArgumentNullException("cLibCaller cannot be null.");
			}
			if (keyFactory == null)
			{
				throw new ArgumentNullException("keyFactory cannot be null.");
			}
			if (initialKeys == null || initialKeys.Length != KEY_COUNT)
			{
				throw new ArgumentNullException($"initialKeys cannot be null and must cointain {KEY_COUNT} keys.");
			}

			_timer = new Timer
			{
				AutoReset = false
			};
			_logger = logger;
			_timer.Elapsed += Timer_Elapsed;
			_keyFactory = keyFactory;
			_keys = initialKeys;
			_keyLength = initialKeys[0].KeyLength;
			_akmConfig = config;
			AkmStatus status = Init(ref _akmConfig);
			if (status != AkmStatus.Success)
			{
				_timer.Dispose();
				_timer = null;
				throw new AkmError(status);
			}
		}

		private void Timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			TimerTimedOut();
		}

		private AkmProcessCtx PrepareProcessCtx()
		{
			AkmProcessCtx ctx = new AkmProcessCtx
			{
				relationship = _relationship,
				time_ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
			};
			return ctx;
		}

		private AkmStatus Init(ref AkmConfiguration config)
		{
			AkmProcessCtx ctx = PrepareProcessCtx();
			AkmStatus status = ICLibCalls.AKMInit(ref ctx, ref config);
			_relationship = ctx.relationship;

			if (status == AkmStatus.Success)
			{
				IDecryptedFrame dummy = null;
				status = GenericProcess(ref ctx, null, ref dummy);
			}
			if (status != AkmStatus.Success)
			{
				Free();
			}
			return status;
		}

		private void Free()
		{
			ICLibCalls.AKMFree(_relationship);
			_relationship = IntPtr.Zero;
		}


		private AkmStatus TimerTimedOut()
		{
			AkmProcessCtx ctx = PrepareProcessCtx();
			ctx.akmEvent = AkmEvent.TimeOut;
			IDecryptedFrame dummy = null;
			return GenericProcess(ref ctx, null, ref dummy);
		}

		private AkmStatus GenericProcess(ref AkmProcessCtx ctx, IEncryptedFrame encFrame, ref IDecryptedFrame decFrame)
		{
			lock (_procLock)
			{
				return GenericProcessInternal(ref ctx, encFrame, ref decFrame);
			}
		}

		private AkmStatus GenericProcessInternal(ref AkmProcessCtx ctx, IEncryptedFrame encFrame, ref IDecryptedFrame decFrame)
		{
			IsConfigurationUpdated = false;
			byte[] srcAddr = decFrame?.GetSourceAddressAsByteArray();
			GCHandle? hSrcAddr = null;
			ctx.srcAddr = IntPtr.Zero;
			if (srcAddr != null)
			{
				hSrcAddr = GCHandle.Alloc(srcAddr, GCHandleType.Pinned);
				ctx.srcAddr = hSrcAddr.Value.AddrOfPinnedObject();
			}
			while (true)
			{
				ICLibCalls.AKMProcess(ref ctx);
				if (hSrcAddr.HasValue && ctx.srcAddr == IntPtr.Zero)
				{
					hSrcAddr.Value.Free();
					hSrcAddr = null;
				}
#if DEBUG
				//_logger.LogDebug($"AKM OpCode: {ctx.cmd.opcode}");
#endif
				switch (ctx.cmd.opcode)
				{
					case AkmCmdOpCode.Return:
						return (AkmStatus)ctx.cmd.p1;
					case AkmCmdOpCode.SetSendEvent:
						if (ctx.cmd.p1 != 0)
						{
							IsConfigurationUpdated = IsConfigurationUpdated || (((short)(CurrentAkmEvent ?? AkmEvent.None) > 0) && (ctx.cmd.p2 == 0));
							CurrentAkmEvent = (AkmEvent)ctx.cmd.p2;
						}
						else
							CurrentAkmEvent = null;
						break;
					case AkmCmdOpCode.SetKey:
						_keys[ctx.cmd.p1] = _keyFactory.Create(ctx.cmd.data, _keyLength);
						break;
					case AkmCmdOpCode.ResetKey:
						_keys[ctx.cmd.p1] = null;
						break;
					case AkmCmdOpCode.MoveKey:
						_keys[ctx.cmd.p1] = _keys[ctx.cmd.p2];
						_keys[ctx.cmd.p2] = null;
						break;
					case AkmCmdOpCode.UseKeys:
						_encKeyIdx = ctx.cmd.p1;
						_decKeyIdx = ctx.cmd.p2;
						break;
					case AkmCmdOpCode.RetryDec:
						decFrame = encFrame?.Decrypt(_keys[ctx.cmd.p1]);
						ctx.akmEvent = decFrame != null ? decFrame.FrameEvent : AkmEvent.CannotDecrypt;
						srcAddr = decFrame?.GetSourceAddressAsByteArray();
						hSrcAddr?.Free();
						hSrcAddr = null;
						ctx.srcAddr = IntPtr.Zero;
						if (srcAddr != null)
						{
							hSrcAddr = GCHandle.Alloc(srcAddr, GCHandleType.Pinned);
							ctx.srcAddr = hSrcAddr.Value.AddrOfPinnedObject();
						}
						break;
					case AkmCmdOpCode.SetTimer:
						SetTimer(Marshal.ReadInt64(ctx.cmd.data));
						break;
					case AkmCmdOpCode.ResetTimer:
						ResetTimer();
						break;
				}
			}

		}

		private void SetTimer(long t)
		{
			_timer.Stop();
			var n = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			_timer.Interval = t - n;
			_timer.Start();
		}

		private void ResetTimer()
		{
			_timer.Stop();
		}

		internal AkmStatus LocalSEI()
		{
			AkmProcessCtx ctx = PrepareProcessCtx();
			ctx.akmEvent = AkmEvent.LocalSEI;
			IDecryptedFrame dummy = null;
			return GenericProcess(ref ctx, null, ref dummy);
		}


		/// <summary>
		/// Processes IEncryptedFrame object to create IDecryptedFrame instance
		/// </summary>
		/// <param name="encFrame"></param>
		/// <param name="decFrame"></param>
		/// <returns>AKM status as frame processing result</returns>
		public AkmStatus ProcessFrame(IEncryptedFrame encFrame, out IDecryptedFrame decFrame)
		{
			AkmProcessCtx ctx = PrepareProcessCtx();
			AkmEvent evt;
#if DEBUG
			_logger.LogDebug($"RECV - KEY: {_decKeyIdx} [{BitConverter.ToString(_keys[_decKeyIdx].KeyAsBytes)}] ");
#endif
			decFrame = encFrame?.Decrypt(_keys[_decKeyIdx]);
			evt = ctx.akmEvent = decFrame != null ? decFrame.FrameEvent : AkmEvent.CannotDecrypt;

			var result = GenericProcess(ref ctx, encFrame, ref decFrame);
#if DEBUG
			if (decFrame != null)
			{
				byte[] srcAddr = decFrame.GetSourceAddressAsByteArray(); Array.Reverse(srcAddr);
				byte[] trgAddr = decFrame.GetTargetAddressAsByteArray(); Array.Reverse(trgAddr);

				_logger.LogDebug($"RECV - RID: {decFrame.RelationshipId} ");
				_logger.LogDebug($"RECV - SRC: {BitConverter.ToString(srcAddr).Replace("-", string.Empty)} ");
				_logger.LogDebug($"RECV - TRG: {BitConverter.ToString(trgAddr).Replace("-", string.Empty)} ");
				_logger.LogDebug($"RECV - EVT: {evt} ({(int)evt})");
				_logger.LogDebug($"Relationship Event: {_currentAkmEvent} ({(int)_currentAkmEvent})  ");
			}

#endif
			return result;
		}
		/// <summary>
		/// Creates IEncryptedFrame from IDecryptedFrame object allowing for forced AKM event
		/// </summary>
		/// <param name="decFrame">IDecryptedFrame object</param>
		/// <param name="forcedAkmEvent">Optional value for forcing AKM event</param>
		/// <returns>IEncryptedFrame object</returns>
		public IEncryptedFrame PrepareFrame(IDecryptedFrame decFrame, AkmEvent? forcedAkmEvent = null)
		{
			AkmEvent AKMEvent = CurrentAkmEvent ?? AkmEvent.None;
			if (forcedAkmEvent.HasValue)
			{
				LocalSEI();
				AKMEvent = AKMCommon.Enum.AkmEvent.RecvSEI;
			}
			AkmProcessCtx ctx = PrepareProcessCtx();
			decFrame.SetFrameEvent(AKMEvent);
#if DEBUG
			byte[] srcAddr = decFrame.GetSourceAddressAsByteArray(); Array.Reverse(srcAddr);
			byte[] trgAddr = decFrame.GetTargetAddressAsByteArray(); Array.Reverse(trgAddr);

			_logger.LogDebug($"SEND - KEY: {_encKeyIdx} [{BitConverter.ToString(_keys[_encKeyIdx].KeyAsBytes)}] ");
			_logger.LogDebug($"SEND - RID: {decFrame.RelationshipId} ");
			_logger.LogDebug($"SEND - SRC: {BitConverter.ToString(srcAddr).Replace("-", string.Empty)} ");
			_logger.LogDebug($"SEND - TRG: {BitConverter.ToString(trgAddr).Replace("-", string.Empty)} ");
			_logger.LogDebug($"SEND - Event: {AKMEvent} ({(int)AKMEvent})  ");
#endif
			var encFrame = decFrame?.Encrypt(_keys[_encKeyIdx]);
			return encFrame;
		}

		public IEncryptedFrame PrepareFrameWithEvent(IDecryptedFrame decFrame, AkmEvent AKMEvent)
		{
			AkmProcessCtx ctx = PrepareProcessCtx();
			decFrame.SetFrameEvent(AKMEvent);
#if DEBUG
			byte[] srcAddr = decFrame.GetSourceAddressAsByteArray(); Array.Reverse(srcAddr);
			byte[] trgAddr = decFrame.GetTargetAddressAsByteArray(); Array.Reverse(trgAddr);

			_logger.LogDebug($"SEND - KEY: {_encKeyIdx} [{BitConverter.ToString(_keys[_encKeyIdx].KeyAsBytes)}] ");
			_logger.LogDebug($"SEND - RID: {decFrame.RelationshipId} ");
			_logger.LogDebug($"SEND - SRC: {BitConverter.ToString(srcAddr).Replace("-", string.Empty)} ");
			_logger.LogDebug($"SEND - TRG: {BitConverter.ToString(trgAddr).Replace("-", string.Empty)} ");
			_logger.LogDebug($"SEND - Event: {AKMEvent} ({(int)AKMEvent})  ");
#endif
			var encFrame = decFrame?.Encrypt(_keys[_encKeyIdx]);
			return encFrame;
		}

		/// <summary>
		/// Prepares IEncrypted frame object based on provided IDecrypted frame
		/// </summary>
		/// <param name="decFrame">IDecrypted frame instance</param>
		/// <param name="encFrame">output with creaetd IEncryptedFrame</param>
		/// <returns>AKM status</returns>
		public AkmStatus PrepareFrame(IDecryptedFrame decFrame, out IEncryptedFrame encFrame)
		{
			return PrepareFrame(decFrame, out encFrame, null);
		}
		/// <summary>
		/// Prepares IEncrypted frame object based on provided IDecrypted frame with option to enforce AKM Event
		/// </summary>
		/// <param name="decFrame">IDecrypted frame instance</param>
		/// <param name="encFrame">output with creaetd IEncryptedFrame</param>
		/// <param name="forcedAkmEvent">forced AkmEvent value</param>
		/// <returns>AKM status</returns>
		public AkmStatus PrepareFrame(IDecryptedFrame decFrame, out IEncryptedFrame encFrame, AkmEvent? forcedAkmEvent)
		{
			decFrame.SetFrameEvent(forcedAkmEvent ?? (CurrentAkmEvent ?? AkmEvent.None));
			encFrame = decFrame?.Encrypt(_keys[_encKeyIdx]);
			return encFrame != null ? AkmStatus.Success : AkmStatus.FatalError;
		}

		/// <inheritdoc/>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				_timer?.Dispose();
				_timer = null;
			}
			Free();
		}

		/// <summary>
		/// Destructor
		/// </summary>
		~AkmRelationship()
		{
			Dispose(false);
		}
		/// <inheritdoc/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Returns currenlty used AKM config for this Relationship
		/// </summary>
		/// <param name="pdv">byte array with PDV</param>
		/// <param name="nodeAddresses">byte array with Relationship nodes</param>
		/// <param name="selfNodeAddr">byte array with own node address</param>
		internal AkmConfigParams GetCurrentAKMConfig(out byte[] pdv, out byte[] nodeAddresses, out byte[] selfNodeAddr)
		{
			AkmConfigParams akmConfigParams;
			lock (_procLock)
			{
				var akmConfig = new AkmConfiguration();
				ICLibCalls.AKMGetConfig(_relationship, ref akmConfig);
				int addrListLen = akmConfig.cfgParams.N * akmConfig.cfgParams.SRNA;
				akmConfigParams = akmConfig.cfgParams;
				pdv = new byte[PDV_Size];
				nodeAddresses = new byte[addrListLen];
				selfNodeAddr = new byte[akmConfig.cfgParams.SRNA];
				GCHandle hPdv = GCHandle.Alloc(pdv, GCHandleType.Pinned);
				GCHandle hNodeAddresses = GCHandle.Alloc(nodeAddresses, GCHandleType.Pinned);
				GCHandle hSelfNodeAddr = GCHandle.Alloc(selfNodeAddr, GCHandleType.Pinned);
				akmConfig.pdv = hPdv.AddrOfPinnedObject();
				akmConfig.nodeAddresses = hNodeAddresses.AddrOfPinnedObject();
				akmConfig.selfNodeAddress = hSelfNodeAddr.AddrOfPinnedObject();
				ICLibCalls.AKMGetConfig(_relationship, ref akmConfig);
				hPdv.Free();
				hNodeAddresses.Free();
				hSelfNodeAddr.Free();
			}

			return akmConfigParams;
		}
	}
}
