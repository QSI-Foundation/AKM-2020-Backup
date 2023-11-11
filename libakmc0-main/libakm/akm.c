/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */


#include "akm_internal.h"
#include "akm_core.h"
#include <stdlib.h>
#include <stdbool.h>
#include <string.h>
#include <assert.h>

static void cMain(struct AKMProcessCtx* ctx);
static void cInit0(struct AKMProcessCtx* ctx);

static inline int getAddrSize(struct AKMProcessCtx* ctx)
{
	return ctx->relationship->config.SRNA;
}

static inline int findSrcNodeIdx(struct AKMProcessCtx* ctx)
{
	if (!ctx->srcAddr)
		return -1;
	return addrlist_find_idx_vec(&ctx->relationship->nodeAddresses, getAddrSize(ctx), ctx->srcAddr);
}

static void uncountNodeSubCounters(struct RelSubCounters* relCnts, const struct NodeSubCounters* nodeCnts)
{
	for (int i = 0; i < AKM_NUM_OF_STATES; ++i)
	{
		if (nodeCnts->cnts[i])
		{
			relCnts->nodes[i]--;
			assert(relCnts->nodes[i] >= 0);
		}
	}
}

static void uncountNodeCounters(struct RelCounters* relCnts, const struct NodeCounters* nodeCnts)
{
	uncountNodeSubCounters(&relCnts->normal, &nodeCnts->normal);
	uncountNodeSubCounters(&relCnts->fallback, &nodeCnts->fallback);
}

static void uncountNodeByIdx(struct AKMProcessCtx* ctx, int idx)
{
	struct NodeCounters* cnts = NodeCntsVec_elem(&ctx->relationship->nodeCounters, (size_t)idx);
	uncountNodeCounters(&ctx->relationship->relCounters, cnts);
}

static void removeNodeByIdx(struct AKMProcessCtx* ctx, int idx)
{
	if (idx < 0)
		return;
	uncountNodeByIdx(ctx, idx);
	addrlist_remove_by_idx_vec(&ctx->relationship->nodeAddresses, getAddrSize(ctx), idx);
	akm_time_vec_erase(&ctx->relationship->nodeLastRcvTimes, (size_t)idx, 1);
	NodeCntsVec_erase(&ctx->relationship->nodeCounters, (size_t)idx, 1);
	ctx->relationship->config.N--;
	if (idx < ctx->relationship->selfIdx)
		ctx->relationship->selfIdx--;
	struct ProcessingInfo* proc = &ctx->relationship->proc;
	if (idx < proc->recvFrameSrcNodeIdx)
		proc->recvFrameSrcNodeIdx--;
	else if (idx == proc->recvFrameSrcNodeIdx)
		proc->recvFrameSrcNodeIdx = -1;
}

static void setLastReceptionTimeForAllNodes(struct AKMProcessCtx* ctx)
{
	const int nodeCnt = ctx->relationship->config.N;
	akm_time_t* nodeTimes = akm_time_vec_elem(&ctx->relationship->nodeLastRcvTimes, 0);
	for (int i = 0; i < nodeCnt; ++i)
		nodeTimes[i] = ctx->time_ms;
}

static void removeTimedOutNodes(struct AKMProcessCtx* ctx)
{
	if (ctx->relationship->proc.skipTimeOutNodesRemoval)
		return;
	ctx->relationship->proc.skipTimeOutNodesRemoval = true;
	const akm_time_t timeout = ctx->relationship->config.NNRT;
	akm_time_t* nodeTimes = akm_time_vec_elem(&ctx->relationship->nodeLastRcvTimes, 0);
	for (int i = 0; i < ctx->relationship->config.N; ++i)
	{
		if (i == ctx->relationship->selfIdx)
			continue;
		const akm_time_t time_diff_from_last_reception = ctx->time_ms - nodeTimes[i];
		if (time_diff_from_last_reception > timeout)
		{
			removeNodeByIdx(ctx, i);
			--i;
		}
	}
}

static bool checkConfiguration(const struct AKMConfiguration* config)
{
	if (!config)
		return false;
	if (!config->pdv)
		return false;
	if (!config->nodeAddresses)
		return false;
	if (!config->selfNodeAddress)
		return false;
	if (addrlist_calc_size_check(config->params.N, config->params.SRNA) < 0)
		return false;
	if (addrlist_check_sorted_nodups_raw(config->nodeAddresses, config->params.N, config->params.SRNA) < 0)
		return false;
	return true;
}

enum AKMStatus AKMInit(struct AKMProcessCtx* ctx, const struct AKMConfiguration* config)
{
	const akm_time_t tm = ctx->time_ms;
	memset(ctx, 0, sizeof(*ctx));
	ctx->time_ms = tm;
	if (!checkConfiguration(config))
		return AKMStFatalError;
	const int selfNodeIdx = addrlist_find_idx_raw(config->nodeAddresses, config->params.N, config->params.SRNA, config->selfNodeAddress);
	if (selfNodeIdx < 0)
		return AKMStUnknownSource;
	struct AKMRelationship* relationship = (struct AKMRelationship*)malloc(sizeof(struct AKMRelationship));
	if (!relationship)
		return AKMStNoMemory;
	memset(relationship, 0, sizeof(*relationship));
	relationship->selfIdx = selfNodeIdx;
	memcpy(&relationship->config, &config->params, sizeof(config->params));
	memcpy(&relationship->pdv, config->pdv, sizeof(relationship->pdv));
	const size_t nodeCnt = config->params.N;
	const size_t totalNodeListBytes = (size_t)(config->params.N) * (size_t)(config->params.SRNA);
	relationship->proc.keyBuffer = malloc(config->params.SK);
	bool mem_ok = !!relationship->proc.keyBuffer;
	mem_ok = mem_ok && bytevector_resize(&relationship->nodeAddresses, totalNodeListBytes);
	mem_ok = mem_ok && akm_time_vec_resize(&relationship->nodeLastRcvTimes, nodeCnt);
	mem_ok = mem_ok && NodeCntsVec_resize(&relationship->nodeCounters, nodeCnt);
	if (!mem_ok)
	{
		AKMFree(relationship);
		return AKMStNoMemory;
	}
	memcpy(relationship->nodeAddresses.buffer, config->nodeAddresses, totalNodeListBytes);
	relationship->proc.machState = AKM_MOffline;
	relationship->proc.recvFrameSrcNodeIdx = -1;
	relationship->proc.recvFrameEvent = AKMEvNone;
	relationship->proc.status = AKMStSuccess;
	ctx->relationship = relationship;
	ctx->akmEvent = AKMEvNone;
	setContinuation(ctx, cInit0);
	return AKMStSuccess;
}

void AKMGetConfig(struct AKMRelationship* relationship, struct AKMConfiguration* config)
{
	memcpy(&config->params, &relationship->config, sizeof(relationship->config));
	if (config->pdv)
	{
		memcpy(config->pdv, &relationship->pdv, sizeof(relationship->pdv));
	}
	if (config->nodeAddresses)
	{
		const size_t addrListLen = (size_t)(relationship->config.N) * (size_t)(relationship->config.SRNA);
		assert(addrListLen == relationship->nodeAddresses.size);
		memcpy(config->nodeAddresses, relationship->nodeAddresses.buffer, addrListLen);
	}
	if (config->selfNodeAddress)
	{
		assert(relationship->selfIdx >= 0 && relationship->selfIdx < relationship->config.N);
		const size_t srna = relationship->config.SRNA;
		const size_t selfAddrOffset = (size_t)(relationship->selfIdx) * srna;
		assert(selfAddrOffset + srna <= relationship->nodeAddresses.size);
		memcpy(config->selfNodeAddress, bytevector_getptr(&relationship->nodeAddresses, selfAddrOffset), srna);
	}
}

void AKMFree(struct AKMRelationship* relationship)
{
	if (!relationship)
		return;
	bytevector_free(&relationship->nodeAddresses);
	akm_time_vec_free(&relationship->nodeLastRcvTimes);
	NodeCntsVec_free(&relationship->nodeCounters);
	free(relationship->proc.keyBuffer);
	free(relationship);
}

static void yieldProcess(struct AKMProcessCtx* ctx, enum AKMCmdOpcode opcode, int p1, int p2, const void* data)
{
	assert(!ctx->relationship->proc.yieldProcess);
	ctx->relationship->proc.yieldProcess = true;
	ctx->cmd.opcode = opcode;
	ctx->cmd.p1 = p1;
	ctx->cmd.p2 = p2;
	ctx->cmd.data = data;
}

static void setRetStatus(struct AKMProcessCtx* ctx, enum AKMStatus status)
{
	ctx->relationship->proc.status = status;
}

static void yieldOpUseKeys(struct AKMProcessCtx* ctx, enum AKMKey encKey, enum AKMKey decKey)
{
	struct ProcessingInfo* proc = &ctx->relationship->proc;
	proc->decTryKey = decKey;
	if (proc->encKey != encKey || proc->decKey != decKey)
	{
		proc->encKey = encKey;
		proc->decKey = decKey;
		yieldProcess(ctx, AKMCmdOpUseKeys, encKey, decKey, NULL);
	}
}

static void yieldOpRetryDec(struct AKMProcessCtx* ctx, enum AKMKey decTryKey)
{
	ctx->relationship->proc.decTryKey = decTryKey;
	yieldProcess(ctx, AKMCmdOpRetryDec, decTryKey, 0, NULL);
}

void AKMProcess(struct AKMProcessCtx* ctx)
{
	do
	{
		getContinuation(ctx)(ctx);
		ctx->akmEvent = AKMEvNone;
		ctx->srcAddr = NULL;
	}
	while (!ctx->relationship->proc.yieldProcess);
	ctx->relationship->proc.yieldProcess = false;
}

static void cDoUseDecTryKeyAsDecKey(struct AKMProcessCtx* ctx)
{
	popContinuation(ctx);
	struct ProcessingInfo* proc = &ctx->relationship->proc;
	yieldOpUseKeys(ctx, proc->encKey, proc->decTryKey);
}

static void xcDoUseKey(struct AKMProcessCtx* ctx, enum AKMKey key)
{
	popContinuation(ctx);
	struct ProcessingInfo* proc = &ctx->relationship->proc;
	yieldOpUseKeys(ctx, key, key);
}

static void cDoUseCSK(struct AKMProcessCtx* ctx) { xcDoUseKey(ctx, AKM_CSK); }
static void cDoUseNSK(struct AKMProcessCtx* ctx) { xcDoUseKey(ctx, AKM_NSK); }
static void cDoUseCFSK(struct AKMProcessCtx* ctx) { xcDoUseKey(ctx, AKM_CFSK); }
static void cDoUseNFSK(struct AKMProcessCtx* ctx) { xcDoUseKey(ctx, AKM_NFSK); }

static void incrementNodeCnt(struct AKMProcessCtx* ctx, int nodeIdx, enum AKMSysState sysState)
{
	struct NodeSubCounters* cnts = nodeSubCounters(NodeCntsVec_elem(&ctx->relationship->nodeCounters, nodeIdx), ctx->relationship->proc.machState);
	if (cnts->cnts[sysState]++ < 1)
		relSubCounters(&ctx->relationship->relCounters, ctx->relationship->proc.machState)->nodes[sysState]++;
}

static void countNodeState(struct AKMProcessCtx* ctx, int nodeIdx, enum AKMSysState nodeSysState)
{
	const enum AKMSysState selfSysState = ctx->relationship->proc.sysState;
	const enum AKMSysStateRel stRel = states_relation(selfSysState, nodeSysState);
	switch (stRel) {
	case STREL_PREV:
		if (nodeSysState == AKM_SE)
			break;
		/* fall-through */
	case STREL_SAME:
		incrementNodeCnt(ctx, nodeIdx, nodeSysState);
		break;
	case STREL_NEXT:
	case STREL_CROSS:
		incrementNodeCnt(ctx, nodeIdx, selfSysState);
		incrementNodeCnt(ctx, nodeIdx, related_state(selfSysState, STREL_NEXT));
		break;
	}
}

static void switchToFallbackEstablishing(struct AKMProcessCtx* ctx)
{
	struct ProcessingInfo* proc = &ctx->relationship->proc;
	if (proc->machState == AKM_MFallbackEstablishing)
		return;
	resetCounters(ctx);
	proc->machState = AKM_MFallbackEstablishing;
	proc->sysState = AKM_SEI;
	ctx->relationship->lastStateChangeTime = ctx->time_ms;
	incrementNodeCnt(ctx, ctx->relationship->selfIdx, proc->sysState);
	yieldOpUseKeys(ctx, AKM_CFSK, AKM_CFSK);
}

static void switchToNormalEstablishing(struct AKMProcessCtx* ctx)
{
	struct ProcessingInfo* proc = &ctx->relationship->proc;
	if (proc->machState == AKM_MNormalEstablishing)
		return;
	resetCounters(ctx);
	proc->machState = AKM_MNormalEstablishing;
	proc->sysState = AKM_SEI;
	ctx->relationship->lastStateChangeTime = ctx->time_ms;
	setLastReceptionTimeForAllNodes(ctx);
	incrementNodeCnt(ctx, ctx->relationship->selfIdx, proc->sysState);
}

void cInit0(struct AKMProcessCtx* ctx)
{
	setContinuation(ctx, cMain);
	switchToNormalEstablishing(ctx);
}

static void handleEvRecv(struct AKMProcessCtx* ctx);
static void handleLocalSEI(struct AKMProcessCtx* ctx);
static void handleEvCannotDecrypt(struct AKMProcessCtx* ctx);
static void handleProcFin(struct AKMProcessCtx* ctx);

void cMain(struct AKMProcessCtx* ctx)
{
	switch (ctx->akmEvent)
	{
	case AKMEvNone:
		handleProcFin(ctx);
		break;
	case AKMEvRecvSE:
	case AKMEvRecvSEI:
	case AKMEvRecvSEC:
	case AKMEvRecvSEF:
		handleEvRecv(ctx);
		break;
	case AKMEvCannotDecrypt:
		handleEvCannotDecrypt(ctx);
		break;
	case AKMEvTimeOut:
		break;
	case AKMEvLocalSEI:
		handleLocalSEI(ctx);
		break;
	}
}

static void updateState(struct AKMProcessCtx* ctx);
static void checkDecrFailLimit(struct AKMProcessCtx* ctx);
static void checkStateChangeTimeout(struct AKMProcessCtx* ctx);
static void schedNextTimeOut(struct AKMProcessCtx* ctx);
static void updateSendEvent(struct AKMProcessCtx* ctx);

static void handleProcFin(struct AKMProcessCtx* ctx)
{
	struct ProcessingInfo* proc = &ctx->relationship->proc;
	removeTimedOutNodes(ctx);
	updateState(ctx);
	if (proc->yieldProcess || proc->contStack.topIdx > 0)
		return;
	checkDecrFailLimit(ctx);
	if (proc->yieldProcess || proc->contStack.topIdx > 0)
		return;
	checkStateChangeTimeout(ctx);
	if (proc->yieldProcess || proc->contStack.topIdx > 0)
		return;
	schedNextTimeOut(ctx);
	if (proc->yieldProcess || proc->contStack.topIdx > 0)
		return;
	updateSendEvent(ctx);
	if (proc->yieldProcess || proc->contStack.topIdx > 0)
		return;
	resetSkipFlags(ctx);
	yieldProcess(ctx, AKMCmdOpReturn, ctx->relationship->proc.status, 0, NULL);
	proc->status = AKMStSuccess;
	proc->recvFrameSrcNodeIdx = -1;
	proc->recvFrameEvent = AKMEvNone;
	assert(proc->decKey == proc->decTryKey);
}

static void handleCannotDecryptFin(struct AKMProcessCtx* ctx)
{
	if(ctx->relationship->proc.machState == AKM_MFallbackEstablishing)
		ctx->relationship->relCounters.fallback.decryptFails++;
	else
		ctx->relationship->relCounters.normal.decryptFails++;
	ctx->relationship->proc.decTryKey = ctx->relationship->proc.decKey;
}

static void cRetryDec(struct AKMProcessCtx* ctx);
static void cRetryDecTryFb(struct AKMProcessCtx* ctx);

static void retryWithFallbackKey(struct AKMProcessCtx* ctx)
{
	pushContinuation(ctx, cRetryDecTryFb);
	yieldOpRetryDec(ctx, AKM_CFSK);
}

void handleEvCannotDecrypt(struct AKMProcessCtx* ctx)
{
	struct ProcessingInfo* proc = &ctx->relationship->proc;
	switch (proc->machState)
	{
	case AKM_MOffline:
		setRetStatus(ctx, AKMStFatalError);
		break;
	case AKM_MEstablished:
		retryWithFallbackKey(ctx);
		break;
	case AKM_MNormalEstablishing:
		switch (proc->sysState)
		{
		case AKM_SEI:
		case AKM_SEC:
			pushContinuation(ctx, cRetryDec);
			yieldOpRetryDec(ctx, ((proc->decKey == AKM_CSK) ? AKM_NSK : AKM_CSK));
			break;
		default:
			retryWithFallbackKey(ctx);
			break;
		}
		break;
	case AKM_MFallbackEstablishing:
		switch (proc->sysState)
		{
		case AKM_SEI:
		case AKM_SEC:
			pushContinuation(ctx, cRetryDec);
			yieldOpRetryDec(ctx, ((proc->decKey == AKM_CFSK) ? AKM_NFSK : AKM_CFSK));
			break;
		default:
			handleCannotDecryptFin(ctx);
			break;
		}
		break;
	}
}

void cRetryDec(struct AKMProcessCtx* ctx)
{
	popContinuation(ctx);
	struct ProcessingInfo* proc = &ctx->relationship->proc;
	switch (ctx->akmEvent)
	{
	case AKMEvRecvSE:
	case AKMEvRecvSEI:
	case AKMEvRecvSEC:
	case AKMEvRecvSEF:
		pushContinuation(ctx, cDoUseDecTryKeyAsDecKey);
		handleEvRecv(ctx);
		break;
	case AKMEvCannotDecrypt:
		if (proc->machState == AKM_MFallbackEstablishing)
		{
			handleCannotDecryptFin(ctx);
		}
		else
		{
			pushContinuation(ctx, cRetryDecTryFb);
			yieldOpRetryDec(ctx, AKM_CFSK);
		}
		break;
	default:
		setRetStatus(ctx, AKMStFatalError);
		break;
	}
}

void cRetryDecTryFb(struct AKMProcessCtx* ctx)
{
	popContinuation(ctx);
	struct ProcessingInfo* proc = &ctx->relationship->proc;
	switch (ctx->akmEvent)
	{
	case AKMEvRecvSE:
	case AKMEvRecvSEI:
	case AKMEvRecvSEC:
	case AKMEvRecvSEF:
		switchToFallbackEstablishing(ctx);
		handleEvRecv(ctx);
		break;
	case AKMEvCannotDecrypt:
		handleCannotDecryptFin(ctx);
		break;
	default:
		setRetStatus(ctx, AKMStFatalError);
		break;
	}
}

static void cDoHandleRecvEv0(struct AKMProcessCtx* ctx);

static void handleEvRecv(struct AKMProcessCtx* ctx)
{
	struct ProcessingInfo* proc = &ctx->relationship->proc;
	switch (proc->machState)
	{
	case AKM_MOffline:
		break;
	case AKM_MEstablished:
		if (ctx->akmEvent == AKMEvRecvSE)
			break;
		/* fall-through */
	case AKM_MNormalEstablishing:
	case AKM_MFallbackEstablishing:
		proc->recvFrameEvent = ctx->akmEvent;
		proc->recvFrameSrcNodeIdx = findSrcNodeIdx(ctx);
		pushContinuation(ctx, cDoHandleRecvEv0);
		break;
	}
}

static void cDoHandleRecvEv1(struct AKMProcessCtx* ctx);

static void cDoHandleRecvEv0(struct AKMProcessCtx* ctx)
{
	setContinuation(ctx, cDoHandleRecvEv1);
	handleLocalSEI(ctx);
}

static void handleLocalSEI(struct AKMProcessCtx* ctx)
{
	struct ProcessingInfo* proc = &ctx->relationship->proc;
	if (proc->machState == AKM_MEstablished)
	{
		switchToNormalEstablishing(ctx);
	}
}

static enum AKMSysState recvEventToSysState(enum AKMEvent akmEvent)
{
	static_assert((int)AKM_SE == (int)AKMEvRecvSE, "");
	static_assert((int)AKM_SEI == (int)AKMEvRecvSEI, "");
	static_assert((int)AKM_SEC == (int)AKMEvRecvSEC, "");
	static_assert((int)AKM_SEF == (int)AKMEvRecvSEF, "");
	return (enum AKMSysState)akmEvent;
}

static void cDoHandleRecvEv1(struct AKMProcessCtx* ctx)
{
	popContinuation(ctx);
	struct ProcessingInfo* proc = &ctx->relationship->proc;
	if (proc->recvFrameSrcNodeIdx < 0)
	{
		setRetStatus(ctx, AKMStUnknownSource);
	}
	else
	{
		*akm_time_vec_elem(&ctx->relationship->nodeLastRcvTimes, proc->recvFrameSrcNodeIdx) = ctx->time_ms;
		countNodeState(ctx, proc->recvFrameSrcNodeIdx, recvEventToSysState((enum AKMEvent)proc->recvFrameEvent));
	}
}

static void cDoGenNSK(struct AKMProcessCtx* ctx)
{
	popContinuation(ctx);
	AKM_ProcessRandomDataSet(&ctx->relationship->pdv, ctx->relationship->config.CSS, ctx->relationship->proc.keyBuffer, ctx->relationship->config.SK, &ctx->relationship->config.NSS);
	yieldProcess(ctx, AKMCmdOpSetKey, AKM_NSK, ctx->relationship->config.SK, ctx->relationship->proc.keyBuffer);
}

static void cDoGenNFSK(struct AKMProcessCtx* ctx)
{
	popContinuation(ctx);
	AKM_ProcessRandomDataSet(&ctx->relationship->pdv, ctx->relationship->config.FSS, ctx->relationship->proc.keyBuffer, ctx->relationship->config.SK, &ctx->relationship->config.NFSS);
	yieldProcess(ctx, AKMCmdOpSetKey, AKM_NFSK, ctx->relationship->config.SK, ctx->relationship->proc.keyBuffer);
}

static void cDoGenCFSK(struct AKMProcessCtx* ctx)
{
	popContinuation(ctx);
	AKM_ProcessRandomDataSet(&ctx->relationship->pdv, ctx->relationship->config.SFSS, ctx->relationship->proc.keyBuffer, ctx->relationship->config.SK, &ctx->relationship->config.FSS);
	yieldProcess(ctx, AKMCmdOpSetKey, AKM_CFSK, ctx->relationship->config.SK, ctx->relationship->proc.keyBuffer);
}

static void cDoMoveNSKToCSK(struct AKMProcessCtx* ctx)
{
	popContinuation(ctx);
	yieldProcess(ctx, AKMCmdOpMoveKey, AKM_CSK, AKM_NSK, NULL);
	ctx->relationship->config.CSS = ctx->relationship->config.NSS;
}

static void cDoClearKeyBuffer(struct AKMProcessCtx* ctx)
{
	popContinuation(ctx);
	memset(ctx->relationship->proc.keyBuffer, 0, ctx->relationship->config.SK);
}

static void cDoMoveNFSKToCSK(struct AKMProcessCtx* ctx)
{
	popContinuation(ctx);
	yieldProcess(ctx, AKMCmdOpMoveKey, AKM_CSK, AKM_NFSK, NULL);
	ctx->relationship->config.CSS = ctx->relationship->config.NFSS;
	ctx->relationship->config.SFSS = ctx->relationship->config.NSFSS;
	ctx->relationship->config.NSFSS = ctx->relationship->config.FSS;
}

static void regenerateKeysDuringNormalEstablishment(struct AKMProcessCtx* ctx)
{
	pushContinuation(ctx, cDoClearKeyBuffer);
	pushContinuation(ctx, cDoGenNFSK);
	pushContinuation(ctx, cDoGenNSK);
	pushContinuation(ctx, cDoUseCSK);
	pushContinuation(ctx, cDoMoveNSKToCSK);
}

static void regenerateKeysDuringFallbackEstablishment(struct AKMProcessCtx* ctx)
{
	pushContinuation(ctx, cDoClearKeyBuffer);
	pushContinuation(ctx, cDoGenNFSK);
	pushContinuation(ctx, cDoGenNSK);
	pushContinuation(ctx, cDoGenCFSK);
	pushContinuation(ctx, cDoUseCSK);
	pushContinuation(ctx, cDoMoveNFSKToCSK);
}

void updateState(struct AKMProcessCtx* ctx)
{
	struct ProcessingInfo* proc = &ctx->relationship->proc;
	switch (proc->machState)
	{
	case AKM_MOffline:
	case AKM_MEstablished:
		break;
	case AKM_MNormalEstablishing:
	case AKM_MFallbackEstablishing:
		{
			struct RelSubCounters* relCnts = relSubCounters(&ctx->relationship->relCounters, proc->machState);
			enum AKMSysState state = proc->sysState;
			for (int i = 0; i < AKM_NUM_OF_STATES; ++i)
			{
				if (relCnts->nodes[state] >= ctx->relationship->config.N)
				{
					if (state == AKM_SE)
					{
						proc->machState = AKM_MEstablished;
						resetCounters(ctx);
						break;
					}
					else
					{
						state = related_state(state, STREL_NEXT);
						if (state == AKM_SE)
						{
							if (proc->machState == AKM_MFallbackEstablishing)
								regenerateKeysDuringFallbackEstablishment(ctx);
							else
								regenerateKeysDuringNormalEstablishment(ctx);
						}
					}
				}
				else
				{
					break;
				}
			}
			if (proc->sysState != state)
			{
				incrementNodeCnt(ctx, ctx->relationship->selfIdx, state);
				if (state == AKM_SEC || state == AKM_SEF)
				{
					pushContinuation(ctx, (proc->machState == AKM_MFallbackEstablishing) ? cDoUseNFSK : cDoUseNSK);
				}
				proc->sysState = state;
				ctx->relationship->lastStateChangeTime = ctx->time_ms;
			}
		}
		break;
	}
}

void checkDecrFailLimit(struct AKMProcessCtx* ctx)
{
	struct ProcessingInfo* proc = &ctx->relationship->proc;
	const int magicFactor = 10;
	const int limit = magicFactor * ctx->relationship->config.N;
	switch (proc->machState) {
	case AKM_MOffline:
	case AKM_MEstablished:
		break;
	case AKM_MNormalEstablishing:
		if (ctx->relationship->relCounters.normal.decryptFails >= limit)
		{
			switchToFallbackEstablishing(ctx);
		}
		break;
	case AKM_MFallbackEstablishing:
		if (ctx->relationship->relCounters.fallback.decryptFails >= limit)
		{
			// TODO ?
		}
		break;
	}
}

void checkStateChangeTimeout(struct AKMProcessCtx* ctx)
{
	struct ProcessingInfo* proc = &ctx->relationship->proc;
	const akm_time_t time_diff_from_last_state_change = ctx->time_ms - ctx->relationship->lastStateChangeTime;
	switch (proc->machState) {
	case AKM_MOffline:
	case AKM_MEstablished:
		break;
	case AKM_MNormalEstablishing:
		if (time_diff_from_last_state_change > ctx->relationship->config.NSET) {
			switchToFallbackEstablishing(ctx);
		}
		break;
	case AKM_MFallbackEstablishing:
		if (time_diff_from_last_state_change > ctx->relationship->config.FBSET) {
			// TODO ?
		}
		break;
	}
}

static bool calcNextTimeOut(struct AKMProcessCtx* ctx, akm_time_t* pNextTimeOut)
{
	struct ProcessingInfo* proc = &ctx->relationship->proc;
	akm_time_t nextTimeOut;
	switch (proc->machState) {
	case AKM_MOffline:
	case AKM_MEstablished:
		return false;
	case AKM_MNormalEstablishing:
		nextTimeOut = ctx->relationship->lastStateChangeTime + ctx->relationship->config.NSET;
		break;
	case AKM_MFallbackEstablishing:
		nextTimeOut = ctx->relationship->lastStateChangeTime + ctx->relationship->config.FBSET;
		break;
	}
	const akm_time_t nnrt = ctx->relationship->config.NNRT;
	const int nodeCnt = ctx->relationship->config.N;
	akm_time_t* const nodeTimes = akm_time_vec_elem(&ctx->relationship->nodeLastRcvTimes, 0);
	for (int i = 0; i < nodeCnt; ++i)
	{
		if (i == ctx->relationship->selfIdx)
			continue;
		const akm_time_t nodeTimeout = nodeTimes[i] + nnrt;
		if (nodeTimeout < nextTimeOut)
			nextTimeOut = nodeTimeout;
	}
	*pNextTimeOut = nextTimeOut + 1;
	return true;
}

void schedNextTimeOut(struct AKMProcessCtx* ctx)
{
	struct ProcessingInfo* proc = &ctx->relationship->proc;
	if (proc->skipTimeOutSched)
		return;
	proc->skipTimeOutSched = true;
	akm_time_t nextTimeOut;
	if (calcNextTimeOut(ctx, &nextTimeOut))
	{
		assert(nextTimeOut > ctx->time_ms);
		if (!proc->validNextTimeout || nextTimeOut != proc->nextTimeout)
		{
			proc->nextTimeout = nextTimeOut;
			proc->validNextTimeout = true;
			yieldProcess(ctx, AKMCmdOpSetTimer, 0, 0, &proc->nextTimeout);
		}
	}
	else
	{
		if (proc->validNextTimeout)
		{
			yieldProcess(ctx, AKMCmdOpResetTimer, 0, 0, NULL);
			proc->validNextTimeout = false;
		}
	}
}

static int checkUpdateSendEvent(struct AKMProcessCtx* ctx, int* pSendEvent)
{
	struct ProcessingInfo* proc = &ctx->relationship->proc;
	*pSendEvent = (proc->machState != AKM_MOffline && proc->machState != AKM_MEstablished) ? proc->sysState : AKM_SE;
	return proc->machState != AKM_MOffline;

}

static void doUpdateSendEvent(struct AKMProcessCtx* ctx)
{
	popContinuation(ctx);
	int sendEvent;
	const int sendOk = checkUpdateSendEvent(ctx, &sendEvent);
	yieldProcess(ctx, AKMCmdOpSetSendEvent, sendOk, sendEvent, NULL);
	struct ProcessingInfo* proc = &ctx->relationship->proc;
	proc->sendOk = (int8_t)sendOk;
	proc->sendEvent = (int8_t)sendEvent;
}

static void updateSendEvent(struct AKMProcessCtx* ctx)
{
	int sendEvent;
	const int sendOk = checkUpdateSendEvent(ctx, &sendEvent);
	struct ProcessingInfo* proc = &ctx->relationship->proc;
	if (sendOk != proc->sendOk || sendEvent != proc->sendEvent)
	{
		pushContinuation(ctx, doUpdateSendEvent);
	}
}

