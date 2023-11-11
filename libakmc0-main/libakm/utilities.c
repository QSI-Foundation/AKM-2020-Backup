/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */


#include "utilities.h"
#include <stdint.h>

bool memIsZero(const void* mem, size_t n) {
	const uint8_t* p = (const uint8_t*)mem;
	unsigned x = 0;
	while(n-- > 0)
		x |= *p++;
	return ((x - 1) >> 8) & 1;
}
