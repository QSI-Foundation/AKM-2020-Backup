/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */

using AKMLogic;
using NUnit.Framework;
using System;
using System.Text;

namespace AKM_Tests
{
	public class AkmKey_Tests
	{
		[SetUp]
		public void Setup()
		{

		}

		[Test]
		public void AkmKeyCreateWithProperLength()
		{
			var akmKey = new AkmKey(128);
			Assert.AreEqual(128, akmKey.KeyLength);
			Assert.AreEqual(128, akmKey.KeyAsBytes.Length);
			Assert.AreEqual(128, akmKey.KeyAsString.Length);
		}

		[Test]
		public void AkmKeySetKeyThrowsException()
		{
			var akmKey = new AkmKey(128);
			string nullString = null;

			var ex = Assert.Throws<ArgumentException>(() => akmKey.SetKeyFromString(string.Empty));
			Assert.AreEqual("Key cannot be empty.", ex.Message);

			ex = Assert.Throws<ArgumentException>(() => akmKey.SetKeyFromString(nullString));
			Assert.AreEqual("Key cannot be empty.", ex.Message);

			ex = Assert.Throws<ArgumentException>(() => akmKey.SetKeyFromString("a"));
			Assert.AreEqual("Invalid new key length.", ex.Message);

			ex = Assert.Throws<ArgumentException>(() => akmKey.SetKey((new byte[] { 1, 2, 3 })));
			Assert.AreEqual("Invalid array length. Can't be null and must be equal to defined Key Length.", ex.Message);
		}

		[Test]
		public void AkmSetKeyWorksCorrectly()
		{
			var akmKey = new AkmKey(10);
			var keyString = "ABCDEFGHIJ";
			var keyBytes = Encoding.UTF8.GetBytes(keyString);

			akmKey.SetKey(keyBytes);

			Assert.AreEqual(keyString, akmKey.KeyAsString);

			keyBytes = new byte[] { 100, 101, 102, 103, 104, 105, 106, 107, 108, 109 };
			keyString = Encoding.UTF8.GetString(keyBytes);

			akmKey.SetKeyFromString(keyString);
			var akmKeyBytes = akmKey.KeyAsBytes;

			Assert.AreEqual(akmKeyBytes.Length, keyBytes.Length);
			for (int i = 0; i < keyBytes.Length; i++)
			{
				Assert.AreEqual(keyBytes[i], akmKeyBytes[i]);
			}
		}
	}
}
