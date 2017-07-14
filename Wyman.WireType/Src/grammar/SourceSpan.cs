using System.Diagnostics;

namespace Wyman.WireType.grammar
{
    [DebuggerDisplay("{DebuggerDisplay, nq}")]
    class SourceSpan
    {
        public SourceSpan(SourceStream source, int index, int count)
        {
            _count = count;
            _index = index;
            _source = source;
        }

        private int _count;
        private int _index;
        private SourceStream _source;

        private string DebuggerDisplay
        {
            get { return $"{nameof(SourceSpan)}: {AsString()}"; }
        }

        public string AsString()
        {
            string value;
            if (_source.ReadAbsolute(_index, _count, out value))
                return value;

            return "error!";
        }
    }
}
