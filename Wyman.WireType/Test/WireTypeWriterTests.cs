using System.IO;
using Xunit;

namespace Wyman.WireType.Test
{
    public class WireTypeWriterTests : WireTypeTestsBase
    {
        [Fact]
        public unsafe void DataCompaction()
        {
            const int iterations = 1024;
            const long i32Size = sizeof(int) * iterations;
            const long i64Size = sizeof(long) * iterations;
            const long u32Size = sizeof(uint) * iterations;
            const long u64Size = sizeof(ulong) * iterations;

            byte* buffer = stackalloc byte[128];
            int i32Written = 0;
            int i64written = 0;
            int u32Written = 0;
            int u64Written = 0;

            for (int i = 0; i < iterations; i += 1)
            {
                // Include negative values for signed types.
                int v = i - iterations / 2;

                i32Written += global::WireType.Varint.Write(v, buffer);
            }

            Assert.True(i32Written < i32Size, $"No compaction for i64 expected {i32Written} to be less than {i32Size}.");

            for (long i = 0; i < iterations; i += 1)
            {
                // Include negative values for signed types.
                long v = i - iterations / 2;

                i64written += global::WireType.Varint.Write(v, buffer);
            }

            Assert.True(i64written < i64Size, $"No compaction for i64 expected {i64written} to be less than {i64Size}.");

            for (uint i = 0; i < iterations; i += 1)
            {
                u32Written += global::WireType.Varint.Write(i, buffer);
            }

            Assert.True(u32Written < i64Size, $"No compaction for i64 expected {u32Written} to be less than {u32Size}.");

            for (ulong i = 0; i < iterations; i += 1)
            {
                u64Written += global::WireType.Varint.Write(i, buffer);
            }

            Assert.True(u64Written < u64Size, $"No compaction for i64 expected {u64Written} to be less than {u64Size}.");
        }

        [Fact]
        public unsafe void WriteBlob()
        {
            const int blobExpectedWrittenSize = 95;

            var expectedBlobContents = BlobSerialized;
            var buffer = new byte[512];

            using (var stream = new MemoryStream(buffer))
            using (var writer = new global::WireType.WireTypeWriter(stream))
            {
                writer.Write(1, BlobDeserialized);

                Assert.Equal(blobExpectedWrittenSize, writer.TotalWritten);
                AssertValues(BlobSerialized, buffer, blobExpectedWrittenSize);
            }
        }

        [Fact]
        public unsafe void WriteVariable()
        {
            const int i32ExpectedWriteSize = 1 + 5;
            const int i64ExpectedWriteSize = 1 + 10;
            const int u32ExpectedWriteSize = 1 + 5;
            const int u64ExpectedWriteSize = 1 + 9;

            var expectedValues = VariableValesSerialized;
            int expectedTotalWritten = 0;
            var buffer = new byte[expectedValues.Length];

            using (var stream = new MemoryStream(buffer))
            using (var writer = new global::WireType.WireTypeWriter(stream))
            {
                writer.Write(1, i32ExpectedValue);

                expectedTotalWritten += i32ExpectedWriteSize;

                Assert.Equal(i32ExpectedWriteSize, writer.TotalWritten);
                AssertValues(expectedValues, buffer, expectedTotalWritten);

                writer.Write(2, i64ExpectedValue);

                expectedTotalWritten += i64ExpectedWriteSize;

                Assert.Equal(expectedTotalWritten, writer.TotalWritten);
                AssertValues(expectedValues, buffer, expectedTotalWritten);

                writer.Write(3, u32ExpectedValue);

                expectedTotalWritten += u32ExpectedWriteSize;

                Assert.Equal(expectedTotalWritten, writer.TotalWritten);
                AssertValues(expectedValues, buffer, expectedTotalWritten);

                writer.Write(4, u64ExpectedValue);

                expectedTotalWritten += u64ExpectedWriteSize;

                Assert.Equal(expectedTotalWritten, writer.TotalWritten);
                AssertValues(expectedValues, buffer, expectedTotalWritten);
            }
        }
    }
}
