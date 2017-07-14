using System.Text;

namespace Wyman.WireType
{
    [System.Diagnostics.DebuggerDisplay("{ToString(), nq}")]
    unsafe struct u64Bitview
    {
        private ulong _;

        public override string ToString()
        {
            var builder = new StringBuilder();

            ulong value = _;
            byte* ptr = (byte*)&value;

            for (int i = 0; i < sizeof(ulong); i += 1)
            {
                u8Bitview.Write(builder, ptr[i]);

                builder.Append(' ');
            }

            return builder.ToString();
        }

        public static explicit operator u64Bitview(ulong value)
        {
            return new u64Bitview { _ = value, };
        }

        public static explicit operator u64Bitview(long value)
        {
            unchecked
            {
                return new u64Bitview { _ = *(ulong*)&value, };
            }
        }
    }

    [System.Diagnostics.DebuggerDisplay("{ToString(), nq}")]
    unsafe struct u32Bitview
    {
        private uint _;

        public override string ToString()
        {
            var builder = new StringBuilder();

            uint value = _;
            byte* ptr = (byte*)&value;

            for (int i = 0; i < sizeof(uint); i += 1)
            {
                u8Bitview.Write(builder, ptr[i]);

                builder.Append(' ');
            }

            return builder.ToString();
        }

        public static explicit operator u32Bitview(uint value)
        {
            return new u32Bitview { _ = value, };
        }

        public static explicit operator u32Bitview(int value)
        {
            unchecked
            {
                return new u32Bitview { _ = *(uint*)&value, };
            }
        }
    }

    [System.Diagnostics.DebuggerDisplay("{ToString(), nq}")]
    unsafe struct u8Bitview
    {
        private byte _;

        public override string ToString()
        {
            var builder = new StringBuilder();

            Write(builder, _);

            return builder.ToString();
        }

        public static void Write(StringBuilder builder, byte value)
        {
            byte bit = 0x80;

            while (bit != 0)
            {
                if ((value & bit) != 0)
                {
                    builder.Append('1');
                }
                else
                {
                    builder.Append('0');
                }

                bit >>= 1;
            }
        }

        public static explicit operator u8Bitview(byte value)
        {
            return new u8Bitview { _ = value, };
        }

        public static explicit operator u8Bitview(sbyte value)
        {
            unchecked
            {
                return new u8Bitview { _ = *(byte*)&value, };
            }
        }
    }
}
