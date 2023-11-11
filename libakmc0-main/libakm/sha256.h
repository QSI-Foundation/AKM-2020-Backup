/*
 * Copyright 2020 OlympusSky Technologies S.A. All Rights Reserved.
 */


#ifndef INC_SHA256_H_
#define INC_SHA256_H_

#include <stddef.h>
#include <stdint.h>

#define SHA224_256_BLOCK_SIZE (512 / 8)
#define SHA256_DIGEST_SIZE (256 / 8)

struct SHA256_Context {
	size_t tot_len;
	size_t len;
	uint32_t h[8];
	uint8_t block[2 * SHA224_256_BLOCK_SIZE];
};

typedef struct SHA256_Context SHA256_Ctx;

void SHA256_init(SHA256_Ctx* c);
void SHA256_update(SHA256_Ctx* c, const void* message, size_t len);
void SHA256_finalize(SHA256_Ctx* c, uint8_t digest[SHA256_DIGEST_SIZE]);

void SHA256_calculate(SHA256_Ctx* c, const void* message, size_t len, uint8_t digest[SHA256_DIGEST_SIZE]);

static inline void SHA256_calc(const void* message, size_t len, uint8_t digest[SHA256_DIGEST_SIZE]) {
	SHA256_Ctx ctx;
	SHA256_calculate(&ctx, message, len, digest);
}

#endif /* INC_SHA256_H_ */
