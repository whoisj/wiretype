using System;
using System.Collections.Generic;
using System.Globalization;
using static System.Globalization.CultureInfo;

namespace Wyman.WireType.grammar
{
    partial class Parser
    {
        const string TokenAssignment = "=";
        const string TokenBlockFirst = "{";
        const string TokenBlockFinal = "}";
        const string TokenCommentBlockFirst = "/*";
        const string TokenCommentBlockFinal = "*/";
        const string TokenCommentLineFirst = "//";
        const string TokenCommandLineFinal = "\n";
        const string TokenNameExtension = ".";

        public SymbolTable GetSymbolsFromFile(string path)
        {
            if (!SourceFile.OpenFile(path, out SourceFile file))
                throw new Exception($"Failed to open source file \"{path}\".");

            var parser = new Parser();
            var source = file.GetSource();
            var table = new SymbolTable();
            SymbolTable symbols;

            while (source.IsValid())
            {
                SkipCommentsAndWhitespace(source);

                if (ImportFile(source, out symbols))
                {
                    table.Add(symbols);

                    continue;
                }
                else if (Match(source, out NamespaceType ns))
                {
                    symbols = ns.create_table();

                    table.Add(symbols);

                    continue;
                }

                throw new ParseError($"Failed to parse \"{file.Path()}\".", source);
            }

            foreach (var item in table)
            {
                if (item.type is StructType s)
                {
                    HashSet<int> ordinals = new HashSet<int>();
                    var members = s.members();

                    foreach (var member in members)
                    {
                        if (!ordinals.Add(member.ordinal))
                            throw new Exception("NEED TYPED EXCEPTION HERE");

                        var definition = member.definition;
                        var type_kind = definition.Kind();

                        if ((type_kind & ~MemberKind.List) == 0)
                        {
                            NamespaceType ns = null;
                            BaseType parent = definition;

                            while (parent.Parent(out parent))
                            {
                                if (parent is null
                                    || (ns = parent as NamespaceType) != null)
                                    break;
                            }

                            if (ns is null)
                                throw new Exception("NEED TYPED EXCEPTION HERE");

                            var full_name = ns.FullName();
                            var parts = full_name.Split(new[] { TokenNameExtension }, StringSplitOptions.None);
                            var type_name = definition.TypeName();

                            if (parts.Length > 1)
                            {
                                Array.Reverse(parts);
                            }

                            for (int i = 0; i <= parts.Length; i += 1)
                            {
                                BaseType real_type;
                                if ((real_type = table.Get(type_name)) != null)
                                {
                                    switch (real_type)
                                    {
                                        case StructType a:
                                            type_kind |= MemberKind.structList;
                                            break;

                                        case EnumType b:
                                            type_kind |= MemberKind.enumType;
                                            break;

                                        default:
                                            throw new Exception("NEED TYPED EXCEPTION HERE");
                                    }

                                    member.definition.Update(type_name, type_kind);
                                    break;
                                }

                                if (i < parts.Length)
                                {
                                    type_name = $"{parts[i]}.{type_name}";
                                }
                            }

                            if ((type_kind & ~MemberKind.List) == 0)
                                throw new Exception($"{member.definition.FullName()} is unknown.");
                        }
                    }
                }
            }

            return table;
        }

        static bool MatchAny(SourceStream source, GrammarParserDelegate[] parsers, out BaseType result)
        {
            for (int i = 0; i < parsers.Length; i += 1)
            {
                if (parsers[i](source, out result))
                    return true;
            }

            result = null;
            return false;
        }

        static bool SkipCommentsAndWhitespace(SourceStream source)
        {
            if (!source.IsValid())
                return false;

            var slice = source.GetSlice();

            while (true)
            {
                slice.SkipWhitespace();

                if (slice.MatchString(TokenCommentBlockFirst))
                {
                    int index = slice.FindNext(TokenCommentBlockFinal);
                    if (index < 0)
                    {
                        slice.MoveBy(-TokenCommentBlockFirst.Length);

                        throw new ParseError($"unterminated comment, expected to find '{TokenCommentBlockFinal}'.", slice);
                    }

                    slice.MoveBy(index + TokenCommentBlockFinal.Length);
                }
                else if (slice.MatchString(TokenCommentLineFirst))
                {
                    int index = slice.FindNext(TokenCommandLineFinal);
                    if (index < 0)
                        if (index < 0)
                        {
                            slice.MoveBy(-TokenCommentLineFirst.Length);

                            throw new ParseError($"unterminated comment, expected to find '\\n'.", slice);
                        }

                    slice.MoveBy(index + TokenCommandLineFinal.Length);
                }
                else
                {
                    source.Join(slice);

                    return true;
                }
            }
        }
    }

    delegate bool GrammarParserDelegate(SourceStream source, out BaseType result);

    static class Extentions
    {
        public static bool IsDigit(this char c)
        {
            return char.IsDigit(c);
        }

        public static bool IsLetter(this char c)
        {
            return c >= 'A' && c <= 'Z'
                || c >= 'a' && c <= 'z';
        }

        public static bool IsNumber(this string s)
        {
            if (s is null || s.Length == 0)
                return false;

            for (int i = 0; i < s.Length; i += 1)
            {
                if (!s[i].IsDigit())
                    return false;
            }

            return true;
        }

        public static bool IsNameLegal(this char c)
        {
            return c.IsDigit()
                || c.IsLetter()
                || c == '_';
        }

        public static bool IsNameLegal(this string s)
        {
            if (s is null || s.Length == 0)
                return false;

            if (s[0].IsDigit())
                return false;

            for (int i = 1; i < s.Length; i += 1)
            {
                if (!s[i].IsNameLegal())
                    return false;
            }

            return true;
        }

        public static bool IsLegalFullName(this string s)
        {
            if (s is null || s.Length == 0)
                return false;

            if (s[0].IsDigit())
                return false;

            for (int i = 1; i < s.Length; i += 1)
            {
                if (!s[i].IsNameLegal()
                    && s[i] != '.')
                    return false;
            }

            return true;
        }

        public static bool IsWhitespace(this char c)
        {
            return char.IsWhiteSpace(c)
                || char.IsControl(c);
        }

        public static bool TryParse(this string s, out byte v)
        {
            var styles = NumberStyles.Integer;
            styles ^= NumberStyles.AllowLeadingSign;

            if (byte.TryParse(s, styles, InvariantCulture, out v))
                return true;

            return false;
        }

        public static bool TryParse(this string s, out int v)
        {
            if (int.TryParse(s, NumberStyles.Integer, InvariantCulture, out v))
                return true;

            return false;
        }
    }
}
