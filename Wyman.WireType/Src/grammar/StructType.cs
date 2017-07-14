using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wyman.WireType.grammar
{
    class StructType : BaseType
    {
        public StructType(string name, TypeSet members, SourceSpan span)
            : base(name, span)
        {
            _members = members;

            foreach(var member in members)
            {
                member.definition.Parent(this);
            }
        }

        private TypeSet _members;
        public TypeSet members()
        {
            return _members;
        }

        public override void WriteToTable(SymbolTable table)
        {
            table.Add(this);

            foreach(var item in _members)
            {
                item.definition.WriteToTable(table);
            }
        }
    }

    partial class Parser
    {
        const string KeywordStruct = "struct";
        const string TokenStructFinal = TokenBlockFinal;
        const string TokenStructFirst = TokenBlockFirst;

        public bool Match(SourceStream source, out StructType result)
        {
            result = null;

            if (!source.IsValid())
                return false;

            var slice = source.GetSlice();

            if (!slice.MatchString(KeywordStruct))
                return false;

            SkipCommentsAndWhitespace(slice);

            if (!slice.ReadWord(out string name))
                throw new ParseError("expected struct name.", slice);

            if (!name.IsNameLegal())
            {
                slice.MoveBy(-name.Length);
                throw new ParseError($"'{name}' is not a legal struct name.", slice);
            }

            SkipCommentsAndWhitespace(slice);

            if (!slice.MatchString(TokenStructFirst))
                throw new ParseError($"expeceted '{TokenStructFirst}'.", slice);

            var members = new TypeSet();

            while (slice.IsValid())
            {
                SkipCommentsAndWhitespace(slice);

                if (slice.MatchString(TokenStructFinal))
                    break;

                if (!Match(slice, out MemberType member))
                    throw new ParseError("failed to parse member.", slice);

                members.Add(member.Ordinal(), member);
            }

            var span = source.Join(slice);

            result = new StructType(name, members, span);

            return true;
        }

        public bool MatchStruct(SourceStream source, out BaseType result)
        {
            StructType value;
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
