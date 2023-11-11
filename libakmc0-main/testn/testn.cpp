/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */


#include "../libakm/akm.h"
#include <iostream>
#include <random>

const uint16_t nodeAddresses[] = { 3, 5, 7, 9 };
const uint16_t selfAddress[] = { 9 };

typedef bool(*test_func)(AKMRelationship* relationship);

bool test_basic(AKMRelationship* relationship);
bool test_skip(AKMRelationship* relationship);
bool test_localSEI(AKMRelationship* relationship);
bool test_fallback(AKMRelationship* relationship);
bool test_fbk_from_established(AKMRelationship* relationship);
bool test_decrypt_fails(AKMRelationship* relationship);
bool test_timeouts(AKMRelationship* relationship);

test_func tests[] =
{
	test_basic,
	test_skip,
	test_localSEI,
	test_fallback,
	test_fbk_from_established,
	test_decrypt_fails,
	test_timeouts,
	nullptr,
};

AKMRelationship* makeRelationship()
{
	AKMProcessCtx ctx = { 0 };
	AKMConfiguration config = { 0 };
	AKMParameterDataVector pdv;
	std::random_device rd;
	std::uniform_int_distribution<> dist(0, 255);
	for (int i = 0; i < AKM_PARAMETER_DATA_VECTOR_SIZE; ++i)
		pdv.data[i] = dist(rd);
	config.nodeAddresses = nodeAddresses;
	config.selfNodeAddress = selfAddress;
	config.pdv = &pdv;
	config.params.SK = 1;
	config.params.SRNA = sizeof(selfAddress);
	config.params.N = sizeof(nodeAddresses) / config.params.SRNA;
	config.params.NNRT = 1000000000;
	config.params.NSET = 1000000000;
	config.params.FBSET = 1000000000;
	config.params.FSSET = 1000000000;
	AKMStatus status = AKMInit(&ctx, &config);
	if (status == AKMStSuccess)
	{
		do
			AKMProcess(&ctx);
		while (ctx.cmd.opcode != AKMCmdOpReturn);
		status = (AKMStatus)ctx.cmd.p1;
	}
	if (status != AKMStSuccess)
	{
		AKMFree(ctx.relationship);
		ctx.relationship = NULL;
	}
	return ctx.relationship;
}

int main()
{
	AKMRelationship* relationship = makeRelationship();
	for (int i = 0; ; ++i)
	{
		if (!tests[i])
			break;
		if (!tests[i](relationship))
			break;
	}
	AKMFree(relationship);
	return 0;
}

#define CHECK(x) do { if(!(x)) { std::cout << __LINE__ << ": " << #x << std::endl; return false; } } while(0)

bool test_basic(AKMRelationship* relationship)
{
	AKMProcessCtx ctx = { 0 };
	ctx.relationship = relationship;
	ctx.srcAddr = nodeAddresses + 0;
	ctx.akmEvent = AKMEvRecvSEI;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 1;
	ctx.akmEvent = AKMEvRecvSEI;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 2;
	ctx.akmEvent = AKMEvRecvSEI;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpUseKeys && ctx.cmd.p1 == 1 && ctx.cmd.p2 == 1);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetSendEvent && ctx.cmd.p1 == 1 && ctx.cmd.p2 == AKMEvRecvSEC);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 0;
	ctx.akmEvent = AKMEvRecvSEC;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 1;
	ctx.akmEvent = AKMEvRecvSEC;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 2;
	ctx.akmEvent = AKMEvRecvSEC;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetSendEvent && ctx.cmd.p1 == 1 && ctx.cmd.p2 == AKMEvRecvSEF);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 0;
	ctx.akmEvent = AKMEvRecvSEF;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 1;
	ctx.akmEvent = AKMEvRecvSEF;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 2;
	ctx.akmEvent = AKMEvRecvSEF;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpMoveKey && ctx.cmd.p1 == 0 && ctx.cmd.p2 == 1);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpUseKeys && ctx.cmd.p1 == 0 && ctx.cmd.p2 == 0);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetKey && ctx.cmd.p1 == 1);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetKey && ctx.cmd.p1 == 3);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetSendEvent && ctx.cmd.p1 == 1 && ctx.cmd.p2 == AKMEvRecvSE);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 0;
	ctx.akmEvent = AKMEvRecvSE;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 1;
	ctx.akmEvent = AKMEvRecvSE;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 2;
	ctx.akmEvent = AKMEvRecvSE;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpResetTimer);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	return true;
}

bool test_skip(AKMRelationship* relationship)
{
	AKMProcessCtx ctx = { 0 };
	ctx.relationship = relationship;
	ctx.srcAddr = nodeAddresses + 0;
	ctx.akmEvent = AKMEvRecvSEI;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetTimer);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetSendEvent && ctx.cmd.p1 == 1 && ctx.cmd.p2 == AKMEvRecvSEI);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = NULL;
	ctx.akmEvent = AKMEvCannotDecrypt;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpRetryDec && ctx.cmd.p1 == 1);
	ctx.srcAddr = nodeAddresses + 0;
	ctx.akmEvent = AKMEvRecvSEC;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpUseKeys && ctx.cmd.p1 == 0 && ctx.cmd.p2 == 1);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 1;
	ctx.akmEvent = AKMEvRecvSEC;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 2;
	ctx.akmEvent = AKMEvRecvSEC;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpUseKeys && ctx.cmd.p1 == 1 && ctx.cmd.p2 == 1);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetSendEvent && ctx.cmd.p1 == 1 && ctx.cmd.p2 == AKMEvRecvSEF);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 0;
	ctx.akmEvent = AKMEvRecvSE;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 1;
	ctx.akmEvent = AKMEvRecvSE;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 2;
	ctx.akmEvent = AKMEvRecvSE;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpMoveKey && ctx.cmd.p1 == 0 && ctx.cmd.p2 == 1);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpUseKeys && ctx.cmd.p1 == 0 && ctx.cmd.p2 == 0);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetKey && ctx.cmd.p1 == 1);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetKey && ctx.cmd.p1 == 3);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpResetTimer);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetSendEvent && ctx.cmd.p1 == 1 && ctx.cmd.p2 == AKMEvRecvSE);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	return true;
}

bool test_localSEI(AKMRelationship* relationship)
{
	AKMProcessCtx ctx = { 0 };
	ctx.relationship = relationship;
	ctx.srcAddr = NULL;
	ctx.akmEvent = AKMEvLocalSEI;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetTimer);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetSendEvent && ctx.cmd.p1 == 1 && ctx.cmd.p2 == AKMEvRecvSEI);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = NULL;
	ctx.akmEvent = AKMEvCannotDecrypt;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpRetryDec && ctx.cmd.p1 == 1);
	ctx.srcAddr = nodeAddresses + 0;
	ctx.akmEvent = AKMEvRecvSEC;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpUseKeys && ctx.cmd.p1 == 0 && ctx.cmd.p2 == 1);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 1;
	ctx.akmEvent = AKMEvRecvSEC;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 2;
	ctx.akmEvent = AKMEvRecvSEC;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpUseKeys && ctx.cmd.p1 == 1 && ctx.cmd.p2 == 1);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetSendEvent && ctx.cmd.p1 == 1 && ctx.cmd.p2 == AKMEvRecvSEF);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 0;
	ctx.akmEvent = AKMEvRecvSE;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 1;
	ctx.akmEvent = AKMEvRecvSE;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 2;
	ctx.akmEvent = AKMEvRecvSE;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpMoveKey && ctx.cmd.p1 == 0 && ctx.cmd.p2 == 1);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpUseKeys && ctx.cmd.p1 == 0 && ctx.cmd.p2 == 0);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetKey && ctx.cmd.p1 == 1);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetKey && ctx.cmd.p1 == 3);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpResetTimer);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetSendEvent && ctx.cmd.p1 == 1 && ctx.cmd.p2 == AKMEvRecvSE);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	return true;
}

bool test_fallback(AKMRelationship* relationship)
{
	AKMProcessCtx ctx = { 0 };
	ctx.relationship = relationship;
	ctx.srcAddr = nodeAddresses + 0;
	ctx.akmEvent = AKMEvRecvSEI;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetTimer);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetSendEvent && ctx.cmd.p1 == 1 && ctx.cmd.p2 == AKMEvRecvSEI);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = NULL;
	ctx.akmEvent = AKMEvCannotDecrypt;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpRetryDec && ctx.cmd.p1 == 1);
	ctx.srcAddr = NULL;
	ctx.akmEvent = AKMEvCannotDecrypt;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpRetryDec && ctx.cmd.p1 == 2);
	ctx.srcAddr = nodeAddresses + 0;
	ctx.akmEvent = AKMEvRecvSEC;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpUseKeys && ctx.cmd.p1 == 2 && ctx.cmd.p2 == 2);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 1;
	ctx.akmEvent = AKMEvRecvSEC;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 2;
	ctx.akmEvent = AKMEvRecvSEC;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpUseKeys && ctx.cmd.p1 == 3 && ctx.cmd.p2 == 3);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetSendEvent && ctx.cmd.p1 == 1 && ctx.cmd.p2 == AKMEvRecvSEF);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 0;
	ctx.akmEvent = AKMEvRecvSE;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 1;
	ctx.akmEvent = AKMEvRecvSE;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 2;
	ctx.akmEvent = AKMEvRecvSE;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpMoveKey && ctx.cmd.p1 == 0 && ctx.cmd.p2 == 3);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpUseKeys && ctx.cmd.p1 == 0 && ctx.cmd.p2 == 0);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetKey && ctx.cmd.p1 == 2);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetKey && ctx.cmd.p1 == 1);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetKey && ctx.cmd.p1 == 3);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpResetTimer);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetSendEvent && ctx.cmd.p1 == 1 && ctx.cmd.p2 == AKMEvRecvSE);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	return true;
}

bool test_fbk_from_established(AKMRelationship* relationship)
{
	AKMProcessCtx ctx = { 0 };
	ctx.relationship = relationship;
	ctx.srcAddr = NULL;
	ctx.akmEvent = AKMEvCannotDecrypt;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpRetryDec && ctx.cmd.p1 == 2);
	ctx.srcAddr = nodeAddresses + 0;
	ctx.akmEvent = AKMEvRecvSEC;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpUseKeys && ctx.cmd.p1 == 2 && ctx.cmd.p2 == 2);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetTimer);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetSendEvent && ctx.cmd.p1 == 1 && ctx.cmd.p2 == AKMEvRecvSEI);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 1;
	ctx.akmEvent = AKMEvRecvSEC;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 2;
	ctx.akmEvent = AKMEvRecvSEC;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpUseKeys && ctx.cmd.p1 == 3 && ctx.cmd.p2 == 3);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetSendEvent && ctx.cmd.p1 == 1 && ctx.cmd.p2 == AKMEvRecvSEF);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 0;
	ctx.akmEvent = AKMEvRecvSE;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 1;
	ctx.akmEvent = AKMEvRecvSE;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 2;
	ctx.akmEvent = AKMEvRecvSE;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpMoveKey && ctx.cmd.p1 == 0 && ctx.cmd.p2 == 3);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpUseKeys && ctx.cmd.p1 == 0 && ctx.cmd.p2 == 0);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetKey && ctx.cmd.p1 == 2);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetKey && ctx.cmd.p1 == 1);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetKey && ctx.cmd.p1 == 3);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpResetTimer);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetSendEvent && ctx.cmd.p1 == 1 && ctx.cmd.p2 == AKMEvRecvSE);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	return true;
}

bool test_decrypt_fails(AKMRelationship* relationship)
{
	AKMProcessCtx ctx = { 0 };
	ctx.relationship = relationship;
	ctx.akmEvent = AKMEvCannotDecrypt;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpRetryDec && ctx.cmd.p1 == 2);
	ctx.akmEvent = AKMEvCannotDecrypt;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.akmEvent = AKMEvCannotDecrypt;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpRetryDec && ctx.cmd.p1 == 2);
	ctx.akmEvent = AKMEvCannotDecrypt;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.akmEvent = AKMEvLocalSEI;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetTimer);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetSendEvent && ctx.cmd.p1 == 1 && ctx.cmd.p2 == AKMEvRecvSEI);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.akmEvent = AKMEvCannotDecrypt;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpRetryDec && ctx.cmd.p1 == 1);
	ctx.akmEvent = AKMEvCannotDecrypt;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpRetryDec && ctx.cmd.p1 == 2);
	ctx.akmEvent = AKMEvCannotDecrypt;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.akmEvent = AKMEvCannotDecrypt;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpRetryDec && ctx.cmd.p1 == 1);
	ctx.akmEvent = AKMEvCannotDecrypt;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpRetryDec && ctx.cmd.p1 == 2);
	ctx.akmEvent = AKMEvCannotDecrypt;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 0;
	ctx.akmEvent = AKMEvRecvSEC;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 1;
	ctx.akmEvent = AKMEvRecvSEC;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 2;
	ctx.akmEvent = AKMEvRecvSEC;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpUseKeys && ctx.cmd.p1 == 1 && ctx.cmd.p2 == 1);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetSendEvent && ctx.cmd.p1 == 1 && ctx.cmd.p2 == AKMEvRecvSEF);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 0;
	ctx.akmEvent = AKMEvRecvSE;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 1;
	ctx.akmEvent = AKMEvRecvSE;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 2;
	ctx.akmEvent = AKMEvRecvSE;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpMoveKey && ctx.cmd.p1 == 0 && ctx.cmd.p2 == 1);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpUseKeys && ctx.cmd.p1 == 0 && ctx.cmd.p2 == 0);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetKey && ctx.cmd.p1 == 1);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetKey && ctx.cmd.p1 == 3);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpResetTimer);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetSendEvent && ctx.cmd.p1 == 1 && ctx.cmd.p2 == AKMEvRecvSE);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	return true;
}

bool test_timeouts(AKMRelationship* relationship)
{
	AKMProcessCtx ctx = { 0 };
	ctx.relationship = relationship;
	ctx.srcAddr = nodeAddresses + 0;
	ctx.akmEvent = AKMEvRecvSEI;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetTimer);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetSendEvent && ctx.cmd.p1 == 1 && ctx.cmd.p2 == AKMEvRecvSEI);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 1;
	ctx.akmEvent = AKMEvRecvSEI;
	ctx.time_ms += 200000000;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 2;
	ctx.akmEvent = AKMEvRecvSEI;
	ctx.time_ms += 200000000;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpUseKeys && ctx.cmd.p1 == 1 && ctx.cmd.p2 == 1);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetSendEvent && ctx.cmd.p1 == 1 && ctx.cmd.p2 == AKMEvRecvSEC);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 0;
	ctx.akmEvent = AKMEvRecvSEI;
	ctx.time_ms += 200000000;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetTimer);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 1;
	ctx.akmEvent = AKMEvRecvSEI;
	ctx.time_ms += 200000000;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetTimer);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 2;
	ctx.akmEvent = AKMEvRecvSEI;
	ctx.time_ms += 200000000;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 0;
	ctx.akmEvent = AKMEvRecvSEI;
	ctx.time_ms += 200000000;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 1;
	ctx.akmEvent = AKMEvRecvSEI;
	ctx.time_ms += 200000000;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 2;
	ctx.akmEvent = AKMEvRecvSEI;
	ctx.time_ms += 200000000;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpUseKeys && ctx.cmd.p1 == 2 && ctx.cmd.p2 == 2);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetTimer);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetSendEvent && ctx.cmd.p1 == 1 && ctx.cmd.p2 == AKMEvRecvSEI);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 0;
	ctx.akmEvent = AKMEvRecvSEC;
	ctx.time_ms += 200000000;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetTimer);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 1;
	ctx.akmEvent = AKMEvRecvSEC;
	ctx.time_ms += 200000000;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetTimer);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 0;
	ctx.akmEvent = AKMEvRecvSEC;
	ctx.time_ms += 200000000;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 1;
	ctx.akmEvent = AKMEvRecvSEC;
	ctx.time_ms += 200000000;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.akmEvent = AKMEvTimeOut;
	ctx.time_ms += 600000000;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpUseKeys && ctx.cmd.p1 == 3 && ctx.cmd.p2 == 3);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetTimer);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetSendEvent && ctx.cmd.p1 == 1 && ctx.cmd.p2 == AKMEvRecvSEF);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 0;
	ctx.akmEvent = AKMEvRecvSEF;
	ctx.time_ms += 200000000;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetTimer);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 1;
	ctx.akmEvent = AKMEvRecvSEF;
	ctx.time_ms += 200000000;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpMoveKey && ctx.cmd.p1 == 0 && ctx.cmd.p2 == 3);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpUseKeys && ctx.cmd.p1 == 0 && ctx.cmd.p2 == 0);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetKey && ctx.cmd.p1 == 2);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetKey && ctx.cmd.p1 == 1);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetKey && ctx.cmd.p1 == 3);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetTimer);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetSendEvent && ctx.cmd.p1 == 1 && ctx.cmd.p2 == AKMEvRecvSE);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 0;
	ctx.akmEvent = AKMEvRecvSE;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpSetTimer);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	ctx.srcAddr = nodeAddresses + 1;
	ctx.akmEvent = AKMEvRecvSE;
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpResetTimer);
	AKMProcess(&ctx);
	CHECK(ctx.cmd.opcode == AKMCmdOpReturn && ctx.cmd.p1 == AKMStSuccess);
	return true;
}
