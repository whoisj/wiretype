using System;
using Xunit;

namespace Wyman.WireType.Test
{
    public abstract class WireTypeTestsBase
    {
        protected const int BlobDeserializedSize = 92;
        protected const int BlobSerializedSize = 95;
        protected const int VariableValesSerializedSize = 32;

        // Randomly generated numbers.
        protected const int i32ExpectedValue = 1290835501;
        protected const long i64ExpectedValue = 9092287277766530722;
        protected const uint u32ExpectedValue = 1011573654;
        protected const ulong u64ExpectedValue = 5544096263427738395;

        protected byte[ ] BlobDeserialized
        {
            get
            {
                return new byte[ ]
                {
                    0X2D, 0X1B, 0XA2, 0X96, 0X77, 0XD4, 0X44, 0XF6, 0XB1, 0X38, 0X70, 0X70, 0X10, 0XDB, 0XA9, 0X9A,
                    0X22, 0X08, 0XA5, 0X75, 0XED, 0XF5, 0XA6, 0X9F, 0X51, 0XDE, 0X1B, 0XA4, 0XCD, 0XEF, 0X05, 0XAE,
                    0XF8, 0XB3, 0X40, 0XED, 0X52, 0XBD, 0XD5, 0XC1, 0XC7, 0X8B, 0XCE, 0XEB, 0XE5, 0X81, 0X8B, 0XF6,
                    0X5F, 0XBA, 0X93, 0X46, 0XE6, 0XCA, 0XC0, 0X37, 0X75, 0X02, 0X45, 0X99, 0XB8, 0X9F, 0X29, 0XC1,
                    0X33, 0XC1, 0X78, 0X5D, 0X9B, 0XBC, 0X48, 0X64, 0X33, 0XE3, 0XAD, 0X62, 0X26, 0XBB, 0XBA, 0XCF,
                    0X53, 0X25, 0X45, 0X13, 0X5B, 0XBE, 0XC8, 0X2D, 0XF2, 0X08, 0X78, 0X4F,
                };
            }
        }

        protected byte[ ] BlobSerialized
        {
            get
            {
                return new byte[ ]
                {
                    0x42, 0xB8, 0x01, 0x2D, 0x1B, 0xA2, 0x96, 0x77, 0xD4, 0x44, 0xF6, 0xB1, 0x38, 0x70, 0x70, 0x10,
                    0xDB, 0xA9, 0x9A, 0x22, 0x08, 0xA5, 0x75, 0xED, 0xF5, 0xA6, 0x9F, 0x51, 0xDE, 0x1B, 0xA4, 0xCD,
                    0xEF, 0x05, 0xAE, 0xF8, 0xB3, 0x40, 0xED, 0x52, 0xBD, 0xD5, 0xC1, 0xC7, 0x8B, 0xCE, 0xEB, 0xE5,
                    0x81, 0x8B, 0xF6, 0x5F, 0xBA, 0x93, 0x46, 0xE6, 0xCA, 0xC0, 0x37, 0x75, 0x02, 0x45, 0x99, 0xB8,
                    0x9F, 0x29, 0xC1, 0x33, 0xC1, 0x78, 0x5D, 0x9B, 0xBC, 0x48, 0x64, 0x33, 0xE3, 0xAD, 0x62, 0x26,
                    0xBB, 0xBA, 0xCF, 0x53, 0x25, 0x45, 0x13, 0x5B, 0xBE, 0xC8, 0x2D, 0xF2, 0x08, 0x78, 0x4F, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                };
            }
        }

        protected byte[ ] VariableValesSerialized
        {
            get
            {
                return new byte[ ]
                {
                    0x02, 0xDA, 0xD8, 0x84, 0xCF, 0x09, 0x04, 0xC4,
                    0xDA, 0x9E, 0xB1, 0xE5, 0xC6, 0xA5, 0xAE, 0xFC,
                    0x01, 0x06, 0x96, 0xC7, 0xAD, 0xE2, 0x03, 0x08,
                    0x9B, 0x96, 0xB9, 0xF1, 0xD7, 0xC5, 0xA5, 0xF8,
                    0x4C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                };
            }
        }

        protected void AssertValues(byte[ ] expected, byte[ ] actual, int count)
        {
            if (expected is null)
                throw new ArgumentNullException(nameof(expected));
            if (actual is null)
                throw new ArgumentNullException(nameof(actual));
            if (count < 0 || count > expected.Length || count > actual.Length)
                throw new ArgumentOutOfRangeException(nameof(count));

            for (int i = 0; i < count; i += 1)
            {
                Assert.Equal(expected[i], actual[i]);
            }

            for (int i = count; i < actual.Length; i += 1)
            {
                Assert.Equal(0, actual[i]);
            }
        }

        protected void AssertValues(byte[ ] expected, byte[ ] actual)
            => AssertValues(expected, actual, expected?.Length ?? 0);
    }
}
