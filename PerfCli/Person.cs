namespace Sample.More
{
    partial class Person : WireType.IWireType
    {
        public Person()
        { }

        private WireType.StructMember<long> _DoB = new WireType.StructMember<long>(2);
        private WireType.StructMember<byte[][]> _Friends = new WireType.StructMember<byte[][]>(7);
        private WireType.StructMember<byte[][]> _Images = new WireType.StructMember<byte[][]>(567);
        private WireType.StructMember<long> _Len = new WireType.StructMember<long>(3);
        private WireType.StructMember<Sample.More.Person[]> _Mother = new WireType.StructMember<Sample.More.Person[]>(4);
        private WireType.StructMember<byte[]> _Name = new WireType.StructMember<byte[]>(1);
        private WireType.StructMember<byte[]> _Raw = new WireType.StructMember<byte[]>(8);

        public void DoB(System.DateTime value)
        {
            // NetFx System.DateTime has 100-nanosecond granularity, whereas millisecond values were recorded.
            _DoB.Value = value.Ticks / 10000L;
        }

        public bool DoB(out System.DateTime value)
        {
            if (!_DoB.Exists)
            {
                value = default(System.DateTime);
                return false;
            }

            // NetFx System.DateTime has 100-nanosecond granularity, whereas millisecond values were recorded.
            value = new System.DateTime(10000L * _DoB.Value);
            return true;
        }

        public void Friends(System.Collections.Generic.IReadOnlyList<string> value)
        {
            if (value is null)
            {
                _Friends.Value = null;
                return;
            }

            if (value.Count == 0)
            {
                _Friends.Value = System.Array.Empty<byte[]>();
            }
            else
            {
                var values = new byte[value.Count][];

                // NetFx System.String are UTF-16 encoded, whereas UTF-8 values were recorded.
                for (int i = 0; i < value.Count; i += 1)
                {
                    values[i] = (value[i] is null || value[i].Length == 0)
                        ? System.Array.Empty<byte>()
                        : System.Text.Encoding.UTF8.GetBytes(value[i]);
                }

                _Friends.Value = values;
            }
        }

        public bool Friends(out System.Collections.Generic.IReadOnlyList<string> value)
        {
            if (!_Friends.Exists)
            {
                value = default(System.Collections.Generic.IReadOnlyList<string>);
                return false;
            }

            var values = new System.Collections.Generic.List<string>(_Friends.Value.Length);

            // NetFx System.String are UTF-16 encoded, whereas UTF-8 values were recorded.
            foreach (byte[] bytes in _Friends.Value)
            {
                string strval = bytes.Length == 0
                    ? string.Empty
                    : System.Text.Encoding.UTF8.GetString(bytes);

                values.Add(strval);
            }

            value = values;
            return true;
        }

        public void Images(System.Collections.Generic.IReadOnlyList<byte[]> value)
        {
            if (value is null)
            {
                _Images.Value = null;
                return;
            }

            _Images.Value = (value.Count == 0)
                ? System.Array.Empty<byte[]>()
                : System.Linq.Enumerable.ToArray(value);
        }

        public bool Images(out System.Collections.Generic.IReadOnlyList<byte[]> value)
        {
            if (!_Images.Exists)
            {
                value = default(System.Collections.Generic.IReadOnlyList<byte[]>);
                return false;
            }

            value = _Images.Value;
            return true;
        }

        public void Len(System.TimeSpan value)
        {
            // NetFx System.TimeSpan has 100-nanosecond granularity, whereas nanosecond values were recorded.
            _Len.Value = value.Ticks * 100L;
        }

        public bool Len(out System.TimeSpan value)
        {
            if (!_Len.Exists)
            {
                value = default(System.TimeSpan);
                return false;
            }

            // NetFx System.TimeSpan has 100-nanosecond granularity, whereas nanosecond values were recorded.
            value = new System.TimeSpan(_Len.Value / 100L);
            return true;
        }

        public void Mother(System.Collections.Generic.IReadOnlyList<Sample.More.Person> value)
        {
            if (value is null || value.Count == 0)
            {
                _Mother.Value = System.Array.Empty<Sample.More.Person>();
                return;
            }

            // Public export is a read-only list, whereas an array was recorded.
            _Mother.Value = System.Linq.Enumerable.ToArray(value);
        }

        public bool Mother(out System.Collections.Generic.IReadOnlyList<Sample.More.Person> value)
        {
            if (!_Mother.Exists)
            {
                value = default(System.Collections.Generic.IReadOnlyList<Sample.More.Person>);
                return false;
            }

            value = _Mother.Value;
            return true;
        }

        public void Name(string value)
        {
            if (value is null)
            {
                _Name.Value = null;
                return;
            }

            // NetFx System.String are UTF-16 encoded, whereas UTF-8 values were recorded.
            _Name.Value = System.Text.Encoding.UTF8.GetBytes(value);
        }

        public bool Name(out string value)
        {
            if (!_Name.Exists)
            {
                value = default(string);
                return false;
            }

            // NetFx System.String are UTF-16 encoded, whereas UTF-8 values were recorded.
            value = System.Text.Encoding.UTF8.GetString(_Name.Value);
            return true;
        }

        public void Raw(byte[] value)
        {
            if (value is null)
            {
                _Raw.Value = null;
                return;
            }

            _Raw.Value = (value.Length == 0)
                ? System.Array.Empty<byte>()
                : value;
        }

        public bool Raw(out byte[] value)
        {
            if (!_Raw.Exists)
            {
                value = default(byte[]);
                return false;
            }

            value = _Raw.Value;
            return true;
        }

        public unsafe bool ReadFrom(WireType.WireTypeReader reader)
        {
            if (reader is null)
                throw new System.ArgumentNullException(nameof(reader));

            long size = reader.Current.Size;
            long alreadyRead = reader.TotalRead;

            while (reader.TotalRead - alreadyRead < size)
            {
                if (!reader.Read(out WireType.TypeHeader header))
                    return false;

                switch (header.Ordinal)
                {
                    case 1: // Name
                    {
                        if (header.Kind != WireType.TypeKind.ExplicitSize)
                        {
                            reader.Skip();
                        }

                        if (reader.Read(out byte[] value))
                        {
                            _Name.Value = value;
                        }
                    }
                    break;

                    case 2: // DoB
                    {
                        if (header.Kind != WireType.TypeKind.ImplicitSize)
                        {
                            reader.Skip();
                        }

                        if (reader.Read(out long value))
                        {
                            _DoB.Value = value;
                        }
                    }
                    break;

                    case 3: // Len
                    {
                        if (header.Kind != WireType.TypeKind.ImplicitSize)
                        {
                            reader.Skip();
                        }

                        if (reader.Read(out long value))
                        {
                            _Len.Value = value;
                        }
                    }
                    break;

                    case 4: // Mother
                    {
                        if (header.Kind != WireType.TypeKind.ExplicitSizeList)
                        {
                            reader.Skip();
                        }

                        if (reader.Read(out Sample.More.Person[] values))
                        {
                            _Mother.Value = values;
                        }
                    }
                    break;

                    case 7: // Friends
                    {
                        if (header.Kind != WireType.TypeKind.ExplicitSizeList)
                        {
                            reader.Skip();
                        }

                        if (reader.Read(out byte[][] values))
                        {
                            _Friends.Value = values;
                        }
                    }
                    break;

                    case 8: // Raw
                    {
                        if (header.Kind != WireType.TypeKind.ExplicitSize)
                        {
                            reader.Skip();
                        }

                        if (reader.Read(out byte[] value))
                        {
                            _Raw.Value = value;
                        }
                    }
                    break;

                    case 567: // Images
                    {
                        if (header.Kind != WireType.TypeKind.ExplicitSizeList)
                        {
                            reader.Skip();
                        }

                        if (reader.Read(out byte[][] values))
                        {
                            _Images.Value = values;
                        }
                    }
                    break;

                }
            }

            return true;
        }

        public unsafe void WriteTo(WireType.WireTypeWriter writer)
        {
            if (_Name.Exists)
            {
                writer.Write(_Name.Ordinal, _Name.Value);
            }

            if (_DoB.Exists)
            {
                writer.Write(_DoB.Ordinal, _DoB.Value);
            }

            if (_Len.Exists)
            {
                writer.Write(_Len.Ordinal, _Len.Value);
            }

            if (_Mother.Exists)
            {
                writer.Write(_Mother.Ordinal, _Mother.Value);
            }

            if (_Friends.Exists)
            {
                writer.Write(_Friends.Ordinal, _Friends.Value);
            }

            if (_Raw.Exists)
            {
                writer.Write(_Raw.Ordinal, _Raw.Value);
            }

            if (_Images.Exists)
            {
                writer.Write(_Images.Ordinal, _Images.Value);
            }

        }
    }
}
