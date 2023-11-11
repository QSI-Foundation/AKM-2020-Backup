/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */


#ifndef INC_UTILITIES_H_
#define INC_UTILITIES_H_

#include <stddef.h>
#include <stdbool.h>
#include <string.h>
#include <assert.h>

#ifdef _MSC_VER

#ifdef NDEBUG

#define unreachable() _assume(0)

#define assume(cond) _assume(cond)

#else

#define unreachable() assert(0)

#define assume(cond) assert(cond)

#endif

#define likely(cond) (cond)

#define unlikely(cond) (cond)

#define restrict __restrict

#else

#ifdef NDEBUG

#define unreachable __builtin_unreachable

#define assume(cond) do { if(!(cond)) { unreachable(); } } while(0)

#else

#define unreachable() assert(0)

#define assume(cond) assert(cond)

#endif

#define expect __builtin_expect

#define likely(cond) expect(!!(cond),1)

#define unlikely(cond) expect(!!(cond),0)

#endif

bool memIsZero(const void* mem, size_t n);

static inline void memCpyEx(void* restrict dst, size_t dst_len, const void* restrict src, size_t src_len, int pad_fill) {
	const size_t cp_len = ((dst_len < src_len) ? dst_len : src_len);
	memcpy(dst, src, cp_len);
	const size_t pad_len = dst_len - cp_len;
	if(pad_len > 0) {
		memset(((char*)dst) + cp_len, pad_fill, pad_len);
	}
}

#endif /* INC_UTILITIES_H_ */
