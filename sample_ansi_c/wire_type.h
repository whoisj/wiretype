#include <stdio.h>
#include <string.h>

#ifndef __WIRE_TYPE__
#define __WIRE_TYPE__

static int wiretype__success(int result)
{
	return result >= 0;
}

// Reads a 32-bit signed integer from a variable size integer buffer.
// Returns 0 if successfull; otherwise -1.
int wiretype_vari32__read(char* varint, signed long* value_out);

// Writes a variable sized 32-bit signed integer to a buffer.
// Returns 0 if successfull; otherwise -1.
int wiretype_vari32__write(signed long* value, char* varint_out);

// Reads a 64-bit signed integer from a variable size integer buffer.
// Returns 0 if successfull; otherwise -1.
int wiretype_vari64__read(char* varint, signed long long* value_out);

// Writes a variable sized 64-bit signed integer to a buffer.
// Returns 0 if successfull; otherwise -1.
int wiretype_vari64__write(signed long long* value, char* varint_out);

// Reads a 32-bit unsigned integer from a variable size integer buffer.
// Returns 0 if successfull; otherwise -1.
int wiretype_varu32__read(char* varint, unsigned long* value_out);

// Writes a variable sized 32-bit unsigned integer to a buffer.
// Returns 0 if successfull; otherwise -1.
int wiretype_varu32__write(unsigned long* value, char* varint_out);

// Reads a 64-bit unsigned integer from a variable size integer buffer.
// Returns 0 if successfull; otherwise -1.
int wiretype_varu64__read(char* varint, unsigned long long* value_out);

// Writes a variable sized 64-bit unsigned integer to a buffer.
// Returns 0 if successfull; otherwise -1.
int wiretype_varu64__write(unsigned long long* value, char* varint_out);

// Structure containing the details a wire type.
typedef struct wiretype_typedetail wiretype_typedetail;

// Defines the kinds of wide type values which can be written / read.
typedef enum wiretype_kind
{
	// Variable length integer value.
	wiretype_kind_varint = 0,
	// Fixed length, 32-bit value.
	wiretype_kind_fixed32 = 1,
	// Fixed length, 64-bit value.
	wiretype_kind_fixed64 = 2,
	// Arbitrary, specified length value.
	wiretype_kind_length = 3,
} wiretype_kind;

// Gets the kind of the specified wiretype_typedetail.
// Returns 0 if successfull; otherwise -1.
int wiretype_typedetail__kind_get(wiretype_typedetail *data, wiretype_kind *kind_out);

// Sets the kind of the specified wiretype_typedetail.
// Returns 0 if successfull; otherwise -1.
int wiretype_typedetail__kind_set(wiretype_typedetail *data, wiretype_kind *kind_new);

// Gets the ordinal of the specified wiretype_typedetail
// Returns 0 if successfull; otherwise -1.
int wiretype_typedetail__ordinal_get(wiretype_typedetail *data, unsigned long *oridinal_out);

// Gets the ordinal of the specified wiretype_typedetail.
// Returns 0 if successfull; otherwise -1.
int wiretype_typedetail__ordinal_set(wiretype_typedetail *data, unsigned long *oridinal_new);

// Initializes a wiretype_typedetail value from a data source.
// Returns the number of bytes read from the data source if successful; otherwise a negative value.
int wiretype_typedetail__initialize(void *data, wiretype_typedetail *detail_out);

#endif
