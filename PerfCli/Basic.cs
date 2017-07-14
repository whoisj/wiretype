namespace Sample
{
    partial class Basic : WireType.IWireType
    {
        public Basic()
        { }

        private WireType.StructMember<Sample.Values> _Enum = new WireType.StructMember<Sample.Values>(3);
        private WireType.StructMember<Sample.Values[]> _Enums = new WireType.StructMember<Sample.Values[]>(6);
        private WireType.StructMember<ulong[]> _List = new WireType.StructMember<ulong[]>(5);
        private WireType.StructMember<byte[]> _Text = new WireType.StructMember<byte[]>(1);
        private WireType.StructMember<int> _Value = new WireType.StructMember<int>(2);
        private WireType.StructMember<float[]> _Values = new WireType.StructMember<float[]>(4);

        public void Enum(Sample.Values value)
        {
            _Enum.Value = value;
        }

        public bool Enum(out Sample.Values value)
        {
            if (!_Enum.Exists)
            {
                value = default(Sample.Values);
                return false;
            }

            value = _Enum.Value;
            return true;
        }

        public void Enums(System.Collections.Generic.IReadOnlyList<Sample.Values> value)
        {
            if (value is null || value.Count == 0)
            {
                _Enums.Value = System.Array.Empty<Sample.Values>();
                return;
            }

            // Public export is a read-only list, whereas an array was recorded.
            _Enums.Value = System.Linq.Enumerable.ToArray(value);
        }

        public bool Enums(out System.Collections.Generic.IReadOnlyList<Sample.Values> value)
        {
            if (!_Enums.Exists)
            {
                value = default(System.Collections.Generic.IReadOnlyList<Sample.Values>);
                return false;
            }

            value = _Enums.Value;
            return true;
        }

        public void List(System.Collections.Generic.IReadOnlyList<ulong> value)
        {
            if (value is null || value.Count == 0)
            {
                _List.Value = System.Array.Empty<ulong>();
                return;
            }

            // Public export is a read-only list, whereas an array was recorded.
            _List.Value = System.Linq.Enumerable.ToArray(value);
        }

        public bool List(out System.Collections.Generic.IReadOnlyList<ulong> value)
        {
            if (!_List.Exists)
            {
                value = default(System.Collections.Generic.IReadOnlyList<ulong>);
                return false;
            }

            value = _List.Value;
            return true;
        }

        public void Text(string value)
        {
            if (value is null)
            {
                _Text.Value = null;
                return;
            }

            // NetFx System.String are UTF-16 encoded, whereas UTF-8 values were recorded.
            _Text.Value = System.Text.Encoding.UTF8.GetBytes(value);
        }

        public bool Text(out string value)
        {
            if (!_Text.Exists)
            {
                value = default(string);
                return false;
            }

            // NetFx System.String are UTF-16 encoded, whereas UTF-8 values were recorded.
            value = System.Text.Encoding.UTF8.GetString(_Text.Value);
            return true;
        }

        public void Value(int value)
        {
            _Value.Value = value;
        }

        public bool Value(out int value)
        {
            if (!_Value.Exists)
            {
                value = default(int);
                return false;
            }

            value = _Value.Value;
            return true;
        }

        public void Values(System.Collections.Generic.IReadOnlyList<float> value)
        {
            if (value is null || value.Count == 0)
            {
                _Values.Value = System.Array.Empty<float>();
                return;
            }

            // Public export is a read-only list, whereas an array was recorded.
            _Values.Value = System.Linq.Enumerable.ToArray(value);
        }

        public bool Values(out System.Collections.Generic.IReadOnlyList<float> value)
        {
            if (!_Values.Exists)
            {
                value = default(System.Collections.Generic.IReadOnlyList<float>);
                return false;
            }

            value = _Values.Value;
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
                    case 1: // Text
                    {
                        if (header.Kind != WireType.TypeKind.ExplicitSize)
                        {
                            reader.Skip();
                        }

                        if (reader.Read(out byte[] value))
                        {
                            _Text.Value = value;
                        }
                    }
                    break;

                    case 2: // Value
                    {
                        if (header.Kind != WireType.TypeKind.ImplicitSize)
                        {
                            reader.Skip();
                        }

                        if (reader.Read(out int value))
                        {
                            _Value.Value = value;
                        }
                    }
                    break;

                    case 3: // Enum
                    {
                        if (header.Kind != WireType.TypeKind.ImplicitSize)
                        {
                            reader.Skip();
                        }

                        if (reader.Read(out int value))
                        {
                            _Enum.Value = (Sample.Values)value;
                        }
                    }
                    break;

                    case 4: // Values
                    {
                        if (header.Kind != WireType.TypeKind.ImplicitSizeList)
                        {
                            reader.Skip();
                        }

                        if (reader.Read(out float[] values))
                        {
                            _Values.Value = values;
                        }
                    }
                    break;

                    case 5: // List
                    {
                        if (header.Kind != WireType.TypeKind.ImplicitSizeList)
                        {
                            reader.Skip();
                        }

                        if (reader.Read(out ulong[] values))
                        {
                            _List.Value = values;
                        }
                    }
                    break;

                    case 6: // Enums
                    {
                        if (header.Kind != WireType.TypeKind.ImplicitSizeList)
                        {
                            reader.Skip();
                        }

                        if (reader.Read(out int[] temp))
                        {
                            var values = new Sample.Values[temp.Length];

                            System.Buffer.BlockCopy(temp, 0, values, 0, temp.Length * sizeof(int));

                            _Enums.Value = values;
                        }
                    }
                    break;

                }
            }

            return true;
        }

        public unsafe void WriteTo(WireType.WireTypeWriter writer)
        {
            if (_Text.Exists)
            {
                writer.Write(_Text.Ordinal, _Text.Value);
            }

            if (_Value.Exists)
            {
                writer.Write(_Value.Ordinal, _Value.Value);
            }

            if (_Enum.Exists)
            {
                int value = (int)_Enum.Value;
                writer.Write(_Enum.Ordinal, value);
            }

            if (_Values.Exists)
            {
                writer.Write(_Values.Ordinal, _Values.Value);
            }

            if (_List.Exists)
            {
                writer.Write(_List.Ordinal, _List.Value);
            }

            if (_Enums.Exists)
            {
                var values = new int[_Enums.Value.Length];

                System.Buffer.BlockCopy(_Enums.Value, 0, values, 0, values.Length * sizeof(int));

                writer.Write(_Enums.Ordinal, values);
            }

        }
    }
}
