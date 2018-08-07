#include "wire_type.h"

int wiretype_vari32__read(char* varint, signed long* value_out)
{
	return wiretype_varu32__read(varint, (unsigned long*)value_out);
}

int wiretype_vari32__write(signed long* value, char* varint_out)
{
	return wiretype_varu32__write((unsigned long*)value, varint_out);
}

int wiretype_vari64__read(char* varint, signed long long* value_out)
{
	return wiretype_varu64__read(varint, (unsigned long long*)value_out);
}

int wiretype_vari64__write(signed long long* value, char* varint_out)
{
	return wiretype_varu64__write((unsigned long long*)value, varint_out);
}

int wiretype_varu32__read(char* varint, unsigned long* value_out)
{
	int count = 0;
	unsigned long dst = 0;

	while (count < 5)
	{
		dst |= (unsigned long)((varint[count] & 0x7F) << (7 * count));

		if ((varint[count] & 0x80) == 0)
			break;

		count += 1;
	}

	*value_out = dst;

	return count + 1;
}

int wiretype_varu32__write(unsigned long* value, char* varint_out)
{
	int count = 0;
	unsigned long src = *value;

	while (count < 5)
	{
		varint_out[count] = (char)(src & 0x7F);
		src >>= 7;

		if (src == 0)
			break;

		varint_out[count] |= 0x80;
		count += 1;
	}

	return count + 1;
}

int wiretype_varu64__read(char* varint, unsigned long long* value_out)
{
	int count = 0;
	unsigned long long dst = 0;

	while (count < 10)
	{
		dst |= ((unsigned long long)(varint[count] & 0x7F) << (7 * count));

		if ((varint[count] & 0x80) == 0)
			break;

		count += 1;
	}

	*value_out = dst;

	return count + 1;
}

int wiretype_varu64__write(unsigned long long* value, char* varint_out)
{
	int count = 0;
	unsigned long long src = *value;

	while (count < 10)
	{
		varint_out[count] = (char)(src & 0x7F);
		src >>= 7;

		if (src == 0)
			break;

		varint_out[count] |= 0x80;
		count += 1;
	}

	return count + 1;
}

typedef struct wiretype_typedetail
{
	unsigned long _value;
};

int wiretype_typedetail__kind_get(wiretype_typedetail *data, wiretype_kind *kind_out)
{
	if (!data || !kind_out)
		return -1;

	*kind_out = (wiretype_kind)((data->_value & 0x000000F0) >> 4);

	return 0;
}

int wiretype_typedetail__kind_set(wiretype_typedetail *data, wiretype_kind *kind_new)
{
	if (!data || !kind_new)
		return -1;

	data->_value &= 0xFFFFFF0F;
	data->_value |= ((*kind_new << 4) & 0x000000F0);

	return 0;
}

int wiretype_typedetail__ordinal_get(wiretype_typedetail *data, unsigned long *oridinal_out)
{
	if (!data || !oridinal_out)
		return -1;

	*oridinal_out = (unsigned long)((data->_value & 0xFFFFFF00) >> 8);

	return 0;
}

int wiretype_typedetail__ordinal_get(wiretype_typedetail *data, unsigned long *oridinal_new)
{
	if (!data || !oridinal_new)
		return -1;

	data->_value &= 0x000000FF;
	data->_value |= (unsigned long)((*oridinal_new << 8) & 0xFFFFFF00);

	return 0;
}

int wiretype_typedetail__initialize(void *data, wiretype_typedetail *detail_out)
{
	if (!data || !detail_out)
		return -1;

	detail_out->_value = *((unsigned long *)data);

	return sizeof(wiretype_typedetail);
}