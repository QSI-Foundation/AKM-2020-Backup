/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */


#include "akm_core.h"
#include "sha256.h"
#include "flagset.h"
#include "endianness.h"
#include "utilities.h"
#include <string.h>

#define  TWO_TO_THE_31ST                2147483648
#define  TWO_TO_THE_32ND_POWER_MINUS_1  0xFFFFFFFF
#define  TWO_TO_THE_16TH                65536
#define  MINIMUM_ALLOWED_PDV_SUBSET     32
#define  AKM_PRIME_NUMBERS_ARRAY_LEN    32

static const uint8_t FirstThirtyTwoPrimes [AKM_PRIME_NUMBERS_ARRAY_LEN] = {
      2,      3,     5,     7,    11,    13,    17,    19,    23,    29,
     31,     37,    41,    43,    47,    53,    59,    61,    67,    71,
     73,     79,    83,    89,    97,   101,   103,   107,   109,   113,
    127,   131
};


uint32_t AKM_Modulo_64K_RandomValue(uint32_t RandomSeedValue) {
	uint32_t  random = RandomSeedValue;

	while (random < TWO_TO_THE_31ST) {
		random <<= 1;

		const int i = random % AKM_PRIME_NUMBERS_ARRAY_LEN;
		const uint16_t prime = FirstThirtyTwoPrimes[i];
		if ((TWO_TO_THE_32ND_POWER_MINUS_1 - random) > (2u * prime)) {
			random += prime;
			random += random % prime;
		}
	}

	return (uint32_t) (random % TWO_TO_THE_16TH);
}

void AKM_ProcessRandomDataSet(const struct AKMParameterDataVector* PDV, uint32_t SeedToUseForProcessingPDV, void* pNewEncryptionKey, size_t keyLen, uint32_t* pNewSeed) {

    int                                 SelectedIndex;
    int                                 RandomIndex;
    uint32_t                            Difference;
    uint32_t                            ParameterDataSubsetSize;
    uint32_t                            NumberOfParametersSelected;
    uint32_t                            ParameterSelectionSeed;
    uint32_t                            random1;
    uint32_t                            random2;
    uint32_t                            random3;
    FLAGSET_T(AKM_PARAMETER_DATA_VECTOR_SIZE) SelectedParameterFlags;
    uint8_t                             SelectedPDV[AKM_PARAMETER_DATA_VECTOR_SIZE];
    uint8_t                             SelectedPDVDigest[SHA256_DIGEST_SIZE];

    memset(&SelectedParameterFlags, 0, sizeof(SelectedParameterFlags));
    ParameterDataSubsetSize = AKM_Modulo_64K_RandomValue(SeedToUseForProcessingPDV) % AKM_PARAMETER_DATA_VECTOR_SIZE;

    while ((ParameterDataSubsetSize < MINIMUM_ALLOWED_PDV_SUBSET) || (ParameterDataSubsetSize == AKM_PARAMETER_DATA_VECTOR_SIZE)) {
        random1 = FirstThirtyTwoPrimes [ParameterDataSubsetSize % AKM_PRIME_NUMBERS_ARRAY_LEN];
        random2 = (ParameterDataSubsetSize << 1) + random1;
        random3 = random2 % random1;
        if (random3 == 0) {
            ParameterDataSubsetSize = (random1 + random2) % AKM_PARAMETER_DATA_VECTOR_SIZE;
        } else {
            ParameterDataSubsetSize = random3 % AKM_PARAMETER_DATA_VECTOR_SIZE;
        }
    }
    NumberOfParametersSelected = 0;
    ParameterSelectionSeed = SeedToUseForProcessingPDV;
    while (NumberOfParametersSelected < ParameterDataSubsetSize) {

        SelectedIndex = AKM_Modulo_64K_RandomValue(ParameterSelectionSeed) % AKM_PARAMETER_DATA_VECTOR_SIZE;

        if (!FLAGSET_GET(SelectedParameterFlags, SelectedIndex)) {
            SelectedPDV[NumberOfParametersSelected] = PDV->data[SelectedIndex];
            FLAGSET_SET(SelectedParameterFlags, SelectedIndex, true);
            NumberOfParametersSelected++;
        }

        RandomIndex = ParameterSelectionSeed % FirstThirtyTwoPrimes [SelectedIndex % AKM_PRIME_NUMBERS_ARRAY_LEN];
        RandomIndex = RandomIndex % AKM_PRIME_NUMBERS_ARRAY_LEN;

        Difference = TWO_TO_THE_32ND_POWER_MINUS_1 - ParameterSelectionSeed;

        if (Difference > FirstThirtyTwoPrimes [RandomIndex]) {
            ParameterSelectionSeed += FirstThirtyTwoPrimes [RandomIndex];
        } else {
            ParameterSelectionSeed -= Difference;
        }
    }

    SHA256_calc(SelectedPDV, ParameterDataSubsetSize, SelectedPDVDigest);

	memCpyEx(pNewEncryptionKey, keyLen, SelectedPDVDigest, SHA256_DIGEST_SIZE, 0);

    const uint8_t newSeedBytes[4] = {
        SelectedPDVDigest[0],
        SelectedPDVDigest[5],
        SelectedPDVDigest[10],
        SelectedPDVDigest[15],
    };

    *pNewSeed = read_le32(newSeedBytes);
}
