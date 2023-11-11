/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */


#ifndef AKM_INTERNAL_H
#define AKM_INTERNAL_H

#include "akm.h"
#include "addr_list.h"
#include "bytevector.h"
#include <string.h>

DEFINE_VECTOR_T(akm_time_vec,akm_time_t)

enum AKMKey
{
	AKM_CSK = 0,
	AKM_NSK = 1,
	AKM_CFSK = 2,
	AKM_NFSK = 3,
};

static inline isFallbackKey(enum AKMKey key) { return key == AKM_CFSK || key == AKM_NFSK;  }

#define AKM_NUM_OF_STATES 4

enum AKMSysState
{
	AKM_SE = 0,
	AKM_SEI = 1,
	AKM_SEC = 2,
	AKM_SEF = 3,
};

enum AKMSysStateRel {
	STREL_SAME = 0,
	STREL_NEXT = 1,
	STREL_CROSS = 2,
	STREL_PREV = 3,
};

static inline enum AKMSysStateRel states_relation(enum AKMSysState base, enum AKMSysState state) {
	return (enum AKMSysStateRel)((((unsigned)state) - ((unsigned)base)) & 0x03);
}

static inline enum AKMSysState related_state(enum AKMSysState base, enum AKMSysStateRel rel) {
	return (enum AKMSysState)((((unsigned)base) + ((unsigned)rel)) & 0x03);
}

enum AKMMachState
{
	AKM_MOffline = 0,
	AKM_MEstablished = 1,
	AKM_MNormalEstablishing = 2,
	AKM_MFallbackEstablishing = 3,
};

struct NodeSubCounters
{
	int cnts[AKM_NUM_OF_STATES];
};

struct NodeCounters
{
	struct NodeSubCounters normal;
	struct NodeSubCounters fallback;
};

static inline struct NodeSubCounters* nodeSubCounters(struct NodeCounters* cnts, enum AKMMachState machState) { return (machState == AKM_MFallbackEstablishing) ? &cnts->fallback : &cnts->normal; }

DEFINE_VECTOR_T(NodeCntsVec,struct NodeCounters)

struct RelSubCounters
{
	int nodes[AKM_NUM_OF_STATES];
	int decryptFails;
};

struct RelCounters
{
	struct RelSubCounters normal;
	struct RelSubCounters fallback;
};

static inline struct RelSubCounters* relSubCounters(struct RelCounters* cnts, enum AKMMachState machState) { return (machState == AKM_MFallbackEstablishing) ? &cnts->fallback : &cnts->normal; }

#define CONTINUATION_STACK_DEPTH 16

typedef void(*cont_func_t)(struct AKMProcessCtx*);

struct ContinuationStack
{
	int topIdx;
	cont_func_t stack[CONTINUATION_STACK_DEPTH];
};

static inline void contStack_setContinuation(struct ContinuationStack* cs, cont_func_t cont) { cs->stack[cs->topIdx] = cont; }
static inline void contStack_pushContinuation(struct ContinuationStack* cs, cont_func_t cont) { cs->topIdx++; contStack_setContinuation(cs, cont); }
static inline void contStack_popContinuation(struct ContinuationStack* cs) { contStack_setContinuation(cs, NULL); cs->topIdx--; }
static inline cont_func_t contStack_getContinuation(struct ContinuationStack* cs) { return cs->stack[cs->topIdx]; }

struct ProcessingInfo
{
	akm_time_t nextTimeout;
	bool validNextTimeout;
	bool skipTimeOutNodesRemoval, skipTimeOutSched;
	bool yieldProcess;
	int8_t status;
	int8_t encKey, decKey, decTryKey;
	int8_t sysState, machState;
	int8_t sendEvent, sendOk;
	int8_t recvFrameEvent;
	int recvFrameSrcNodeIdx;
	void* keyBuffer;
	struct ContinuationStack contStack;
};

struct AKMRelationship
{
	struct ProcessingInfo proc;
	int selfIdx;
	struct AKMConfigParams config;
	struct AKMParameterDataVector pdv;
	bytevector nodeAddresses;
	akm_time_t lastStateChangeTime;
	akm_time_vec nodeLastRcvTimes;
	struct RelCounters relCounters;
	NodeCntsVec nodeCounters;
};

static inline void setContinuation(struct AKMProcessCtx* ctx, cont_func_t cont) { contStack_setContinuation(&ctx->relationship->proc.contStack, cont); }
static inline void pushContinuation(struct AKMProcessCtx* ctx, cont_func_t cont) { contStack_pushContinuation(&ctx->relationship->proc.contStack, cont); }
static inline void popContinuation(struct AKMProcessCtx* ctx) { contStack_popContinuation(&ctx->relationship->proc.contStack); }
static inline cont_func_t getContinuation(struct AKMProcessCtx* ctx) { return contStack_getContinuation(&ctx->relationship->proc.contStack); }

static inline void resetCounters(struct AKMProcessCtx* ctx)
{
	memset(&ctx->relationship->relCounters, 0, sizeof(ctx->relationship->relCounters));
	NodeCntsVec_zero(&ctx->relationship->nodeCounters);
}

static inline void resetSkipFlags(struct AKMProcessCtx* ctx)
{
	struct ProcessingInfo* proc = &ctx->relationship->proc;
	proc->skipTimeOutNodesRemoval = false;
	proc->skipTimeOutSched = false;
}

#endif
