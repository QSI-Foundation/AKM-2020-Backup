/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

using AKMLogic;
using NUnit.Framework;
using System;
using System.Runtime.InteropServices;

namespace AKM_Tests
{
	public class AkmKeyFactory_Tests
	{
		private const string KEY_STRING = "eiRDJkYpSEBNY1FmVGpXYVpyNHU3eCFBJUQqRy1LYXQ=";
		private byte[] _keyBytes;
		private AkmKeyFactory _akmKeyFactory;
		[OneTimeSetUp]
		public void Setup()
		{
			_keyBytes = Convert.FromBase64String(KEY_STRING);
			_akmKeyFactory = new AkmKeyFactory(_keyBytes.Length);

		}

		[Test]
		public void AkmKeyFactoryCreateKeyFromString()
		{
			var akmKey = _akmKeyFactory.Create(KEY_STRING);
			Assert.AreEqual(KEY_STRING, akmKey.KeyAsBase64String);
			Assert.AreEqual(_keyBytes, akmKey.KeyAsBytes);
			Assert.AreEqual(_keyBytes.Length, akmKey.KeyLength);
		}

		[Test]
		public void AkmKeyFactoryCreateKeyFromByteArray()
		{
			var akmKey = _akmKeyFactory.Create(_keyBytes);
			Assert.AreEqual(KEY_STRING, akmKey.KeyAsBase64String);
			Assert.AreEqual(_keyBytes, akmKey.KeyAsBytes);
			Assert.AreEqual(_keyBytes.Length, akmKey.KeyLength);
		}

		[Test]
		public void AkmKeyFactoryCreateKeyFromPointer()
		{
			var intPtr = Marshal.UnsafeAddrOfPinnedArrayElement<byte>(_keyBytes, 0);
			var akmKey = _akmKeyFactory.Create(intPtr, _keyBytes.Length);

			Assert.AreEqual(KEY_STRING, akmKey.KeyAsBase64String);
			Assert.AreEqual(_keyBytes, akmKey.KeyAsBytes);
			Assert.AreEqual(_keyBytes.Length, akmKey.KeyLength);
		}

	}
}
