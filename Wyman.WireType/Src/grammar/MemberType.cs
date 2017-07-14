namespace Wyman.WireType.grammar
{
    class MemberType : BaseType
    {
        public MemberType(string name, string type_name, int ordinal, MemberKind kind, SourceSpan span)
            : base(name, span)
        {
            _kind = kind;
            _ordinal = ordinal;
            _type_name = type_name;
        }

        private MemberKind _kind;
        private int _ordinal;
        private string _type_name;

        public MemberKind Kind()
        {
            return _kind;
        }

        public int Ordinal()
        {
            return _ordinal;
        }

        public static MemberKind ResolveKind(string type_name)
        {
            switch (type_name)
            {
                case "i32": return MemberKind.i32Type;
                case "u32": return MemberKind.u32Type;
                case "i64": return MemberKind.i64Type;
                case "u64": return MemberKind.u64Type;
                case "f32": return MemberKind.f32Type;
                case "f64": return MemberKind.f64Type;
                case "string": return MemberKind.stringType;
                case "timestamp": return MemberKind.timestampType;
                case "timespan": return MemberKind.timespanType;
                case "blob": return MemberKind.blobType;

                default:
                    return MemberKind.unknown;
            }
        }

        public string TypeName()
        {
            return _type_name;
        }

        public void Update(string type_name, MemberKind kind)
        {
            _kind = kind;
            _type_name = type_name;
        }
    }

    enum MemberKind
    {
        unknown = 0x00,

        i32Type = 0x01,
        u32Type = 0x02,
        i64Type = 0x03,
        u64Type = 0x04,
        f32Type = 0x05,
        f64Type = 0x06,
        timestampType = 0x07,
        timespanType = 0x08,
        enumType = 0x0A,
        stringType = 0x0B,
        blobType = 0x0C,
        structType = 0x0D,

        List = 0x80,

        /* list of types */
        i32List = i32Type | List,
        u32List = u32Type | List,
        i64List = i64Type | List,
        u64List = u64Type | List,
        f32List = f32Type | List,
        f64List = f64Type | List,
        timestampList = timestampType | List,
        timespanList = timespanType | List,
        enumList = enumType | List,
        stringList = stringType | List,
        blobList = blobType | List,
        structList = structType | List,
    }

    partial class Parser
    {
        const string KeywordMemberList = "list";
        const string KeywordMemberType = "type";
        const string TokenMemberAssignment = TokenAssignment;
        const string TokenMemberTerminator = ";";

        public bool Match(SourceStream source, out MemberType result)
        {
            result = null;

            if (!source.IsValid())
                return false;

            var kind = MemberKind.unknown;
            var slice = source.GetSlice();

            if (slice.MatchString(KeywordMemberList))
            {
                kind |= MemberKind.List;
            }
            else if (!slice.MatchString(KeywordMemberType))
                return false;

            SkipCommentsAndWhitespace(slice);

            if (!slice.ReadWord(out string type_name))
                throw new ParseError("expected type name.", slice);

            kind |= MemberType.ResolveKind(type_name);

            SkipCommentsAndWhitespace(slice);

            if (!slice.ReadWord(out string name))
                throw new ParseError("expected to read field name.", slice);

            SkipCommentsAndWhitespace(slice);

            if (!slice.MatchString(TokenMemberAssignment))
                throw new ParseError($"expected '{TokenMemberAssignment}'.", slice);

            SkipCommentsAndWhitespace(slice);

            if (!slice.ReadWord(out string ordstr))
                throw new ParseError("expected to read member ordinal.", slice);

            if (!ordstr.TryParse(out int ordinal))
            {
                slice.MoveBy(-ordstr.Length);
                throw new ParseError($"'{ordstr}' is not a valid oridinal.", slice);
            }

            SkipCommentsAndWhitespace(slice);

            if (!slice.MatchString(TokenMemberTerminator))
                throw new ParseError($"expected '{TokenMemberTerminator}'.", slice);

            var span = source.Join(slice);

            result = new MemberType(name, type_name, ordinal, kind, span);

            return true;
        }

        public bool MatchMember(SourceStream source, out BaseType result)
        {
            MemberType value;
            if (Match(source, out value))
            {
                result = value;
                return true;
            }

            result = null;
            return false;
        }
    }
}
