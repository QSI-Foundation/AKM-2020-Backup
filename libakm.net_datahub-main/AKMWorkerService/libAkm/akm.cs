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
 
using System;
using System.Runtime.InteropServices;
using System.Timers;

namespace mlibakm
{
    public enum Event
    {
        None = -1,
        RecvSE = 0,
        RecvSEI = 1,
        RecvSEC = 2,
        RecvSEF = 3,
        CannotDecrypt = 4,
        TimeOut = 5,
    }

    public enum Status
    {
        Success = 0,
        NoMemory = 1,
        UnknownSource = 2,
        FatalError = 3,
    }

    enum CmdOpcode
    {
        Return = 0,
        SetSendEvent = 1,
        SetKey = 2,
        ResetKey = 3,
        MoveKey = 4,
        UseKeys = 5,
        RetryDec = 6,
        SetTimer = 7,
        ResetTimer = 8,
    }

    public struct ConfigParams
    {
        // Size of Key
        public byte SK;
        // Size of Ring Node Addresses
        public byte SRNA;
        // Number of Nodes within AKM Relationship/Echo Ring
        public ushort N;
        // Current Session Seed
        public uint CSS;
        // Next Session Seed
        public uint NSS;
        // Fallback Session Seed
        public uint FSS;
        // Next Fallback Session Seed
        public uint NFSS;
        // Shadow Fallback Session Seed
        public uint SFSS;
        // Next Shadow Fallback Session Seed
        public uint NSFSS;
        // Emergency Failsafe Session Seed
        public uint EFSS;
        // Node Nonresponse Timeout
        public long NNRT;
        // Normal Session Establishment Timeout
        public long NSET;
        // Fallback Resynchronization Session Establishment Timeout
        public long FBSET;
        // Failsafe Resynchronization Session Establishment Timeout
        public long FSSET;
    }

    public struct Configuration
    {
        public ConfigParams cfgParams;
        public IntPtr pdv;
        public IntPtr nodeAddresses;
        public IntPtr selfNodeAddress;
    }

    struct Command
    {
        public CmdOpcode opcode;
        public int p1, p2;
        public IntPtr data;
    }

    struct ProcessCtx
    {
        public IntPtr relationship;
        public Event akmEvent;
        public IntPtr srcAddr;
        public long time_ms;
        public Command cmd;
    }

    public interface IKey
    {
    }

    public interface IKeyFactory
    {
        IKey Create(IntPtr pKeyBytes, int keyLen);
    }

    public interface IEncryptedFrame
    {
        IDecryptedFrame Decrypt(IKey key);
    }

    public interface IDecryptedFrame
    {
        IEncryptedFrame Encrypt(IKey key);
        Event GetFrameEvent();
        byte[] GetSourceAddress();
    }

    public class AkmError : Exception
    {
        public readonly Status status;
        public AkmError(Status status)
        {
            this.status = status;
        }
    }

    public class Relationship : IDisposable
    {
        private IntPtr relationship = IntPtr.Zero;
        private IKeyFactory keyFactory;
        public const int KeyCount = 4;
        private IKey[] keys;
        private int encKeyIdx, decKeyIdx;
        private Event? sendEvent;
        public const int ParameterDataVectorSize = 128;
        private Timer timer;
        private object procLock = new object();

        [DllImport("libakm.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern Status AKMInit(ref ProcessCtx ctx, ref Configuration config);

        [DllImport("libakm.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void AKMProcess(ref ProcessCtx ctx);

        [DllImport("libakm.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void AKMFree(IntPtr relationship);

        public Relationship(IKeyFactory keyFactory, IKey[] initialKeys, ref Configuration config)
        {
            timer = new Timer();
            timer.AutoReset = false;
            timer.Elapsed += Timer_Elapsed;
            this.keyFactory = keyFactory;
            this.keys = initialKeys;
            Status status = Init(ref config);
            if (status != Status.Success)
            {
                timer.Dispose();
                timer = null;
                throw new AkmError(status);
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            TimerTimedOut();
        }

        private ProcessCtx PrepareProcessCtx()
        {
            ProcessCtx ctx = new ProcessCtx();
            ctx.relationship = relationship;
            ctx.time_ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return ctx;
        }

        private Status Init(ref Configuration config)
        {
            ProcessCtx ctx = PrepareProcessCtx();
            Status status = AKMInit(ref ctx, ref config);
            relationship = ctx.relationship;
            if (status == Status.Success)
            {
                IDecryptedFrame dummy = null;
                status = GenericProcess(ref ctx, null, ref dummy);
            }
            if (status != Status.Success)
            {
                Free();
            }
            return status;
        }

        private void Free()
        {
            AKMFree(relationship);
            relationship = IntPtr.Zero;
        }

        public Status ProcessFrame(IEncryptedFrame encFrame, out IDecryptedFrame decFrame)
        {
            ProcessCtx ctx = PrepareProcessCtx();
            decFrame = encFrame?.Decrypt(keys[decKeyIdx]);
            ctx.akmEvent = decFrame != null ? decFrame.GetFrameEvent() : Event.CannotDecrypt;
            return GenericProcess(ref ctx, encFrame, ref decFrame);
        }

        private Status TimerTimedOut()
        {
            ProcessCtx ctx = PrepareProcessCtx();
            ctx.akmEvent = Event.TimeOut;
            IDecryptedFrame dummy = null;
            return GenericProcess(ref ctx, null, ref dummy);
        }

        private Status GenericProcess(ref ProcessCtx ctx, IEncryptedFrame encFrame, ref IDecryptedFrame decFrame)
        {
            lock (procLock)
            {
                return GenericProcessInternal(ref ctx, encFrame, ref decFrame);
            }
        }

        private Status GenericProcessInternal(ref ProcessCtx ctx, IEncryptedFrame encFrame, ref IDecryptedFrame decFrame)
        {
            byte[] srcAddr = decFrame?.GetSourceAddress();
            GCHandle? hSrcAddr = null;
            ctx.srcAddr = IntPtr.Zero;
            if (srcAddr != null)
            {
                hSrcAddr = GCHandle.Alloc(srcAddr, GCHandleType.Pinned);
                ctx.srcAddr = hSrcAddr.Value.AddrOfPinnedObject();
            }
            while (true)
            {
                AKMProcess(ref ctx);
                if (hSrcAddr.HasValue && ctx.srcAddr == IntPtr.Zero)
                {
                    hSrcAddr.Value.Free();
                    hSrcAddr = null;
                }
                switch (ctx.cmd.opcode)
                {
                    case CmdOpcode.Return:
                        return (Status)ctx.cmd.p1;
                    case CmdOpcode.SetSendEvent:
                        if (ctx.cmd.p1 != 0)
                            sendEvent = (Event)ctx.cmd.p2;
                        else
                            sendEvent = null;
                        break;
                    case CmdOpcode.SetKey:
                        keys[ctx.cmd.p1] = keyFactory.Create(ctx.cmd.data, ctx.cmd.p2);
                        break;
                    case CmdOpcode.ResetKey:
                        keys[ctx.cmd.p1] = null;
                        break;
                    case CmdOpcode.MoveKey:
                        keys[ctx.cmd.p1] = keys[ctx.cmd.p2];
                        keys[ctx.cmd.p2] = null;
                        break;
                    case CmdOpcode.UseKeys:
                        decKeyIdx = ctx.cmd.p1;
                        encKeyIdx = ctx.cmd.p2;
                        break;
                    case CmdOpcode.RetryDec:
                        decFrame = encFrame?.Decrypt(keys[ctx.cmd.p1]);
                        ctx.akmEvent = decFrame != null ? decFrame.GetFrameEvent() : Event.CannotDecrypt;
                        srcAddr = decFrame?.GetSourceAddress();
                        hSrcAddr?.Free();
                        hSrcAddr = null;
                        ctx.srcAddr = IntPtr.Zero;
                        if (srcAddr != null)
                        {
                            hSrcAddr = GCHandle.Alloc(srcAddr, GCHandleType.Pinned);
                            ctx.srcAddr = hSrcAddr.Value.AddrOfPinnedObject();
                        }
                        break;
                    case CmdOpcode.SetTimer:
                        SetTimer(Marshal.ReadInt64(ctx.cmd.data));
                        break;
                    case CmdOpcode.ResetTimer:
                        ResetTimer();
                        break;
                }
            }
        }

        private void SetTimer(long t)
        {
            timer.Stop();
            timer.Interval = t - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            timer.Start();
        }

        private void ResetTimer()
        {
            timer.Stop();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer?.Dispose();
                timer = null;
            }
            Free();
        }

        ~Relationship()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
