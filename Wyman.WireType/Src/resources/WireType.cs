namespace WireType
{
    internal interface IWireType
    {
        bool ReadFrom(WireTypeReader reader);

        void WriteTo(WireTypeWriter writer);
    }

    internal struct StructMember<T>
    {
        public StructMember(int oridinal)
        {
            _exists = false;
            _ordinal = oridinal;
            _value = default(T);
        }

        private bool _exists;
        private int _ordinal;
        private T _value;

        public bool Exists
        {
            get { return _exists; }
            set
            {
                if (value && !_exists)
                    throw new System.InvalidOperationException($"Cannot set {nameof(WireType)} struct member as true without first setting a value.");

                this = new StructMember<T>()
                {
                    _exists = false,
                    _ordinal = _ordinal,
                    _value = default(T),
                };
            }
        }

        public int Ordinal
        {
            get { return _ordinal; }
        }

        public T Value
        {
            get { return _value; }
            set
            {
                this = new StructMember<T>()
                {
                    _exists = true,
                    _ordinal = _ordinal,
                    _value = value,
                };
            }
        }
    }

    [System.Flags]
    internal enum TypeKind : byte
    {
        ImplicitSize = 0,
        ExplicitSize = 1 << 0,
        List = 1 << 1,

        ImplicitSizeList = ImplicitSize | List,
        ExplicitSizeList = ExplicitSize | List,
    }

    internal unsafe class TypeHeader : System.IEquatable<TypeHeader>
    {
        public static readonly TypeHeaderComparer Comparer = new TypeHeaderComparer();

        const int Mask00000111 = 0x00000007;
        const int Mask00011111 = 0x0000001F;
        const int Mask00100000 = 0x00000020;
        const int Mask10000000 = 0x00000080;
        const int Mask11000000 = 0x000000C0;
        const int Mask11100000 = 0x000000E0;
        const int Shift3 = 3;
        const int Shift5 = 5;
        const int Shift6 = 6;

        private int _kind;
        private int _ordinal;
        private int _size;

        public TypeKind Kind
        {
            get { return (TypeKind)_kind; }
            internal set { _kind = (int)value; }
        }

        public int Ordinal
        {
            get { return _ordinal; }
            internal set { _ordinal = value; }
        }

        public int Size
        {
            get { return _size; }
            internal set { _size = value; }
        }

        /* Headers are variable size, but always at least 8-bits.
         * 
         * Meaning of the first 8-bits of the header:
         *  0-2: are the kind of message the header represents.
         *    3: extended header.
         *   4+: message ordinal, and length varints.
         */

        public int Deserialize(byte* data, int length)
        {
            ulong value = *(ulong*)data;
            byte* buffer = (byte*)&value;

            int k = 0;
            int l = 0;
            int o = 0;
            int read = 0;

            k = ((data[0] & Mask11000000) >> Shift6);

            // When the bit(3) is set, the value of the ordinal exceeds a single byte
            // in which case all of the bits must be shifted.
            if ((buffer[0] & Mask00100000) != 0)
            {
                // Shift-left all bits in the header by 3
                for (int i = 0; i < length && i < sizeof(ulong); i++)
                {
                    // When the current byte is zero and the previous bytes does not force
                    // include this byte by setting its highest order bit, we're done reading.
                    if (buffer[i] == 0 && i > 0 && (buffer[i - 1] & Mask10000000) == 0)
                        break;

                    buffer[i] = (byte)((buffer[i] << Shift3) | (byte)((buffer[i + 1] & Mask11100000) >> Shift5));
                }

                // Record that an extra byte was read.
                read += 1;
            }
            else
            {
                buffer[0] &= Mask00011111;
            }

            // Read the oridinal.
            int read1 = Varint.Read(buffer, &o);
            int read2 = 0;

            // If the type is size specified, read the size.
            if (k != (int)TypeKind.ImplicitSize)
            {
                read2 = Varint.Read(buffer + read1, &l);
            }

            _kind = k;
            _size = l;
            _ordinal = o;

            read += read1 + read2;

            return read;
        }

        public bool Equals(TypeHeader other)
            => Comparer.Equals(this, other);

        public override bool Equals(object obj)
        {
            return (obj is TypeHeader other && Equals(other))
                || base.Equals(obj);
        }

        public override int GetHashCode()
            => Comparer.GetHashCode(this);

        public int Serialize(byte* data, int length)
        {
            ulong value = 0;
            byte* buffer = (byte*)&value;

            int k = _kind;
            int l = _size;
            int o = _ordinal;
            int wrote = 0;

            // Write the oridinal to the buffer.
            int write1 = Varint.Write(&o, buffer);
            int write2 = 0;

            // If the type-kind is length specified, write the length value.
            if (k != (int)TypeKind.ImplicitSize)
            {
                write2 = Varint.Write(&l, buffer + write1);
            }

            wrote = write1 + write2;

            // If the orderinal varint is larger than 4 bits, we need to start shifting.
            if ((buffer[0] & Mask11100000) != 0)
            {
                // Shift-right all bits written so far by 3.
                for (int i = wrote; i > 0; i -= 1)
                {
                    buffer[i] = (byte)((buffer[i] >> Shift3) | ((buffer[i - 1] & Mask00000111) << Shift5));
                }

                buffer[0] >>= Shift3;

                // Indicate that an extended header is present.
                buffer[0] |= Mask00100000;

                // Note an extra byte was written.
                wrote += 1;
            }

            // Write the type-kind to the top 3-bits.
            buffer[0] |= (byte)((((byte*)&k)[0] << Shift6) & Mask11000000);

            // Copy the final value to the buffer.
            System.Buffer.MemoryCopy(buffer, data, length, wrote);

            return wrote;
        }

        public override string ToString()
        {
            return System.FormattableString.Invariant($"{nameof(Kind)} = {Kind}, {nameof(Ordinal)} = {Ordinal}, {nameof(Size)} = {Size}");
        }

        public static bool operator ==(TypeHeader lhs, TypeHeader rhs)
            => Comparer.Equals(lhs, rhs);

        public static bool operator !=(TypeHeader lhs, TypeHeader rhs)
            => !Comparer.Equals(lhs, rhs);
    }

    internal unsafe class TypeHeaderComparer : System.Collections.Generic.IEqualityComparer<TypeHeader>
    {
        public bool Equals(TypeHeader lhs, TypeHeader rhs)
        {
            if (lhs is null)
                return rhs is null;
            if (rhs is null)
                return false;

            return lhs.Ordinal == rhs.Ordinal
                && lhs.Size == rhs.Size
                && lhs.Kind == rhs.Kind;
        }

        public int GetHashCode(TypeHeader obj)
        {
            if (obj is null)
                return 0;

            return ((int)obj.Kind << 30)
                 | ((obj.Ordinal & 0x7FFF) << 15)
                 | ((obj.Size & 0x7FFF) << 0);
        }
    }

    internal static class Varint
    {
        /// <summary>
        /// Maximum size, in bytes, of a variable-length serialized 4-byte type.
        /// </summary>
        public const int Maximum4ByteSize = 5;
        /// <summary>
        /// Maximum size, in bytes, of a variable-length serialized 8-byte type.
        /// </summary>
        public const int Maximum8ByteSize = 10;

        const int i32LowBitMask = 0x00000001;
        const int i32HighBitMask = unchecked((int)0x80000000);
        const long i64LowBitMask = 0x0000000000000001;
        const long i64HighBitMask = unchecked((long)0x8000000000000000);

        public static unsafe int Read(byte* buffer, int* value)
        {
            unchecked
            {
                int count = Read(buffer, out uint n);

                *value = (int)((n >> 1) ^ (n << 31));

                return count;
            }
        }

        public static unsafe int Read(byte* buffer, out int value)
        {
            int v;
            int r = Read(buffer, &v);

            value = v;
            return r;
        }

        public static unsafe int Read(byte* buffer, long* value)
        {
            unchecked
            {
                int count = Read(buffer, out ulong n);

                *value = (long)((n >> 1) ^ (n << 63));

                return count;
            }
        }

        public static unsafe int Read(byte* buffer, out long value)
        {
            long v;
            int r = Read(buffer, &v);

            value = v;
            return r;
        }

        public static unsafe int Read(byte* buffer, uint* value)
        {
            unchecked
            {
                int count = 0;
                uint dst = 0;

                while (count < Maximum4ByteSize)
                {
                    dst |= (uint)((buffer[count] & 0x7F) << (7 * count));

                    if ((buffer[count] & 0x80) == 0)
                        break;

                    count += 1;
                }

                *value = dst;

                return count + 1;
            }
        }

        public static unsafe int Read(byte* buffer, out uint value)
        {
            uint v;
            int r = Read(buffer, &v);

            value = v;
            return r;
        }

        public static unsafe int Read(byte* buffer, ulong* value)
        {
            unchecked
            {
                int count = 0;
                ulong dst = 0;

                while (count < Maximum8ByteSize)
                {
                    dst |= ((ulong)(buffer[count] & 0x7F) << (7 * count));

                    if ((buffer[count] & 0x80) == 0)
                        break;

                    count += 1;
                }

                *value = dst;

                return count + 1;
            }
        }

        public static unsafe int Read(byte* buffer, out ulong value)
        {
            ulong v;
            int r = Read(buffer, &v);

            value = v;
            return r;
        }

        public static unsafe int Read(byte* buffer, float* value)
        {
            return Read(buffer, (uint*)value);
        }

        public static unsafe int Read(byte* buffer, out float value)
        {
            uint v;
            int r = Read(buffer, &v);

            value = *(float*)&v;
            return r;
        }

        public static unsafe int Read(byte* buffer, double* value)
        {
            return Read(buffer, (ulong*)value);
        }

        public static unsafe int Read(byte* buffer, out double value)
        {
            ulong v;
            int r = Read(buffer, &v);

            value = *(double*)&v;
            return r;
        }

        public static unsafe int Write(int* value, byte* buffer)
        {
            unchecked
            {
                uint n = (uint)*value;

                n = (n << 1) ^ (n >> 31);

                return Write(&n, buffer);
            }
        }

        public static unsafe int Write(int value, byte* buffer)
        {
            return Write(&value, buffer);
        }

        public static unsafe int Write(long* value, byte* buffer)
        {
            unchecked
            {
                ulong n = (ulong)*value;

                n = (n << 1) ^ (n >> 63);

                return Write(&n, buffer);
            }
        }

        public static unsafe int Write(long value, byte* buffer)
        {
            return Write(&value, buffer);
        }

        public static unsafe int Write(uint* value, byte* buffer)
        {
            unchecked
            {
                int count = 0;
                uint src = *value;

                while (count < Maximum4ByteSize)
                {
                    buffer[count] = (byte)(src & 0x7F);
                    src >>= 7;

                    if (src == 0)
                        break;

                    buffer[count] |= 0x80;
                    count += 1;
                }

                return count + 1;
            }
        }

        public static unsafe int Write(uint value, byte* buffer)
        {
            return Write(&value, buffer);
        }

        public static unsafe int Write(ulong* value, byte* buffer)
        {
            unchecked
            {
                int count = 0;
                ulong src = *value;

                while (count < Maximum8ByteSize)
                {
                    buffer[count] = (byte)(src & 0x7F);
                    src >>= 7;

                    if (src == 0)
                        break;

                    buffer[count] |= 0x80;
                    count += 1;
                }

                return count + 1;
            }
        }

        public static unsafe int Write(ulong value, byte* buffer)
        {
            return Write(&value, buffer);
        }

        public static unsafe int Write(float* value, byte* buffer)
        {
            return Write((uint*)value, buffer);
        }

        public static unsafe int Write(float value, byte* buffer)
        {
            uint v = *(uint*)&value;

            return Write(&v, buffer);
        }

        public static unsafe int Write(double* value, byte* buffer)
        {
            return Write((ulong*)value, buffer);
        }

        public static unsafe int Write(double value, byte* buffer)
        {
            ulong v = *(ulong*)&value;

            return Write(&v, buffer);
        }
    }

    internal unsafe class WireTypeReader : System.IDisposable
    {
        const int BufferedCountMinimum = 64;

        public WireTypeReader(System.IO.Stream readableStream)
        {
            if (readableStream is null)
                throw new System.ArgumentNullException(nameof(readableStream));
            if (!readableStream.CanRead)
            {
                var inner = new System.InvalidOperationException("The stream must be readable.");
                throw new System.ArgumentException(inner.Message, nameof(readableStream), inner);
            }

            _buffer = new byte[4096];
            _header = null;
            _get = 0;
            _set = 0;
            _stream = readableStream;
            _syncpoint = new object();

            _gchandle = System.Runtime.InteropServices.GCHandle.Alloc(_buffer, System.Runtime.InteropServices.GCHandleType.Pinned);
        }

        ~WireTypeReader()
        {
            Dispose(true);
        }

        private readonly byte[] _buffer;
        private System.Runtime.InteropServices.GCHandle _gchandle;
        private TypeHeader _header;
        private int _get;
        private int _set;
        private System.IO.Stream _stream;
        private readonly object _syncpoint;

        public TypeHeader Current
        {
            get
            {
                using (AcquireSyncpoint())
                {
                    if (_stream is null)
                        return null;

                    if (_header is null)
                    {
                        ReadHeader();
                    }

                    return _header;
                }
            }
        }

        public bool IsDisposed
        {
            get { lock (_syncpoint) return _stream is null; }
        }

        public long TotalRead
        {
            get
            {
                lock (_syncpoint)
                {
                    if (_stream is null)
                        return 0;

                    return _stream.Position - BufferedCount;
                }
            }
        }

        private int BufferedCount
        {
            get
            {
                System.Diagnostics.Debug.Assert(System.Threading.Monitor.IsEntered(_syncpoint), "Expected lock not held.");

                return (_get <= _set)
                    ? _set - _get
                    : _buffer.Length - _get + _set;
            }
        }

        private byte* Get
        {
            get
            {
                System.Diagnostics.Debug.Assert(System.Threading.Monitor.IsEntered(_syncpoint), "Expected lock not held.");

                return ((byte*)_gchandle.AddrOfPinnedObject()) + _get;
            }
        }

        private byte* Set
        {
            get
            {
                System.Diagnostics.Debug.Assert(System.Threading.Monitor.IsEntered(_syncpoint), "Expected lock not held.");

                return ((byte*)_gchandle.AddrOfPinnedObject()) + _set;
            }
        }

        public void Dispose()
        {
            System.GC.SuppressFinalize(this);

            Dispose(false);
        }

        public bool MoveNext()
        {
            lock (_syncpoint)
            {
                if (_stream is null || _header is null)
                    return false;

                if (BufferedCount < BufferedCountMinimum)
                {
                    PageBuffer(true);
                }

                ReadHeader();

                return _header != null;
            }
        }

        public bool Read(out byte[ ] value)
        {
            using (AcquireSyncpoint())
            {
                if (_stream != null && _header.Kind == TypeKind.ExplicitSize)
                {
                    var buffer = new byte[_header.Size];

                    int read = Read(buffer, buffer.Length);

                    MoveNext();

                    if (read == buffer.Length)
                    {
                        value = buffer;
                        return true;
                    }
                }
            }

            value = null;
            return false;
        }

        public bool Read(out byte[ ][ ] values)
        {
            using (AcquireSyncpoint())
            {
                if (_stream != null && _header.Kind == TypeKind.ExplicitSizeList)
                {
                    int read = 0;
                    int size = _header.Size;
                    var list = new System.Collections.Generic.List<byte[]>();

                    // Advance to the first header in the list.
                    MoveNext();

                    while (read < size)
                    {
                        var value = new byte[_header.Size];

                        read += Read(value, value.Length);

                        list.Add(value);

                        // Advance to the next object in the list, however if moving
                        // ahead is impossible, then something is wrong and it's time
                        // to quite before something goes REALLY wrong.
                        if (!MoveNext())
                        {
                            values = null;
                            return false;
                        }
                    }

                    values = list.ToArray();
                    return true;
                }
            }

            values = null;
            return false;
        }

        public bool Read(out TypeHeader value)
        {
            value = Current;

            return value != null;
        }

        public bool Read<T>(out T value) where T : IWireType, new()
        {
            using (AcquireSyncpoint())
            {
                if (_stream != null && _header.Kind == TypeKind.ExplicitSize)
                {
                    var v = new T();

                    if (v.ReadFrom(this))
                    {
                        value = v;

                        MoveNext();

                        return true;
                    }
                }

                MoveNext();
            }

            value = default(T);
            return false;
        }

        public bool Read<T>(out T[ ] values) where T : IWireType, new()
        {
            using (AcquireSyncpoint())
            {
                if (_stream != null && _header.Kind == TypeKind.ExplicitSizeList)
                {
                    long init = _stream.Position;
                    long size = _header.Size;
                    var list = new System.Collections.Generic.List<T>();

                    // Advance to the first header in the list.
                    MoveNext();

                    // Until the stream has advanced as far as expected, keep reading.
                    while (_stream.Position - init < size)
                    {
                        // Create a new message object.
                        var value = new T();

                        // Read the object's data.
                        value.ReadFrom(this);

                        // Add the object to the list.
                        list.Add(value);

                        // Advance to the next object in the list, however if moving
                        // ahead is impossible, then something is wrong and it's time
                        // to quite before something goes REALLY wrong.
                        if (!MoveNext())
                        {
                            values = null;
                            return false;
                        }
                    }

                    values = list.ToArray();
                    return true;
                }
            }

            values = null;
            return false;
        }

        public bool Read(out int value)
        {
            using (AcquireSyncpoint())
            {
                if (_stream != null && _header.Kind == TypeKind.ImplicitSize)
                {
                    _get += Varint.Read(Get, out value);

                    MoveNext();

                    return true;
                }
            }

            value = default(int);
            return false;
        }

        public bool Read(out int[ ] values)
        {
            using (AcquireSyncpoint())
            {
                if (_stream != null && _header.Kind == TypeKind.ImplicitSizeList)
                {
                    int read = 0;
                    int size = _header.Size;
                    var list = new System.Collections.Generic.List<int>();

                    while (read < size)
                    {
                        EnsureBuffer(Varint.Maximum4ByteSize);

                        int r = Varint.Read(Get, out int value);

                        _get += r;
                        read += r;

                        list.Add(value);
                    }

                    MoveNext();

                    values = list.ToArray();
                    return true;
                }
            }

            values = null;
            return false;
        }

        public bool Read(out long value)
        {
            using (AcquireSyncpoint())
            {
                if (_stream != null && _header.Kind == TypeKind.ImplicitSize)
                {
                    _get += Varint.Read(Get, out value);

                    MoveNext();

                    return true;
                }
            }

            value = default(int);
            return false;
        }

        public bool Read(out long[ ] values)
        {
            using (AcquireSyncpoint())
            {
                if (_stream != null && _header.Kind == TypeKind.ImplicitSizeList)
                {
                    int read = 0;
                    int size = _header.Size;
                    var list = new System.Collections.Generic.List<long>();

                    while (read < size)
                    {
                        EnsureBuffer(Varint.Maximum8ByteSize);

                        int r = Varint.Read(Get, out long value);

                        _get += r;
                        read += r;

                        list.Add(value);
                    }

                    MoveNext();

                    values = list.ToArray();
                    return true;
                }
            }

            values = null;
            return false;
        }

        public bool Read(out uint value)
        {
            using (AcquireSyncpoint())
            {
                if (_stream != null && _header.Kind == TypeKind.ImplicitSize)
                {
                    _get += Varint.Read(Get, out value);

                    MoveNext();

                    return true;
                }
            }

            value = default(int);
            return false;
        }

        public bool Read(out uint[ ] values)
        {
            using (AcquireSyncpoint())
            {
                if (_stream != null && _header.Kind == TypeKind.ImplicitSizeList)
                {
                    int read = 0;
                    int size = _header.Size;
                    var list = new System.Collections.Generic.List<uint>();

                    while (read < size)
                    {
                        EnsureBuffer(Varint.Maximum4ByteSize);

                        int r = Varint.Read(Get, out uint value);

                        _get += r;
                        read += r;

                        list.Add(value);
                    }

                    MoveNext();

                    values = list.ToArray();
                    return true;
                }
            }

            values = null;
            return false;
        }

        public bool Read(out ulong value)
        {
            using (AcquireSyncpoint())
            {
                if (_stream != null && _header.Kind == TypeKind.ImplicitSize)
                {
                    _get += Varint.Read(Get, out value);

                    MoveNext();

                    return true;
                }
            }

            value = default(int);
            return false;
        }

        public bool Read(out ulong[ ] values)
        {
            using (AcquireSyncpoint())
            {
                if (_stream != null && _header.Kind == TypeKind.ImplicitSizeList)
                {
                    int read = 0;
                    int size = _header.Size;
                    var list = new System.Collections.Generic.List<ulong>();

                    while (read < size)
                    {
                        EnsureBuffer(Varint.Maximum8ByteSize);

                        int r = Varint.Read(Get, out ulong value);

                        _get += r;
                        read += r;

                        list.Add(value);
                    }

                    MoveNext();

                    values = list.ToArray();
                    return true;
                }
            }

            values = null;
            return false;
        }

        public bool Read(out float value)
        {
            using (AcquireSyncpoint())
            {
                if (_stream != null && _header.Kind == TypeKind.ImplicitSize)
                {
                    _get += Varint.Read(Get, out value);

                    MoveNext();

                    return true;
                }
            }

            value = default(float);
            return false;
        }

        public bool Read(out float[ ] values)
        {
            using (AcquireSyncpoint())
            {
                if (_stream != null && _header.Kind == TypeKind.ImplicitSizeList)
                {
                    int read = 0;
                    int size = _header.Size;
                    var list = new System.Collections.Generic.List<float>();

                    while (read < size)
                    {
                        EnsureBuffer(Varint.Maximum4ByteSize);

                        int r = Varint.Read(Get, out float value);

                        _get += r;
                        read += r;

                        list.Add(value);
                    }

                    MoveNext();

                    values = list.ToArray();
                    return true;
                }
            }

            values = null;
            return false;
        }

        public bool Read(out double value)
        {
            using (AcquireSyncpoint())
            {
                if (_stream != null && _header.Kind == TypeKind.ImplicitSize)
                {
                    _get += Varint.Read(Get, out value);

                    MoveNext();

                    return true;
                }
            }

            value = default(double);
            return false;
        }

        public bool Read(out double[ ] values)
        {
            using (AcquireSyncpoint())
            {
                if (_stream != null && _header.Kind == TypeKind.ImplicitSizeList)
                {
                    int read = 0;
                    int size = _header.Size;
                    var list = new System.Collections.Generic.List<double>();

                    while (read < size)
                    {
                        EnsureBuffer(Varint.Maximum8ByteSize);

                        int r = Varint.Read(Get, out double value);

                        _get += r;
                        read += r;

                        list.Add(value);
                    }

                    MoveNext();

                    values = list.ToArray();
                    return true;
                }
            }

            values = null;
            return false;
        }

        public bool Skip()
        {
            using (AcquireSyncpoint())
            {
                if (_stream is null || _header is null)
                    return false;

                // Simple message are easy to skip...
                switch (_header.Kind)
                {
                    case TypeKind.ImplicitSize:
                        return Read(out long _);
                }

                /* The message must be length specified. */

                // If the local buffer already has the entire messsage,
                // just skip past it and we're done.
                if (_header.Size <= BufferedCount)
                {
                    _get += _header.Size;
                }
                else
                {
                    int read = 0;
                    byte[] buffer = new byte[4096];

                    // We need to read beyond the end of the local buffer, so
                    // keep reading and paging until the length of the message
                    // has been read/skipped over.
                    while (read < _header.Size)
                    {

                        // Read the data from the stream as much as necissary, up to the size of the buffer.
                        int count = System.Math.Min(_header.Size - read, buffer.Length);

                        read += Read(buffer, count);
                    }
                }

                MoveNext();

                return BufferedCount > 0;
            }
        }

        internal System.IDisposable AcquireSyncpoint()
        {
            lock (_syncpoint)
            {
                if (_header is null)
                {
                    ReadHeader();
                }

                return new MonitorLock(_syncpoint);
            }
        }

        internal int Read(byte[ ] buffer, int index, int count)
        {
            System.Diagnostics.Debug.Assert(System.Threading.Monitor.IsEntered(_syncpoint), "Expected lock not held.");

            if (buffer is null)
                throw new System.ArgumentNullException(nameof(buffer));
            if (index < 0 || index >= buffer.Length)
                throw new System.ArgumentOutOfRangeException(nameof(index));
            if (count < 0 || index + count > buffer.Length || index + count < 0)
                throw new System.ArgumentOutOfRangeException(nameof(count));

            int read = 0;

            while (read < count)
            {
                int avail = _get <= _set
                    ? _set - _get
                    : _buffer.Length - _get;

                // If there is sufficient data available to complete this transaction, do so.
                if (avail >= count - read)
                {
                    int copylen = count - read;

                    // Copy the bytes.
                    System.Buffer.BlockCopy(_buffer, _get, buffer, read, copylen);

                    // Adjust the get-pointer.
                    _get += copylen;

                    // Update read count.
                    read += copylen;

                    return read;
                }
                else if (avail == 0)
                {
                    // Since we're out of available data, paginate more in.
                    PageBuffer(true);

                    // If we've hit the end of the data stream, break the loop
                    // even if we've not completed the read request.
                    if (BufferedCount == 0)
                        break;
                }
                else
                {
                    int copylen = avail;

                    // Copy the bytes.
                    System.Buffer.BlockCopy(_buffer, _get, buffer, read, copylen);

                    // Adjust the get-pointer.
                    _get += copylen;

                    // Update read count.
                    read += copylen;
                }
            }

            return read;
        }

        internal int Read(byte[ ] buffer, int count)
            => Read(buffer, 0, count);

        private void CompactBuffer(bool force = false)
        {
            System.Diagnostics.Debug.Assert(System.Threading.Monitor.IsEntered(_syncpoint), "Expected lock not held.");

            // As long as the get-pointer is behind the set-pointer
            // there's nothing to compact.
            if (!force && _get < _set)
                return;

            // If the get-pointer has caught the set-pointer, then reset them
            // both back to the beginning of the buffer.
            if (_get == _set)
            {
                _get = _set = 0;
                return;
            }

            byte* buffer = (byte*)_gchandle.AddrOfPinnedObject();

            if (_get > _set)
            {
                // Determine how much data needs to be moved from the end to the head of the buffer.
                int avail = _buffer.Length - _get;
                int empty = _buffer.Length - _set - avail;

                byte* tmp = stackalloc byte[4096];

                // Copy the data from the end of the buffer into a temporary location.
                System.Buffer.MemoryCopy(Get, tmp, 4096, avail);

                // Move the data at the head of the buffer enough to make room for the data from the end to fit.
                System.Buffer.MemoryCopy(buffer, Set, _buffer.Length - _set, _set);

                // Copy the data from temporary buffer to the head of the data buffer.
                System.Buffer.MemoryCopy(tmp, buffer, avail, avail);

                // Update the get- and set-pointer
                _set += avail;
                _get = 0;
            }
            else
            {
                // Determine how much data needs to be moved from the end to the head of the buffer.
                int avail = _set - _get;
                int empty = _buffer.Length - avail;

                // Move the unread data back to the start of the buffer.
                System.Buffer.MemoryCopy(Get, buffer, _buffer.Length, avail);

                // Update the get- and set-pointer
                _set = avail;
                _get = 0;
            }
        }

        private void Dispose(bool finalizing)
        {
            if (finalizing)
            {
                System.IO.Stream stream;

                if ((stream = System.Threading.Interlocked.Exchange(ref _stream, null)) != null)
                {
                    if (_gchandle.IsAllocated)
                    {
                        _gchandle.Free();
                    }

                    stream.Flush();
                }
            }
            else
            {
                lock (_syncpoint)
                {
                    if (_gchandle.IsAllocated)
                    {
                        _gchandle.Free();
                    }

                    if (_stream != null)
                    {
                        _stream.Flush();

                        _stream = null;
                    }
                }
            }
        }

        private bool EnsureBuffer(int available)
        {
            System.Diagnostics.Debug.Assert(System.Threading.Monitor.IsEntered(_syncpoint), "Expected lock not held.");

            int avail = _get <= _set
                ? _set - _get
                : _buffer.Length - _get;

            if (avail < available)
            {
                PageBuffer(true);
            }

            return BufferedCount >= available;
        }

        private void PageBuffer(bool forceCompact = false)
        {
            System.Diagnostics.Debug.Assert(System.Threading.Monitor.IsEntered(_syncpoint), "Expected lock not held.");

            CompactBuffer(forceCompact);

            // When the get-pointer catches up to the set-pointer,
            // move them both back to the start of the buffer.
            if (_get == _set)
            {
                _get = 0;
                _set = 0;
            }

            int empty = _get <= _set
                ? _buffer.Length - _set
                : _get - _set;

            // If we have available space, fill it.
            if (empty > 0)
            {
                _set += _stream.Read(_buffer, _set, empty);
            }
        }

        private void ReadHeader()
        {
            System.Diagnostics.Debug.Assert(System.Threading.Monitor.IsEntered(_syncpoint), "Expected lock not held.");

            if (BufferedCount < 4)
            {
                PageBuffer();
            }
            else
            {
                CompactBuffer();
            }

            if (BufferedCount == 0)
            {
                _header = null;
            }
            else
            {
                if (_header is null)
                {
                    _header = new TypeHeader();
                }

                _get += _header.Deserialize(Get, BufferedCount);
            }
        }

        private struct MonitorLock : System.IDisposable
        {
            public MonitorLock(object syncpoint)
            {
                _syncpoint = syncpoint;

                if (_syncpoint != null)
                {
                    System.Threading.Monitor.Enter(syncpoint);
                }
            }

            private object _syncpoint;

            public void Dispose()
            {
                object syncpoint;
                if ((syncpoint = System.Threading.Interlocked.Exchange(ref _syncpoint, null)) != null)
                {
                    System.Threading.Monitor.Exit(syncpoint);
                }
            }
        }
    }

    internal unsafe class WireTypeWriter : System.IDisposable
    {
        public WireTypeWriter(System.IO.Stream writableStream)
        {
            if (writableStream is null)
                throw new System.ArgumentNullException(nameof(writableStream));
            if (!writableStream.CanWrite)
            {
                var inner = new System.InvalidOperationException("The stream must be writable.");
                throw new System.ArgumentException(inner.Message, nameof(writableStream), inner);
            }

            _buffer = new byte[4096];
            _stream = writableStream;
            _syncpoint = new object();

            _gchandle = System.Runtime.InteropServices.GCHandle.Alloc(_buffer, System.Runtime.InteropServices.GCHandleType.Pinned);
        }

        ~WireTypeWriter()
        {
            Dispose(true);
        }

        private readonly byte[] _buffer;
        private System.Runtime.InteropServices.GCHandle _gchandle;
        private System.IO.Stream _stream;
        private readonly object _syncpoint;

        public long TotalWritten
        {
            get
            {
                lock (_syncpoint)
                {
                    if (_stream is null)
                        return 0;

                    return _stream.Position;
                }
            }
        }

        private byte* Pointer
        {
            get
            {
                System.Diagnostics.Debug.Assert(System.Threading.Monitor.IsEntered(_syncpoint), "Expected lock not held.");

                return (byte*)_gchandle.AddrOfPinnedObject();
            }
        }

        public void Dispose()
        {
            System.GC.SuppressFinalize(this);

            Dispose(false);
        }

        public void Write(int ordinal, byte* value, int length)
        {
            if (ordinal <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(ordinal));
            if (value == null)
                throw new System.ArgumentNullException(nameof(value));
            if (length < 0)
                throw new System.ArgumentOutOfRangeException(nameof(length));

            lock (_syncpoint)
            {
                var header = new TypeHeader
                {
                    Kind = TypeKind.ExplicitSize,
                    Ordinal = ordinal,
                    Size = length,
                };

                int count = header.Serialize(Pointer, _buffer.Length);

                _stream.Write(_buffer, 0, count);

                int read = 0;
                while (read < length)
                {
                    int toCopy = length - read;
                    toCopy = System.Math.Min(_buffer.Length, toCopy);

                    System.Buffer.MemoryCopy(value, Pointer, _buffer.Length, toCopy);

                    _stream.Write(_buffer, 0, toCopy);

                    read += toCopy;
                }
            }
        }

        public void Write(int ordinal, byte[ ] value, int offset, int length)
        {
            if (ordinal <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(ordinal));
            if (value is null)
                throw new System.ArgumentNullException(nameof(value));
            if (offset < 0)
                throw new System.ArgumentOutOfRangeException(nameof(offset));
            if (length < 0 || offset + length > value.Length || offset + length < 0)
                throw new System.ArgumentOutOfRangeException(nameof(length));

            fixed (byte* p = value)
            {
                Write(ordinal, p + offset, length);
            }
        }

        public void Write(int ordinal, byte[ ] value)
            => Write(ordinal, value, 0, value?.Length ?? 0);

        public void Write(int ordinal, byte[ ][ ] values)
        {
            if (ordinal <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(ordinal));
            if (values is null)
                throw new System.ArgumentNullException(nameof(values));

            using (var memory = new System.IO.MemoryStream())
            using (var writer = new WireTypeWriter(memory))
            {
                for (int i = 0; i < values.Length; i += 1)
                {
                    if (values[i] == null)
                        continue;

                    writer.Write(i, values[i]);
                }

                int size = unchecked((int)memory.Position);
                byte[] buffer = memory.ToArray();
                var header = new TypeHeader
                {
                    Kind = TypeKind.ExplicitSizeList,
                    Ordinal = ordinal,
                    Size = size,
                };

                lock (_syncpoint)
                {
                    int count = header.Serialize(Pointer, _buffer.Length);

                    _stream.Write(_buffer, 0, count);
                    _stream.Write(buffer, 0, size);
                }
            }
        }

        public void Write<T>(int ordinal, T value) where T : IWireType, new()
        {
            if (ordinal < 0)
                throw new System.ArgumentOutOfRangeException(nameof(ordinal));
            if (value == null)
                throw new System.ArgumentNullException(nameof(value));

            lock (_syncpoint)
            {
                if (_stream is null)
                    return;

                using (var memory = new System.IO.MemoryStream())
                using (var writer = new WireTypeWriter(memory))
                {
                    value.WriteTo(writer);

                    if (memory.Length > int.MaxValue)
                        throw new System.InvalidOperationException("Serialized structure is too large.");

                    byte[] buffer = memory.ToArray();
                    var header = new TypeHeader
                    {
                        Kind = TypeKind.ExplicitSize,
                        Ordinal = ordinal,
                        Size = buffer.Length,
                    };

                    int count = header.Serialize(Pointer, _buffer.Length);

                    // Write the header to the output stream.
                    _stream.Write(_buffer, 0, count);

                    // Write serialized data to output stream.
                    _stream.Write(buffer, 0, buffer.Length);
                }

                _stream.Flush();
            }
        }

        public void Write<T>(int ordinal, T[ ] values) where T : IWireType, new()
        {
            if (ordinal <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(ordinal));
            if (values is null)
                throw new System.ArgumentNullException(nameof(values));

            using (var memory = new System.IO.MemoryStream())
            using (var writer = new WireTypeWriter(memory))
            {
                for (int i = 0; i < values.Length; i += 1)
                {
                    if (values[i] == null)
                        continue;

                    writer.Write(i + 1, values[i]);
                }

                int size = unchecked((int)memory.Position);
                byte[] buffer = memory.ToArray();
                var header = new TypeHeader
                {
                    Kind = TypeKind.ExplicitSizeList,
                    Ordinal = ordinal,
                    Size = size,
                };

                lock (_syncpoint)
                {
                    int count = header.Serialize(Pointer, _buffer.Length);

                    // Write the header to the output stream.
                    _stream.Write(_buffer, 0, count);

                    // Write the struct to the output stream.
                    _stream.Write(buffer, 0, size);
                }
            }
        }

        public void Write(int ordinal, int value)
        {
            if (ordinal <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(ordinal));

            lock (_syncpoint)
            {
                if (_stream is null)
                    return;

                var header = new TypeHeader
                {
                    Kind = TypeKind.ImplicitSize,
                    Ordinal = ordinal,
                };

                int count = header.Serialize(Pointer, _buffer.Length);

                // Write the header to the output stream.
                _stream.Write(_buffer, 0, count);

                count = Varint.Write(value, Pointer);

                // Write the serialized data to the output stream.
                _stream.Write(_buffer, 0, count);
            }
        }

        public void Write(int ordinal, int[ ] values)
        {
            if (ordinal <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(ordinal));
            if (values is null)
                throw new System.ArgumentNullException(nameof(values));

            var buffer = new byte[values.Length * Varint.Maximum8ByteSize];
            int size = 0;

            fixed (byte* memory = buffer)
            {
                for (int i = 0; i < values.Length; i += 1)
                {
                    size += Varint.Write(values[i], memory + size);
                }
            }

            var header = new TypeHeader
            {
                Kind = TypeKind.ImplicitSizeList,
                Ordinal = ordinal,
                Size = size,
            };

            lock (_syncpoint)
            {
                if (_stream is null)
                    return;

                int count = header.Serialize(Pointer, _buffer.Length);

                _stream.Write(_buffer, 0, count);

                _stream.Write(_buffer, 0, size);
            }
        }

        public void Write(int ordinal, long value)
        {
            if (ordinal <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(ordinal));

            lock (_syncpoint)
            {
                if (_stream is null)
                    return;

                var header = new TypeHeader
                {
                    Kind = TypeKind.ImplicitSize,
                    Ordinal = ordinal,
                };

                int count = header.Serialize(Pointer, _buffer.Length);

                _stream.Write(_buffer, 0, count);

                count = Varint.Write(value, Pointer);

                _stream.Write(_buffer, 0, count);
            }
        }

        public void Write(int ordinal, long[ ] values)
        {
            if (ordinal <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(ordinal));
            if (values is null)
                throw new System.ArgumentNullException(nameof(values));

            var buffer = new byte[values.Length * Varint.Maximum8ByteSize];
            int size = 0;

            fixed (byte* memory = buffer)
            {
                for (int i = 0; i < values.Length; i += 1)
                {
                    size += Varint.Write(values[i], memory + size);
                }
            }

            var header = new TypeHeader
            {
                Kind = TypeKind.ImplicitSizeList,
                Ordinal = ordinal,
                Size = size,
            };

            lock (_syncpoint)
            {
                if (_stream is null)
                    return;

                int count = header.Serialize(Pointer, _buffer.Length);

                _stream.Write(_buffer, 0, count);

                _stream.Write(_buffer, 0, size);
            }
        }

        public void Write(int ordinal, uint value)
        {
            if (ordinal <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(ordinal));

            lock (_syncpoint)
            {
                if (_stream is null)
                    return;

                var header = new TypeHeader
                {
                    Kind = TypeKind.ImplicitSize,
                    Ordinal = ordinal,
                };

                int count = header.Serialize(Pointer, _buffer.Length);

                _stream.Write(_buffer, 0, count);

                count = Varint.Write(value, Pointer);

                _stream.Write(_buffer, 0, count);
            }
        }

        public void Write(int ordinal, uint[ ] values)
        {
            if (ordinal <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(ordinal));
            if (values is null)
                throw new System.ArgumentNullException(nameof(values));

            var buffer = new byte[values.Length * Varint.Maximum8ByteSize];
            int size = 0;

            fixed (byte* memory = buffer)
            {
                for (int i = 0; i < values.Length; i += 1)
                {
                    size += Varint.Write(values[i], memory + size);
                }
            }

            var header = new TypeHeader
            {
                Kind = TypeKind.ImplicitSizeList,
                Ordinal = ordinal,
                Size = size,
            };

            lock (_syncpoint)
            {
                if (_stream is null)
                    return;

                int count = header.Serialize(Pointer, _buffer.Length);

                _stream.Write(_buffer, 0, count);

                _stream.Write(_buffer, 0, size);
            }
        }

        public void Write(int ordinal, ulong value)
        {
            if (ordinal <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(ordinal));

            lock (_syncpoint)
            {
                if (_stream is null)
                    return;

                var header = new TypeHeader
                {
                    Kind = TypeKind.ImplicitSize,
                    Ordinal = ordinal,
                };

                int count = header.Serialize(Pointer, _buffer.Length);

                _stream.Write(_buffer, 0, count);

                count = Varint.Write(value, Pointer);

                _stream.Write(_buffer, 0, count);
            }
        }

        public void Write(int ordinal, ulong[ ] values)
        {
            if (ordinal <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(ordinal));
            if (values is null)
                throw new System.ArgumentNullException(nameof(values));

            var buffer = new byte[values.Length * Varint.Maximum8ByteSize];
            int size = 0;

            fixed (byte* memory = buffer)
            {
                for (int i = 0; i < values.Length; i += 1)
                {
                    size += Varint.Write(values[i], memory + size);
                }
            }

            var header = new TypeHeader
            {
                Kind = TypeKind.ImplicitSizeList,
                Ordinal = ordinal,
                Size = size,
            };

            lock (_syncpoint)
            {
                if (_stream is null)
                    return;

                int count = header.Serialize(Pointer, _buffer.Length);

                _stream.Write(_buffer, 0, count);

                _stream.Write(_buffer, 0, size);
            }
        }

        public void Write(int ordinal, float value)
            => Write(ordinal, *(uint*)&value);

        public void Write(int ordinal, float[ ] values)
        {
            if (ordinal <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(ordinal));
            if (values is null)
                throw new System.ArgumentNullException(nameof(values));

            var buffer = new byte[values.Length * Varint.Maximum4ByteSize];
            int size = 0;

            fixed (byte* memory = buffer)
            {
                for (int i = 0; i < values.Length; i += 1)
                {
                    size += Varint.Write(values[i], memory + size);
                }
            }

            var header = new TypeHeader
            {
                Kind = TypeKind.ImplicitSizeList,
                Ordinal = ordinal,
                Size = size,
            };

            lock (_syncpoint)
            {
                if (_stream is null)
                    return;

                int count = header.Serialize(Pointer, _buffer.Length);

                // Write the header to the output stream.
                _stream.Write(_buffer, 0, count);

                // Write the serialized data to the output stream.
                _stream.Write(buffer, 0, size);
            }
        }

        public void Write(int ordinal, double value)
            => Write(ordinal, *(ulong*)&value);

        public void Write(int ordinal, double[ ] values)
        {
            if (ordinal <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(ordinal));
            if (values is null)
                throw new System.ArgumentNullException(nameof(values));

            var buffer = new byte[values.Length * Varint.Maximum8ByteSize];
            int size = 0;

            fixed (byte* memory = buffer)
            {
                for (int i = 0; i < values.Length; i += 1)
                {
                    size += Varint.Write(values[i], memory + size);
                }
            }

            var header = new TypeHeader
            {
                Kind = TypeKind.ImplicitSizeList,
                Ordinal = ordinal,
                Size = size,
            };

            lock (_syncpoint)
            {
                if (_stream is null)
                    return;

                int count = header.Serialize(Pointer, _buffer.Length);

                _stream.Write(_buffer, 0, count);

                _stream.Write(_buffer, 0, size);
            }
        }

        private void Dispose(bool finalizing)
        {
            if (finalizing)
            {
                System.IO.Stream stream;

                if ((stream = System.Threading.Interlocked.Exchange(ref _stream, null)) != null)
                {
                    if (_gchandle.IsAllocated)
                    {
                        _gchandle.Free();
                    }

                    stream.Flush();
                }
            }
            else
            {
                lock (_syncpoint)
                {
                    if (_gchandle.IsAllocated)
                    {
                        _gchandle.Free();
                    }

                    if (_stream != null)
                    {
                        _stream.Flush();

                        _stream = null;
                    }
                }
            }
        }
    }
}
