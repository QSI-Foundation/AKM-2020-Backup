/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace testapp
{
    class DummyKey : mlibakm.IKey
    {
        public byte key;

        public DummyKey(IntPtr pKeyBytes)
        {
            key = Marshal.ReadByte(pKeyBytes);
        }

        public DummyKey(byte keyByte)
        {
            key = keyByte;
        }
    }

    class DummyKeyFactory : mlibakm.IKeyFactory
    {
        public static DummyKeyFactory Instance = new DummyKeyFactory();

        mlibakm.IKey mlibakm.IKeyFactory.Create(IntPtr pKeyBytes, int keyLen)
        {
            return new DummyKey(pKeyBytes);
        }
    }

    class DummyDecryptedFrame : mlibakm.IDecryptedFrame
    {
        private byte[] srcAddress;
        private mlibakm.Event akmEvent;

        public DummyDecryptedFrame(byte[] srcAddr, mlibakm.Event akmEv)
        {
            srcAddress = srcAddr;
            akmEvent = akmEv;
        }

        public mlibakm.IEncryptedFrame Encrypt(mlibakm.IKey key)
        {
            return new DummyEncryptedFrame(this);
        }

        public mlibakm.Event GetFrameEvent()
        {
            return akmEvent;
        }

        public byte[] GetSourceAddress()
        {
            return srcAddress;
        }
    }

    class DummyEncryptedFrame : mlibakm.IEncryptedFrame
    {
        private DummyDecryptedFrame decryptedFrame;

        public DummyEncryptedFrame(DummyDecryptedFrame decFrame)
        {
            decryptedFrame = decFrame;
        }

        public DummyEncryptedFrame(byte[] srcAddr, mlibakm.Event akmEv) : this(new DummyDecryptedFrame(srcAddr, akmEv))
        {
        }

        public mlibakm.IDecryptedFrame Decrypt(mlibakm.IKey key)
        {
            return decryptedFrame;
        }
    }

    class Program
    {
        static mlibakm.Relationship MakeRelationship()
        {
            byte[] nodeAddresses = new byte[] { 3, 5, 7, 9 };
            byte[] selfAddress = new byte[] { 7 };
            byte[] pdv = new byte[mlibakm.Relationship.ParameterDataVectorSize];
            new Random().NextBytes(pdv);
            using (mlibakm.DisposableGCHandle hNodeAddresses = new mlibakm.DisposableGCHandle(GCHandle.Alloc(nodeAddresses, GCHandleType.Pinned)))
            {
                using (mlibakm.DisposableGCHandle hSelfAddress = new mlibakm.DisposableGCHandle(GCHandle.Alloc(selfAddress, GCHandleType.Pinned)))
                {
                    using (mlibakm.DisposableGCHandle hPdv = new mlibakm.DisposableGCHandle(GCHandle.Alloc(pdv, GCHandleType.Pinned)))
                    {
                        mlibakm.Configuration config = new mlibakm.Configuration();
                        config.nodeAddresses = hNodeAddresses.Handle.AddrOfPinnedObject();
                        config.selfNodeAddress = hSelfAddress.Handle.AddrOfPinnedObject();
                        config.pdv = hPdv.Handle.AddrOfPinnedObject();
                        config.cfgParams.SK = 1;
                        config.cfgParams.SRNA = 1;
                        config.cfgParams.N = (ushort)(nodeAddresses.Length / config.cfgParams.SRNA);
                        config.cfgParams.NNRT = 1000000000;
                        config.cfgParams.NSET = 1000000000;
                        config.cfgParams.FBSET = 1000000000;
                        config.cfgParams.FSSET = 1000000000;
                        mlibakm.IKey[] initialKeys = new mlibakm.IKey[mlibakm.Relationship.KeyCount];
                        for(int i = 0; i < initialKeys.Length; ++i)
                        {
                            initialKeys[i] = new DummyKey((byte)i);
                        }
                        return new mlibakm.Relationship(DummyKeyFactory.Instance, initialKeys, ref config);
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            using (mlibakm.Relationship rel = MakeRelationship())
            {
                ReadConfig(rel);
                Test(rel);
                ReadConfig(rel);
            }
        }

        static void ReadConfig(mlibakm.Relationship rel)
        {
            mlibakm.ConfigParams akmParams;
            byte[] pdv;
            byte[] nodeAddresses;
            byte[] selfNodeAddr;
            rel.GetConfig(out akmParams, out pdv, out nodeAddresses, out selfNodeAddr);
            Console.WriteLine("-----");
            Console.WriteLine("cfg.N = " + ((int)akmParams.N).ToString());
            Console.WriteLine("cfg.SRNA = " + ((int)akmParams.SRNA).ToString());
            Console.WriteLine("cfg.CSS = " + akmParams.CSS.ToString());
            Console.WriteLine("cfg.NSS = " + akmParams.NSS.ToString());
            Console.WriteLine("cfg.FSS = " + akmParams.FSS.ToString());
            Console.WriteLine("cfg.NFSS = " + akmParams.NFSS.ToString());
            Console.WriteLine("PDV = " + BitConverter.ToString(pdv));
            Console.WriteLine("nodeAddresses = " + BitConverter.ToString(nodeAddresses));
            Console.WriteLine("selfNodeAddr = " + BitConverter.ToString(selfNodeAddr));
            Console.WriteLine("-----");
        }

        static void Test(mlibakm.Relationship rel)
        {
            mlibakm.IDecryptedFrame decFrame;
            rel.ProcessFrame(new DummyEncryptedFrame(new byte[] { 5 }, mlibakm.Event.RecvSEI), out decFrame);
            rel.ProcessFrame(new DummyEncryptedFrame(new byte[] { 3 }, mlibakm.Event.RecvSEI), out decFrame);
            rel.ProcessFrame(new DummyEncryptedFrame(new byte[] { 9 }, mlibakm.Event.RecvSEI), out decFrame);
            rel.ProcessFrame(new DummyEncryptedFrame(new byte[] { 3 }, mlibakm.Event.RecvSEC), out decFrame);
            rel.ProcessFrame(new DummyEncryptedFrame(new byte[] { 9 }, mlibakm.Event.RecvSEC), out decFrame);
            rel.ProcessFrame(new DummyEncryptedFrame(new byte[] { 5 }, mlibakm.Event.RecvSEC), out decFrame);
            rel.ProcessFrame(new DummyEncryptedFrame(new byte[] { 9 }, mlibakm.Event.RecvSEF), out decFrame);
            rel.ProcessFrame(new DummyEncryptedFrame(new byte[] { 5 }, mlibakm.Event.RecvSEF), out decFrame);
            rel.ProcessFrame(new DummyEncryptedFrame(new byte[] { 3 }, mlibakm.Event.RecvSEF), out decFrame);
            rel.ProcessFrame(new DummyEncryptedFrame(new byte[] { 3 }, mlibakm.Event.RecvSE), out decFrame);
            rel.ProcessFrame(new DummyEncryptedFrame(new byte[] { 9 }, mlibakm.Event.RecvSE), out decFrame);
            rel.ProcessFrame(new DummyEncryptedFrame(new byte[] { 5 }, mlibakm.Event.RecvSE), out decFrame);
            rel.ProcessFrame(new DummyEncryptedFrame(new byte[] { 3 }, mlibakm.Event.RecvSE), out decFrame);
            rel.ProcessFrame(new DummyEncryptedFrame(new byte[] { 9 }, mlibakm.Event.RecvSE), out decFrame);
            rel.ProcessFrame(new DummyEncryptedFrame(new byte[] { 5 }, mlibakm.Event.RecvSEI), out decFrame);
            rel.ProcessFrame(new DummyEncryptedFrame(new byte[] { 3 }, mlibakm.Event.RecvSEI), out decFrame);
            rel.ProcessFrame(new DummyEncryptedFrame(new byte[] { 9 }, mlibakm.Event.RecvSEC), out decFrame);
            rel.ProcessFrame(new DummyEncryptedFrame(new byte[] { 5 }, mlibakm.Event.RecvSEC), out decFrame);
            rel.ProcessFrame(new DummyEncryptedFrame(new byte[] { 3 }, mlibakm.Event.RecvSEF), out decFrame);
            rel.ProcessFrame(new DummyEncryptedFrame(new byte[] { 9 }, mlibakm.Event.RecvSEF), out decFrame);
            rel.ProcessFrame(new DummyEncryptedFrame(new byte[] { 5 }, mlibakm.Event.RecvSE), out decFrame);
            rel.ProcessFrame(new DummyEncryptedFrame(new byte[] { 3 }, mlibakm.Event.RecvSE), out decFrame);
            rel.ProcessFrame(new DummyEncryptedFrame(new byte[] { 9 }, mlibakm.Event.RecvSE), out decFrame);
        }
    }
}
