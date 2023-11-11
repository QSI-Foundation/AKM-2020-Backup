/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */


#include "bytevector.h"
#include <stdlib.h>
#include <string.h>

bool bytevector_resize(bytevector* vec, size_t newSize)
{
	if (newSize > vec->capacity)
	{
		size_t newCapacity = vec->capacity + (vec->capacity >> 1);
		if (newCapacity < newSize)
			newCapacity = newSize;
		if (!bytevector_change_capacity(vec, newCapacity))
		{
			if (newSize < newCapacity)
			{
				if (!bytevector_change_capacity(vec, newSize))
					return false;
			}
			else
				return false;
		}
	}
	vec->size = newSize;
	return true;
}

bool bytevector_change_capacity(bytevector* vec, size_t newCapacity)
{
	if (newCapacity == vec->capacity)
		return true;
	if (newCapacity < 1)
	{
		if (vec->buffer)
		{
			free(vec->buffer);
			vec->buffer = NULL;
		}
	}
	else
	{
		void* newBuffer = realloc(vec->buffer, newCapacity);
		if (!newBuffer)
			return false;
		vec->buffer = newBuffer;
	}
	if (newCapacity < vec->size)
		vec->size = newCapacity;
	vec->capacity = newCapacity;
	return true;
}

bool bytevector_insert(bytevector* vec, size_t offset, const void* data, size_t len)
{
	if (len < 1)
		return true;
	const size_t oldSize = vec->size;
	if (offset > oldSize)
		return false;
	if (!bytevector_resize(vec, oldSize + len))
		return false;
	if (offset < oldSize)
		memmove(bytevector_getptr(vec, offset + len), bytevector_getptr(vec, offset), oldSize - offset);
	if (data)
		memcpy(bytevector_getptr(vec, offset), data, len);
	return true;
}

bool bytevector_erase(bytevector* vec, size_t offset, size_t len)
{
	if (len < 1)
		return true;
	const size_t oldSize = vec->size;
	if (offset >= oldSize)
		return true;
	const size_t eoff = offset + len;
	if (eoff >= oldSize)
		return bytevector_resize(vec, offset);
	memmove(bytevector_getptr(vec, offset), bytevector_getptr(vec, eoff), oldSize - eoff);
	return bytevector_resize(vec, oldSize - len);
}
