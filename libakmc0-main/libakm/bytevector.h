/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */


#ifndef INC_BYTEVECTOR_H
#define INC_BYTEVECTOR_H

#include <stddef.h>
#include <stdbool.h>
#include <string.h>

typedef struct bytevector
{
	void* buffer;
	size_t size;
	size_t capacity;
} bytevector;

static inline void* bytevector_getptr(bytevector* vec, size_t offset) { return (char*)(vec->buffer) + offset; }
bool bytevector_change_capacity(bytevector* vec, size_t newCapacity);
bool bytevector_resize(bytevector* vec, size_t newSize);
bool bytevector_insert(bytevector* vec, size_t offset, const void* data, size_t len);
bool bytevector_erase(bytevector* vec, size_t offset, size_t len);
static inline bool bytevector_free(bytevector* vec) { return bytevector_change_capacity(vec, 0); }
static inline bool bytevector_shrink_to_fit(bytevector* vec) { return bytevector_change_capacity(vec, vec->size); }
static inline void bytevector_zero(bytevector* vec) { if (vec->buffer) { memset(vec->buffer, 0, vec->size); } }

#define DEFINE_VECTOR_T(VecType,ElemType) \
	typedef struct VecType	\
	{	\
		bytevector vec;	\
	} VecType;	\
	\
	static inline size_t VecType ## _count(VecType* vec) { return vec->vec.size / sizeof(ElemType); }	\
	static inline ElemType* VecType ## _elem(VecType* vec, size_t idx) { return (ElemType*)bytevector_getptr(&vec->vec, idx * sizeof(ElemType)); }	\
	static inline bool VecType ## _resize(VecType* vec, size_t newSize) { return bytevector_resize(&vec->vec, newSize * sizeof(ElemType)); }	\
	static inline bool VecType ## _insert(VecType* vec, size_t idx, const ElemType* elem, size_t cnt) { return bytevector_insert(&vec->vec, idx * sizeof(ElemType), elem, cnt * sizeof(ElemType)); }	\
	static inline bool VecType ## _erase(VecType* vec, size_t idx, size_t cnt) { return bytevector_erase(&vec->vec, idx * sizeof(ElemType), cnt * sizeof(ElemType) ); }	\
	static inline bool VecType ## _free(VecType* vec) { return bytevector_free(&vec->vec); }	\
	static inline bool VecType ## _shrink_to_fit(VecType* vec) { return bytevector_shrink_to_fit(&vec->vec); }	\
	static inline void VecType ## _zero(VecType* vec) { bytevector_zero(&vec->vec); }

#endif
