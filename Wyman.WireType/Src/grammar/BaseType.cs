using System.Diagnostics;
using System.Text;

namespace Wyman.WireType.grammar
{
    [DebuggerDisplay("{DebuggerDisplay, nq}")]
    partial class BaseType
    {
        protected BaseType(string name, SourceSpan span)
        {
            _name = name;
            _span = span;
        }

        protected string _name;
        protected BaseType _parent;
        protected SourceSpan _span;

        private string DebuggerDisplay
        {
            get { return $"{GetType().Name}: \"{AsString()}\""; }
        }

        public string AsString()
        {
            return _span.AsString();
        }

        public string FullName()
        {
            StringBuilder buffer = new StringBuilder();

            WriteName(buffer);

            return buffer.ToString();
        }

        public string Name()
        {
            return _name;
        }

        public bool Parent(out BaseType parent)
        {
            parent = _parent;

            return parent != null;
        }

        public void Parent(BaseType value)
        {
            _parent = value;
        }

        public SourceSpan span()
        {
            return _span;
        }

        public virtual void WriteToTable(SymbolTable table)
        {
            table.Add(this);
        }

        protected void WriteName(StringBuilder buffer)
        {
            if (!(_parent is null))
            {
                _parent.WriteName(buffer);
                buffer.Append('.');
            }

            buffer.Append(_name);
        }
    }
}
