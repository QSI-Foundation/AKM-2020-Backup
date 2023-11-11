/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

using AKMCommon.Struct;
using AKMInterface;
using AKMLogic;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace AKM_Tests
{
	public class AkmRelationship_Tests
	{
		private readonly Mock<IKey> keyMock = new Mock<IKey>();
		private readonly Mock<IKeyFactory> keyFactoryMock = new Mock<IKeyFactory>();
		private readonly Mock<IDecryptedFrame> decrMokc = new Mock<IDecryptedFrame>();
		private readonly Mock<IEncryptedFrame> encrMock = new Mock<IEncryptedFrame>();
		private readonly Mock<ICLibCalls> cLibMock = new Mock<ICLibCalls>();
		private readonly Mock<ILogger> loggerMock = new Mock<ILogger>();
		private readonly byte[] byteArr = Encoding.UTF8.GetBytes("KeyValue");
		private AkmConfiguration akmConfig;

		[SetUp]
		public void Setup()
		{
			keyFactoryMock.Setup(x => x.Create(byteArr)).Returns(keyMock.Object);
			decrMokc.Setup(x => x.Encrypt(keyMock.Object)).Returns(encrMock.Object);
			encrMock.Setup(x => x.Decrypt(keyMock.Object)).Returns(decrMokc.Object);
			loggerMock.Setup(x => x.LogDebug(It.IsAny<string>())).Verifiable();
		}

		[Test]
		public void AkmRelationShipCreatedSuccessfully()
		{
			byte[] nodeAddresses = new byte[4] { 3, 5, 7, 9 };
			byte[] selfAddress = new byte[1] { 3 };
			AkmParameterDataVector pdv;
			pdv.data = new byte[128];

			IntPtr umPointerNode = Marshal.AllocHGlobal(nodeAddresses.Length);
			IntPtr umPointerSelf = Marshal.AllocHGlobal(selfAddress.Length);
			IntPtr umPointerPdv = Marshal.AllocHGlobal(pdv.data.Length);

			Marshal.Copy(nodeAddresses, 0, umPointerNode, nodeAddresses.Length);
			Marshal.Copy(selfAddress, 0, umPointerSelf, selfAddress.Length);
			Marshal.Copy(pdv.data, 0, umPointerPdv, pdv.data.Length);

			akmConfig.nodeAddresses = umPointerNode;
			akmConfig.selfNodeAddress = umPointerSelf;
			akmConfig.pdv = umPointerPdv;
			akmConfig.cfgParams.SK = 1;
			akmConfig.cfgParams.SRNA = 1;
			akmConfig.cfgParams.N = (ushort)((nodeAddresses.Length) / akmConfig.cfgParams.SRNA);
			akmConfig.cfgParams.NNRT = 1000000000;
			akmConfig.cfgParams.NSET = 1000000000;
			akmConfig.cfgParams.FBSET = 1000000000;
			akmConfig.cfgParams.FSSET = 1000000000;
			var _akmRel = new AkmRelationship(loggerMock.Object, cLibMock.Object, keyFactoryMock.Object, new[] { keyMock.Object, keyMock.Object, keyMock.Object, keyMock.Object }, ref akmConfig);
			Assert.Pass();
		}

		[Test]

		public void AkmRelationshipNotCreatedForMissingParams()
		{
			Assert.That(() => new AkmRelationship(null,null, null, null, ref akmConfig), Throws.ArgumentNullException);

			Assert.That(() => new AkmRelationship(loggerMock.Object, cLibMock.Object, keyFactoryMock.Object, null, ref akmConfig), Throws.ArgumentNullException);
			Assert.That(() => new AkmRelationship(loggerMock.Object, cLibMock.Object, null, new[] { keyMock.Object }, ref akmConfig), Throws.ArgumentNullException);
			Assert.That(() => new AkmRelationship(loggerMock.Object, null, keyFactoryMock.Object, new[] { keyMock.Object }, ref akmConfig), Throws.ArgumentNullException);

			Assert.That(() => new AkmRelationship(loggerMock.Object, cLibMock.Object, null, null, ref akmConfig), Throws.ArgumentNullException);
			Assert.That(() => new AkmRelationship(loggerMock.Object, null, null, new[] { keyMock.Object }, ref akmConfig), Throws.ArgumentNullException);
			Assert.That(() => new AkmRelationship(loggerMock.Object, null, keyFactoryMock.Object, null, ref akmConfig), Throws.ArgumentNullException);

		}

	}
}
