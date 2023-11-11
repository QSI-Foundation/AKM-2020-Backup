/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */


#ifndef INC_ADDR_LIST_H_
#define INC_ADDR_LIST_H_

#include <stdint.h>
#include "bytevector.h"

int addrlist_calc_size_check(const int addrNum, const int addrSize);

int addrlist_check_sorted_nodups_raw(const uint8_t* const buffer, const int addrNum, const int addrSize);

int addrlist_find_idx_raw(const uint8_t* const buffer, const int addrNum, const int addrSize, const uint8_t* const address);

static inline int addrlist_find_idx_vec(const bytevector* const vec, const int addrSize, const uint8_t* const address)
{
	return addrlist_find_idx_raw(vec->buffer, (int)(vec->size / addrSize), addrSize, address);
}

static inline void addrlist_remove_by_idx_vec(bytevector* const vec, const int addrSize, int const idx)
{
	bytevector_erase(vec, idx * addrSize, addrSize);
}

#endif /* INC_ADDR_LIST_H_ */
