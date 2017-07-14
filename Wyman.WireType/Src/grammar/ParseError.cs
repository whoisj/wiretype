using System;

namespace Wyman.WireType.grammar
{
    class ParseError : Exception
    {
        public ParseError(string message, SourceStream source)
            : base(format_message(message, source))
        { }

        static string format_message(string message, SourceStream source)
        {
            return $"parse error: {message}\n\n {source.AsString()}";
        }
    }
}
