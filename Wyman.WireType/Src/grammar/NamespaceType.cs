namespace Wyman.WireType.grammar
{
    class NamespaceType : BaseType
    {
        public NamespaceType(string name, TypeList types, SourceSpan span)
            : base(name, span)
        {
            _types = types;

            foreach (var item in types)
            {
                item.Parent(this);
            }
        }

        private TypeList _types;

        public SymbolTable create_table()
        {
            var table = new SymbolTable();

            WriteToTable(table);

            return table;
        }

        public TypeList types()
        {
            return _types;
        }

        public override void WriteToTable(SymbolTable table)
        {
            table.Add(this);

            foreach (var item in _types)
            {
                item.WriteToTable(table);
            }
        }
    }

    partial class Parser
    {
        const string KeywordNamespace = "namespace";
        const string TokenNamespaceFinal = TokenBlockFinal;
        const string TokenNamespaceFirst = TokenBlockFirst;

        public bool Match(SourceStream source, out NamespaceType result)
        {
            result = null;

            if (!source.IsValid())
                return false;

            var slice = source.GetSlice();

            SkipCommentsAndWhitespace(slice);

            if (!slice.MatchString(KeywordNamespace))
                return false;

            SkipCommentsAndWhitespace(slice);

            string namespace_name = string.Empty;

            name_match:

            if (!slice.ReadWord(out string name))
                throw new ParseError($"expected namespace 'name'.", slice);

            if (!name.IsNameLegal())
            {
                slice.MoveBy(-name.Length);
                throw new ParseError($"'{name}' is not a legal namespace.", slice);
            }

            namespace_name += name;

            SkipCommentsAndWhitespace(slice);

            if (slice.MatchString(TokenNameExtension))
            {
                namespace_name += TokenNameExtension;
                goto name_match;
            }

            if (!slice.MatchString(TokenNamespaceFirst))
                throw new ParseError($"expected '{TokenNamespaceFirst}'.", slice);

            var types = new TypeList();
            var parsers = new GrammarParserDelegate[]
            {
                MatchStruct,
                match_enum,
                MatchNamespace,
            };

            while (slice.IsValid())
            {
                SkipCommentsAndWhitespace(slice);

                if (slice.MatchString(TokenNamespaceFinal))
                    break;

                if (!MatchAny(slice, parsers, out BaseType type))
                    throw new ParseError("cannot parse", slice);

                types.Add(type);
            }

            var span = source.Join(slice);

            result = new NamespaceType(namespace_name, types, span);

            return true;
        }

        public bool MatchNamespace(SourceStream source, out BaseType result)
        {
            NamespaceType value;
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
