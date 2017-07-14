namespace Sample
{
    partial class Complex : WireType.IWireType
    {
        public Complex()
        { }

        private WireType.StructMember<Sample.Complex[]> _Branches = new WireType.StructMember<Sample.Complex[]>(4);
        private WireType.StructMember<Sample.Basic[]> _Leaves = new WireType.StructMember<Sample.Basic[]>(1);

        public void Branches(System.Collections.Generic.IReadOnlyList<Sample.Complex> value)
        {
            if (value is null || value.Count == 0)
            {
                _Branches.Value = System.Array.Empty<Sample.Complex>();
                return;
            }

            // Public export is a read-only list, whereas an array was recorded.
            _Branches.Value = System.Linq.Enumerable.ToArray(value);
        }

        public bool Branches(out System.Collections.Generic.IReadOnlyList<Sample.Complex> value)
        {
            if (!_Branches.Exists)
            {
                value = default(System.Collections.Generic.IReadOnlyList<Sample.Complex>);
                return false;
            }

            value = _Branches.Value;
            return true;
        }

        public void Leaves(System.Collections.Generic.IReadOnlyList<Sample.Basic> value)
        {
            if (value is null || value.Count == 0)
            {
                _Leaves.Value = System.Array.Empty<Sample.Basic>();
                return;
            }

            // Public export is a read-only list, whereas an array was recorded.
            _Leaves.Value = System.Linq.Enumerable.ToArray(value);
        }

        public bool Leaves(out System.Collections.Generic.IReadOnlyList<Sample.Basic> value)
        {
            if (!_Leaves.Exists)
            {
                value = default(System.Collections.Generic.IReadOnlyList<Sample.Basic>);
                return false;
            }

            value = _Leaves.Value;
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
                    case 1: // Leaves
                    {
                        if (header.Kind != WireType.TypeKind.ExplicitSizeList)
                        {
                            reader.Skip();
                        }

                        if (reader.Read(out Sample.Basic[] values))
                        {
                            _Leaves.Value = values;
                        }
                    }
                    break;

                    case 4: // Branches
                    {
                        if (header.Kind != WireType.TypeKind.ExplicitSizeList)
                        {
                            reader.Skip();
                        }

                        if (reader.Read(out Sample.Complex[] values))
                        {
                            _Branches.Value = values;
                        }
                    }
                    break;

                }
            }

            return true;
        }

        public unsafe void WriteTo(WireType.WireTypeWriter writer)
        {
            if (_Leaves.Exists)
            {
                writer.Write(_Leaves.Ordinal, _Leaves.Value);
            }

            if (_Branches.Exists)
            {
                writer.Write(_Branches.Ordinal, _Branches.Value);
            }

        }
    }
}
