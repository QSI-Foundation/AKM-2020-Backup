/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */


#include "addr_list.h"
#include "utilities.h"
#include <string.h>

int addrlist_calc_size_check(const int addrNum, const int addrSize) {
	if(unlikely((addrSize < 1)
			|| (addrNum < 0)
			|| ((addrSize == 1) && (addrNum > 256))))
		return -1;
	if(addrSize == 1)
		return addrNum;
	const int result = addrNum * addrSize;
	return result;
}

static inline int addrlist_cmp_addrs_rev(const uint8_t* const p, const uint8_t* const q, const int negAddrSize) {
	for(int i = 0; i > negAddrSize; --i) {
		const int diff = p[i] - q[i];
		if(diff != 0)
			return diff;
	}
	return 0;
}

int addrlist_check_sorted_nodups_raw(const uint8_t* const buffer, const int addrNum, const int addrSize) {
	if(unlikely(addrSize < 1))
		return -1;
	if(addrNum < 2)
		return 0;
	const int negAddrSize = -addrSize;
	const uint8_t* p = buffer + addrSize - 1;
	for(int i = 1; i < addrNum; ++i) {
		const uint8_t* const q = p + addrSize;
		if(unlikely(addrlist_cmp_addrs_rev(p, q, negAddrSize) >= 0))
			return -1;
		p = q;
	}
	return 0;
}

static int addrlist_find_idx_raw_generic(const uint8_t* const buffer, const int addrNum, const int addrSize, const uint8_t* const address);
static int addrlist_find_idx_raw_size_1B(const uint8_t* const buffer, const int addrNum, const int addrSize, const uint8_t* const address);

int addrlist_find_idx_raw(const uint8_t* const buffer, const int addrNum, const int addrSize, const uint8_t* const address) {
	switch(addrSize) {
		case 1:
			return addrlist_find_idx_raw_size_1B(buffer, addrNum, addrSize, address);
		default:
			return addrlist_find_idx_raw_generic(buffer, addrNum, addrSize, address);
	}
}

int addrlist_find_idx_raw_generic(const uint8_t* const buffer, const int addrNum, const int addrSize, const uint8_t* const address) {
	if(unlikely(addrSize < 1))
		return -1;
	const int negAddrSize = -addrSize;
	const uint8_t* const addrEnd = address + addrSize - 1;
	const uint8_t* const buffSh = buffer + addrSize - 1;
	int l = 0;
	int r = addrNum;
	while (l < r) {
		const int i = l + (r - l) / 2;
		const int c = addrlist_cmp_addrs_rev(addrEnd, buffSh + i * addrSize, negAddrSize);
		if (c < 0)
			r = i;
		else if (c > 0)
			l = i + 1;
		else
			return i;
	}
	return -1;
}

int addrlist_find_idx_raw_size_1B(const uint8_t* const buffer, const int addrNum, const int addrSize, const uint8_t* const address) {
	(void)addrSize;
	const uint8_t addressVal = *address;
	int l = 0;
	int r = addrNum;
	while (l < r) {
		const int i = l + (r - l) / 2;
		const uint8_t a = buffer[i];
		if (addressVal < a)
			r = i;
		else if (addressVal > a)
			l = i + 1;
		else
			return i;
	}
	return -1;
}
