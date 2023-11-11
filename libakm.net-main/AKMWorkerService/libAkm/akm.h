#ifndef LIBAKM_H
#define LIBAKM_H

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif /* __cplusplus */

#ifdef LIBAKM_EXPORTS
#define LIBAKM_PUBLIC __declspec(dllexport)
#else
#define LIBAKM_PUBLIC __declspec(dllimport)
#endif

typedef int64_t akm_time_t;

enum AKMEvent
{
	AKMEvNone = -1,
	AKMEvRecvSE = 0,
	AKMEvRecvSEI = 1,
	AKMEvRecvSEC = 2,
	AKMEvRecvSEF = 3,
	AKMEvCannotDecrypt = 4,
	AKMEvTimeOut = 5,
};

enum AKMCmdOpcode
{
	AKMCmdOpReturn = 0,
	AKMCmdOpSetSendEvent = 1,
	AKMCmdOpSetKey = 2,
	AKMCmdOpResetKey = 3,
	AKMCmdOpMoveKey = 4,
	AKMCmdOpUseKeys = 5,
	AKMCmdOpRetryDec = 6,
	AKMCmdOpSetTimer = 7,
	AKMCmdOpResetTimer = 8,
};

enum AKMStatus
{
	AKMStSuccess = 0,
	AKMStNoMemory = 1,
	AKMStUnknownSource = 2,
	AKMStFatalError = 3,
};

#define  AKM_PARAMETER_DATA_VECTOR_SIZE     128

struct AKMParameterDataVector
{
	uint8_t data[AKM_PARAMETER_DATA_VECTOR_SIZE];
};

struct AKMConfigParams
{
	// Size of Key
	uint8_t SK;
	// Size of Ring Node Addresses
	uint8_t SRNA;
	// Number of Nodes within AKM Relationship/Echo Ring
	uint16_t N;
	// Current Session Seed
	uint32_t CSS;
	// Next Session Seed
	uint32_t NSS;
	// Fallback Session Seed
	uint32_t FSS;
	// Next Fallback Session Seed
	uint32_t NFSS;
	// Shadow Fallback Session Seed
	uint32_t SFSS;
	// Next Shadow Fallback Session Seed
	uint32_t NSFSS;
	// Emergency Failsafe Session Seed
	uint32_t EFSS;
	// Node Nonresponse Timeout
	akm_time_t NNRT;
	// Normal Session Establishment Timeout
	akm_time_t NSET;
	// Fallback Resynchronization Session Establishment Timeout
	akm_time_t FBSET;
	// Failsafe Resynchronization Session Establishment Timeout
	akm_time_t FSSET;
};

struct AKMConfiguration
{
	struct AKMConfigParams params;
	const struct AKMParameterDataVector* pdv;
	const void* nodeAddresses;
	const void* selfNodeAddress;
};

struct AKMRelationship;

struct AKMCommand
{
	enum AKMCmdOpcode opcode;
	int p1, p2;
	const void* data;
};

struct AKMProcessCtx
{
	struct AKMRelationship* relationship;
	enum AKMEvent akmEvent;
	const void* srcAddr;
	akm_time_t time_ms;
	struct AKMCommand cmd;
};

LIBAKM_PUBLIC enum AKMStatus AKMInit(struct AKMProcessCtx* ctx, const struct AKMConfiguration* config);

LIBAKM_PUBLIC void AKMFree(struct AKMRelationship* relationship);

LIBAKM_PUBLIC void AKMProcess(struct AKMProcessCtx* ctx);

#ifdef __cplusplus
} /* extern "C" */
#endif /* __cplusplus */

#endif
