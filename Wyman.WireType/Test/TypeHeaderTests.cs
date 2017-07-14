using System;
using WireType;
using Xunit;

namespace Wyman.WireType.Test
{
    public unsafe class TypeHeaderTests
    {
        [Fact]
        public void Serialziation()
        {
            var rand = new Random(20121230);

            for (int i = 0; i < 1000; i += 1)
            {
                int kind = rand.Next(0, 3);
                int ordinal = rand.Next(1, 32767);
                int length = 0;

                if (kind == 3)
                {
                    length = rand.Next(8, 2147483647);
                }

                var head1 = new TypeHeader
                {
                    Kind = (TypeKind)kind,
                    Size = length,
                    Ordinal = ordinal,
                };
                var head2 = new TypeHeader{ };

                Assert.Equal((TypeKind)kind, head1.Kind);
                Assert.Equal(length, head1.Size);
                Assert.Equal(ordinal, head1.Ordinal);

                Assert.NotEqual(head1, head2);

                Assert.Equal((TypeKind)0, head2.Kind);
                Assert.Equal(0, head2.Size);
                Assert.Equal(0, head2.Ordinal);

                Guid buf = Guid.Empty;

                int size = head1.Serialize((byte*)&buf, sizeof(Guid));
                int read = head2.Deserialize((byte*)&buf, sizeof(Guid));

                Assert.Equal(size, read);
                Assert.Equal(head1, head2, new TypeHeaderComparer());
            }
        }
    }
}
