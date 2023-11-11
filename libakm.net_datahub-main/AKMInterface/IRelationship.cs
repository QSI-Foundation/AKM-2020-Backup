using AKMCommon.Enum;
using System;

namespace AKMInterface
{
	public interface IRelationship : IDisposable
	{
		IKey[] GetKeys();
		AkmStatus ProcessFrame(IEncryptedFrame encFrame, out IDecryptedFrame decFrame);
		AkmStatus PrepareFrame(IDecryptedFrame decFrame, out IEncryptedFrame encFrame);
		AkmStatus PrepareFrame(IDecryptedFrame decFrame, out IEncryptedFrame encFrame, AkmEvent? forcedAkmEvent);
	}
}
