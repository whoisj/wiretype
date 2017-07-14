using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wyman.WireType.grammar
{
    partial class Parser
    {
        const string KeywordImport = "import";
        const string TokenImportFinal = "\"";
        const string TokenImportFirst = "\"";

        public bool ImportFile(SourceStream source, out SymbolTable result)
        {
            result = null;

            if (!source.IsValid())
                return false;

            var slice = source.GetSlice();

            if (!slice.MatchString(KeywordImport))
                return false;

            SkipCommentsAndWhitespace(slice);

            if (!slice.MatchString(TokenImportFirst))
                throw new ParseError($"Expected '{TokenImportFirst}'.", slice);

            int start = slice.Index();

            int count = slice.FindNext(TokenImportFinal);

            if (count < 0)
                throw new ParseError($"Expected '{TokenImportFinal}'.", slice);

            if (!slice.ReadAbsolute(start, count, out string path))
                throw new ParseError("Unable to read inlcude path.", slice);

            slice.MoveBy(path.Length);

            if (!slice.MatchString(TokenImportFinal))
                throw new ParseError($"Expected '{TokenImportFinal}'.", slice);

            result = GetSymbolsFromFile(path);

            source.Join(slice);

            return true;
        }
    }
}
