/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */


#ifndef INC_ENDIANNESS_H_
#define INC_ENDIANNESS_H_

#include <stddef.h>
#include <stdint.h>
#include <assert.h>

static inline uint16_t read_be16(const void* buf)
{
    const uint8_t* p = (const uint8_t*)(buf);
    return ((uint16_t)(p[0]) << 8) | ((uint16_t)(p[1]));
}

static inline uint16_t read_le16(const void* buf)
{
    const uint8_t* p = (const uint8_t*)(buf);
    return ((uint16_t)(p[1]) << 8) | ((uint16_t)(p[0]));
}

static inline uint32_t read_be24(const void* buf)
{
    const uint8_t* p = (const uint8_t*)(buf);
    return ((uint32_t)(p[0]) << 16) | ((uint32_t)(p[1]) << 8) | ((uint32_t)(p[2]));
}

static inline uint32_t read_le24(const void* buf)
{
    const uint8_t* p = (const uint8_t*)(buf);
    return ((uint32_t)(p[2]) << 16) | ((uint32_t)(p[1]) << 8) | ((uint32_t)(p[0]));
}

static inline uint32_t read_be32(const void* buf)
{
    const uint8_t* p = (const uint8_t*)(buf);
    return ((uint32_t)(p[0]) << 24) | ((uint32_t)(p[1]) << 16) | ((uint32_t)(p[2]) << 8) | ((uint32_t)(p[3]));
}

static inline uint32_t read_le32(const void* buf)
{
    const uint8_t* p = (const uint8_t*)(buf);
    return ((uint32_t)(p[3]) << 24) | ((uint32_t)(p[2]) << 16) | ((uint32_t)(p[1]) << 8) | ((uint32_t)(p[0]));
}

static inline uint64_t read_be48(const void* buf)
{
    const uint8_t* p = (const uint8_t*)(buf);
    return ((uint64_t)(p[0]) << 40) | ((uint64_t)(p[1]) << 32) | ((uint64_t)(p[2]) << 24) | ((uint64_t)(p[3]) << 16) | ((uint64_t)(p[4]) << 8) | ((uint64_t)(p[5]));
}

static inline uint64_t read_le48(const void* buf)
{
    const uint8_t* p = (const uint8_t*)(buf);
    return ((uint64_t)(p[5]) << 40) | ((uint64_t)(p[4]) << 32) | ((uint64_t)(p[3]) << 24) | ((uint64_t)(p[2]) << 16) | ((uint64_t)(p[1]) << 8) | ((uint64_t)(p[0]));
}

static inline uint64_t read_be64(const void* buf)
{
    const uint8_t* p = (const uint8_t*)(buf);
    return ((uint64_t)(p[0]) << 56) | ((uint64_t)(p[1]) << 48) | ((uint64_t)(p[2]) << 40) | ((uint64_t)(p[3]) << 32) | ((uint64_t)(p[4]) << 24) | ((uint64_t)(p[5]) << 16) | ((uint64_t)(p[6]) << 8) | ((uint64_t)(p[7]));
}

static inline uint64_t read_le64(const void* buf)
{
    const uint8_t* p = (const uint8_t*)(buf);
    return ((uint64_t)(p[7]) << 56) | ((uint64_t)(p[6]) << 48) | ((uint64_t)(p[5]) << 40) | ((uint64_t)(p[4]) << 32) | ((uint64_t)(p[3]) << 24) | ((uint64_t)(p[2]) << 16) | ((uint64_t)(p[1]) << 8) | ((uint64_t)(p[0]));
}

static inline uint8_t get16l8(uint16_t x) { return (uint8_t)(x); }
static inline uint8_t get16h8(uint16_t x) { return (uint8_t)(x >> 8); }
static inline uint8_t get32ll8(uint32_t x) { return (uint8_t)(x); }
static inline uint8_t get32lh8(uint32_t x) { return (uint8_t)(x >> 8); }
static inline uint8_t get32hl8(uint32_t x) { return (uint8_t)(x >> 16); }
static inline uint8_t get32hh8(uint32_t x) { return (uint8_t)(x >> 24); }
static inline uint8_t get64lll8(uint64_t x) { return (uint8_t)(x); }
static inline uint8_t get64llh8(uint64_t x) { return (uint8_t)(x >> 8); }
static inline uint8_t get64lhl8(uint64_t x) { return (uint8_t)(x >> 16); }
static inline uint8_t get64lhh8(uint64_t x) { return (uint8_t)(x >> 24); }
static inline uint8_t get64hll8(uint64_t x) { return (uint8_t)(x >> 32); }
static inline uint8_t get64hlh8(uint64_t x) { return (uint8_t)(x >> 40); }
static inline uint8_t get64hhl8(uint64_t x) { return (uint8_t)(x >> 48); }
static inline uint8_t get64hhh8(uint64_t x) { return (uint8_t)(x >> 56); }

static inline void write_be16(void* buf, uint16_t x)
{
    uint8_t* p = (uint8_t*)(buf);
    p[0] = get16h8(x);
    p[1] = get16l8(x);
}

static inline void write_le16(void* buf, uint16_t x)
{
    uint8_t* p = (uint8_t*)(buf);
    p[0] = get16l8(x);
    p[1] = get16h8(x);
}

static inline void write_be24(void* buf, uint32_t x)
{
    uint8_t* p = (uint8_t*)(buf);
    p[0] = get32hl8(x);
    p[1] = get32lh8(x);
    p[2] = get32ll8(x);
}

static inline void write_le24(void* buf, uint32_t x)
{
    uint8_t* p = (uint8_t*)(buf);
    p[0] = get32ll8(x);
    p[1] = get32lh8(x);
    p[2] = get32hl8(x);
}

static inline void write_be32(void* buf, uint32_t x)
{
    uint8_t* p = (uint8_t*)(buf);
    p[0] = get32hh8(x);
    p[1] = get32hl8(x);
    p[2] = get32lh8(x);
    p[3] = get32ll8(x);
}

static inline void write_le32(void* buf, uint32_t x)
{
    uint8_t* p = (uint8_t*)(buf);
    p[0] = get32ll8(x);
    p[1] = get32lh8(x);
    p[2] = get32hl8(x);
    p[3] = get32hh8(x);
}

static inline void write_be48(void* buf, uint64_t x)
{
    uint8_t* p = (uint8_t*)(buf);
    p[0] = get64hlh8(x);
    p[1] = get64hll8(x);
    p[2] = get64lhh8(x);
    p[3] = get64lhl8(x);
    p[4] = get64llh8(x);
    p[5] = get64lll8(x);
}

static inline void write_le48(void* buf, uint64_t x)
{
    uint8_t* p = (uint8_t*)(buf);
    p[0] = get64lll8(x);
    p[1] = get64llh8(x);
    p[2] = get64lhl8(x);
    p[3] = get64lhh8(x);
    p[4] = get64hll8(x);
    p[5] = get64hlh8(x);
}

static inline void write_be64(void* buf, uint64_t x)
{
    uint8_t* p = (uint8_t*)(buf);
    p[0] = get64hhh8(x);
    p[1] = get64hhl8(x);
    p[2] = get64hlh8(x);
    p[3] = get64hll8(x);
    p[4] = get64lhh8(x);
    p[5] = get64lhl8(x);
    p[6] = get64llh8(x);
    p[7] = get64lll8(x);
}

static inline void write_le64(void* buf, uint64_t x)
{
    uint8_t* p = (uint8_t*)(buf);
    p[0] = get64lll8(x);
    p[1] = get64llh8(x);
    p[2] = get64lhl8(x);
    p[3] = get64lhh8(x);
    p[4] = get64hll8(x);
    p[5] = get64hlh8(x);
    p[6] = get64hhl8(x);
    p[7] = get64hhh8(x);
}

static inline uint64_t read_be_bytes(const void* buf, size_t n)
{
    const uint8_t* p = (const uint8_t*)(buf);
    uint64_t res = 0;
    assert(n <= sizeof(res));
    for(size_t i = 0; i < n; ++i)
    {
        res = (res << 8) | p[i];
    }
    return res;
}

static inline uint64_t read_le_bytes(const void* buf, size_t n)
{
    const uint8_t* p = (const uint8_t*)(buf);
    uint64_t res = 0;
    assert(n <= sizeof(res));
    for(size_t i = 0; i < n; ++i)
    {
        res = (res << 8) | p[n - i - 1];
    }
    return res;
}

static inline void write_be_bytes(void* buf, size_t n, uint64_t x)
{
    assert(n <= sizeof(x));
    uint8_t* p = (uint8_t*)(buf);
    for(size_t i = 0; i < n; ++i)
    {
        p[n - i - 1] = (uint8_t)(x);
        x >>= 8;
    }
}

static inline void write_le_bytes(void* buf, size_t n, uint64_t x)
{
    assert(n <= sizeof(x));
    uint8_t* p = (uint8_t*)(buf);
    for(size_t i = 0; i < n; ++i)
    {
        p[i] = (uint8_t)(x);
        x >>= 8;
    }
}


#endif /* INC_ENDIANNESS_H_ */
