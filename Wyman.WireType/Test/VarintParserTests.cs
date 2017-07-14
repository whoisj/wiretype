using System;
using Xunit;

namespace Wyman.WireType.Test
{
    public class VarintParserTests
    {
        [Fact]
        public unsafe void i32Tests()
        {
            var rand = new Random(19770407);

            for (int i = 0; i < 10000; i += 1)
            {
                int number = rand.Next();

                if (i % 2 == 0)
                {
                    number *= -1;
                }

                byte* varint = stackalloc byte[sizeof(uint) + 1];
                int value = 0;

                int written = global::WireType.Varint.Write(&number, varint);
                int read = global::WireType.Varint.Read(varint, &value);

                Assert.Equal(written, read);
                Assert.Equal(number, value);
            }
        }

        [Fact]
        public unsafe void i64Tests()
        {
            var rand = new Random(19770407);

            for (int i = 0; i < 10000; i += 1)
            {
                long number = rand.Next(1 << (i % 31));

                if (i % 3 == 0)
                {
                    number <<= 32;
                    number += rand.Next();
                }

                if (i % 2 == 0)
                {
                    number *= -1;
                }

                byte* varint = stackalloc byte[sizeof(ulong) + 1];
                long value = 0;

                int written = global::WireType.Varint.Write(&number, varint);
                int read = global::WireType.Varint.Read(varint, &value);

                Assert.Equal(written, read);
                Assert.Equal(number, value);
            }
        }

        [Fact]
        public unsafe void u32Tests()
        {
            var rand = new Random(19770407);

            for (int i = 0; i < 10000; i += 1)
            {
                uint number = (uint)rand.Next(1 << (i % 31));

                byte* varint = stackalloc byte[sizeof(uint) + 1];
                uint value = 0;

                int written = global::WireType.Varint.Write(&number, varint);
                int read = global::WireType.Varint.Read(varint, &value);

                Assert.Equal(written, read);
                Assert.Equal(number, value);
            }
        }

        [Fact]
        public unsafe void u64Tests()
        {
            var rand = new Random(19770407);

            for (int i = 0; i < 10000; i += 1)
            {
                ulong number = (ulong)rand.Next(1 << (i % 31));

                if (i % 2 == 0)
                {
                    number <<= 32;
                    number += (ulong)rand.Next();
                }

                byte* varint = stackalloc byte[sizeof(ulong) + 1];
                ulong value = 0;

                int written = global::WireType.Varint.Write(&number, varint);
                int read = global::WireType.Varint.Read(varint, &value);

                Assert.Equal(written, read);
                Assert.Equal(number, value);
            }
        }

        [Fact]
        public unsafe void f32Tests()
        {
            var rand = new Random(19770407);

            for (int i = 0; i < 10000; i += 1)
            {
                float number = (float)rand.NextDouble();

                byte* varint = stackalloc byte[sizeof(float) + 1];
                float value = 0;

                int written = global::WireType.Varint.Write(&number, varint);
                int read = global::WireType.Varint.Read(varint, &value);

                Assert.Equal(written, read);
                Assert.Equal(number, value);
            }
        }

        [Fact]
        public unsafe void f64Tests()
        {
            var rand = new Random(19770407);

            for (int i = 0; i < 10000; i += 1)
            {
                double number = rand.NextDouble();

                if (i % 2 == 0)
                {
                    number *= -1;
                }

                byte* varint = stackalloc byte[sizeof(double) + 1];
                double value = 0;

                int written = global::WireType.Varint.Write(&number, varint);
                int read = global::WireType.Varint.Read(varint, &value);

                Assert.Equal(written, read);
                Assert.Equal(number, value);
            }
        }
    }
}
