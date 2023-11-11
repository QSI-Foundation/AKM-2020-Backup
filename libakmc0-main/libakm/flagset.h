/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */


#ifndef INC_FLAGSET_H_
#define INC_FLAGSET_H_

#include <stddef.h>
#include <stdint.h>
#include <stdbool.h>

typedef uint32_t flagset_word_t;

#define FLAGSET_WORD_SIZE_SHIFT 5
#define FLAGSET_WORD_BITS (1 << FLAGSET_WORD_SIZE_SHIFT)
#define FLAGSET_WORDIDX_SHIFT FLAGSET_WORD_SIZE_SHIFT
#define FLAGSET_SUBIDX_MASK (FLAGSET_WORD_BITS - 1)
#define FLAGSET_ARRAY_LEN(n) (((n) + FLAGSET_SUBIDX_MASK) >> FLAGSET_WORDIDX_SHIFT)
#define FLAGSET_WORD_IDX(i) ((i) >> FLAGSET_WORDIDX_SHIFT)
#define FLAGSET_SUB_IDX(i) ((i) & FLAGSET_SUBIDX_MASK)
#define FLAGSET_SUB_BIT(i) (((flagset_word_t)1) << FLAGSET_SUB_IDX(i))
#define FLAGSET_ARRAY_GET(a, i) ((((a)[FLAGSET_WORD_IDX(i)]) >> FLAGSET_SUB_IDX(i)) & 1)
#define FLAGSET_ARRAY_SET_ONE(a, i) (((a)[FLAGSET_WORD_IDX(i)]) |= FLAGSET_SUB_BIT(i))
#define FLAGSET_ARRAY_SET_ZERO(a, i) (((a)[FLAGSET_WORD_IDX(i)]) &= ~ FLAGSET_SUB_BIT(i))
#define FLAGSET_ARRAY_SET(a, i, v) ((v) ? FLAGSET_ARRAY_SET_ONE((a), (i)) : FLAGSET_ARRAY_SET_ZERO((a), (i)))
#define FLAGSET_ARRAY_TOGGLE(a, i) (((a)[FLAGSET_WORD_IDX(i)]) ^= FLAGSET_SUB_BIT(i))

static inline bool flagset_array_get(const flagset_word_t* fs, const size_t i) {
	return FLAGSET_ARRAY_GET(fs, i);
}

static inline void flagset_array_set(flagset_word_t* fs, const size_t i, const bool v) {
	FLAGSET_ARRAY_SET(fs, i, v);
}

static inline void flagset_array_toggle(flagset_word_t* fs, const size_t i) {
	FLAGSET_ARRAY_TOGGLE(fs, i);
}

#define FLAGSET_T(n) struct { flagset_word_t array[FLAGSET_ARRAY_LEN(n)]; }
#define FLAGSET_GET(f, i) (flagset_array_get((f).array, (i)))
#define FLAGSET_SET(f, i, v) (flagset_array_set((f).array, (i), (v)))
#define FLAGSET_TOGGLE(f, i) (flagset_array_toggle((f).array, (i)))


#endif /* INC_FLAGSET_H_ */
