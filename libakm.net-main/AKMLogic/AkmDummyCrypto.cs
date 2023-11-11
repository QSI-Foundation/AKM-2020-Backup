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
 
using AKMInterface;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace AKMLogic
{
	public class AkmDummyCrypto : ICryptography
	{
		public byte[] CalculateHash(byte[] data)
		{
			using (var sha256 = SHA256.Create())
			{
				return sha256.ComputeHash(data);
			}
		}

		public byte[] Decrypt(byte[] dataToDecrypt, byte[] key)
		{
			return dataToDecrypt;
		}

		public byte[] Encrypt(byte[] dataToEncrypt, byte[] key)
		{
			return dataToEncrypt;
		}
	}
}
