using System.Collections.Generic;

namespace Wyman.WireType.grammar
{
    class EnumType : BaseType
    {
        public EnumType(string name, (string name, byte ordinal)[] members, SourceSpan span)
            : base(name, span)
        {
            _members = members;
        }

        private (string name, byte ordinal)[] _members;

        public IReadOnlyList<(string name, byte ordinal)> members()
        {
            return _members;
        }
    }

    partial class Parser
    {
        const string KeywordEnum = "enum";
        const string TokenEnumAssignment = TokenAssignment;
        const string TokenEnumFinal = TokenBlockFinal;
        const string TokenEnumFirst = TokenBlockFirst;
        const string TokenEnumSeparator = ",";

        public bool Match(SourceStream source, out EnumType result)
        {
            result = null;

            if (!source.IsValid())
                return false;

            var slice = source.GetSlice();

            if (!slice.MatchString(KeywordEnum))
                return false;

            SkipCommentsAndWhitespace(slice);

            if (!slice.ReadWord(out string name))
                throw new ParseError("expected to read enum name.", slice);

            if (!name.IsNameLegal())
            {
                slice.MoveBy(-name.Length);
                throw new ParseError($"'{name}' is not a legal enum name.", slice);
            }

            SkipCommentsAndWhitespace(slice);

            if (!slice.MatchString(TokenEnumFirst))
                throw new ParseError($"expected to find '{TokenEnumFirst}'.", slice);

            SkipCommentsAndWhitespace(slice);

            List<(string name, byte ordinal)> values = new List<(string, byte)>();

            while (slice.IsValid())
            {
                SkipCommentsAndWhitespace(slice);

                if (slice.MatchString(TokenEnumFinal))
                    break;

                if (!slice.ReadWord(out string member_name))
                    throw new ParseError("expected to read member name.", slice);

                if (!member_name.IsNameLegal())
                {
                    slice.MoveBy(-member_name.Length);
                    throw new ParseError($"'{member_name}' is not a legal member name.", slice);
                }

                SkipCommentsAndWhitespace(slice);

                if (!slice.MatchString(TokenEnumAssignment))
                    throw new ParseError($"expected '{TokenEnumAssignment}'.", slice);

                SkipCommentsAndWhitespace(slice);

                if (!slice.ReadWord(out string ordstr))
                    throw new ParseError("unable to read enum value.", slice);

                if (!ordstr.TryParse(out byte ordinal))
                {
                    slice.MoveBy(-ordstr.Length);
                    throw new ParseError($"'{ordstr}' is not a valid oridinal.", slice);
                }

                values.Add((member_name, ordinal));

                SkipCommentsAndWhitespace(slice);

                if (!slice.MatchString(TokenEnumSeparator))
                {
                    if (slice.MatchString(TokenEnumFinal))
                        break;

                    throw new ParseError($"expected '{TokenEnumSeparator}' or '{TokenEnumFinal}'.", slice);
                }

                SkipCommentsAndWhitespace(slice);
            }

            var span = source.Join(slice);

            result = new EnumType(name, values.ToArray(), span);

            return true;
        }

        public bool match_enum(SourceStream source, out BaseType result)
        {
            EnumType value;
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
