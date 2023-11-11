/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */


#ifndef INC_AKM_CORE_H_
#define INC_AKM_CORE_H_

#include <stddef.h>
#include <stdint.h>
#include "akm.h"

uint32_t AKM_Modulo_64K_RandomValue(uint32_t RandomSeedValue);
void AKM_ProcessRandomDataSet(const struct AKMParameterDataVector* PDV, uint32_t SeedToUseForProcessingPDV, void* pNewEncryptionKey, size_t keyLen, uint32_t* pNewSeed);

#endif /* INC_AKM_CORE_H_ */
