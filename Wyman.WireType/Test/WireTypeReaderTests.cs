using System;
using System.IO;
using Xunit;

namespace Wyman.WireType.Test
{
    public unsafe class WireTypeReaderTests : WireTypeTestsBase
    {
        [Fact]
        public void ReadBlob()
        {
            var expectedBlob = BlobDeserialized;
            var buffer = new byte[BlobSerializedSize];

            Buffer.BlockCopy(BlobSerialized, 0, buffer, 0, BlobSerializedSize);

            using (var stream = new MemoryStream(buffer, false))
            using (var reader = new global::WireType.WireTypeReader(stream))
            {
                byte[ ] blob;

                // Assert that the reader can read the blob, with the correct size
                Assert.True(reader.Read(out blob));
                Assert.NotNull(blob);
                Assert.Equal(BlobSerializedSize, reader.TotalRead);
                Assert.Equal(BlobDeserializedSize, blob.Length);

                AssertValues(expectedBlob, blob);
            }
        }

        [Fact]
        public void ReadVariable()
        {
            int i32 = 0;
            long i64 = 0;
            uint u32 = 0;
            ulong u64 = 0;

            var dataSource = VariableValesSerialized;

            using (var stream = new MemoryStream(dataSource))
            using (var reader = new global::WireType.WireTypeReader(stream))
            {
                Assert.NotNull(reader.Current);
                Assert.Equal(global::WireType.TypeKind.ImplicitSize, reader.Current.Kind);

                Assert.True(reader.Read(out i32));
                Assert.Equal(i32ExpectedValue, i32);

                Assert.NotNull(reader.Current);
                Assert.Equal(global::WireType.TypeKind.ImplicitSize, reader.Current.Kind);

                Assert.True(reader.Read(out i64));
                Assert.Equal(i64ExpectedValue, i64);

                Assert.NotNull(reader.Current);
                Assert.Equal(global::WireType.TypeKind.ImplicitSize, reader.Current.Kind);

                Assert.True(reader.Read(out u32));
                Assert.Equal(u32ExpectedValue, u32);

                Assert.NotNull(reader.Current);
                Assert.Equal(global::WireType.TypeKind.ImplicitSize, reader.Current.Kind);

                Assert.True(reader.Read(out u64));
                Assert.Equal(u64ExpectedValue, u64);
            }
        }

        [Fact]
        public void Skip()
        {
            var buffer = new byte[BlobSerializedSize + VariableValesSerializedSize];

            // Concatenate the blob and fixed values serialized buffers.
            Buffer.BlockCopy(BlobSerialized, 0, buffer, 0, BlobSerializedSize);
            Buffer.BlockCopy(VariableValesSerialized, 0, buffer, BlobSerializedSize, VariableValesSerializedSize);

            using (var stream = new MemoryStream(buffer))
            using (var reader = new global::WireType.WireTypeReader(stream))
            {
                // Assert that the first message is the blob.
                Assert.Equal(global::WireType.TypeKind.ExplicitSize, reader.Current.Kind);

                // Assert that skipping the blob works, and skips the number of bytes
                // expected plus one for the next header read.
                Assert.True(reader.Skip());
                Assert.Equal(BlobSerializedSize + 1, reader.TotalRead);

                // Assert the first fixed value is the message after skipping.
                Assert.Equal(global::WireType.TypeKind.ImplicitSize, reader.Current.Kind);
                int i32;
                Assert.True(reader.Read(out i32));
                Assert.Equal(i32ExpectedValue, i32);
            }
        }
    }
}
