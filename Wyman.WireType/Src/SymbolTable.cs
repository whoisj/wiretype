using System;
using System.Collections;
using System.Collections.Generic;

namespace Wyman.WireType
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    class SymbolTable : IEnumerable<(string name, grammar.BaseType type)>
    {
        public SymbolTable()
        {
            _syncpoint = new object();
            _table = new Dictionary<string, grammar.BaseType>(StringComparer.Ordinal);
        }

        private readonly object _syncpoint;
        private readonly Dictionary<string, grammar.BaseType> _table;

        internal string DebuggerDisplay
        {
            get { return $"{nameof(SymbolTable)}: {Count()}"; }
        }

        public void Add(grammar.BaseType type)
        {
            if (type is null)
                return;

            var full_name = type.FullName();

            lock (_syncpoint)
            {
                _table[full_name] = type;
            }
        }

        public void Add(SymbolTable other)
        {
            lock (_syncpoint)
            {
                foreach (var kvp in other)
                {
                    if (_table.TryGetValue(kvp.name, out grammar.BaseType match))
                    {
                        // Defining a namespace more than once is no problem, so allow for that.
                        if (match is grammar.NamespaceType && kvp.type is grammar.NamespaceType)
                            continue;

                        throw new ArgumentException($"Type \"{kvp.name}\" already defined in table.");
                    }

                    _table[kvp.name] = kvp.type;
                }
            }
        }

        public int Count()
        {
            lock (_syncpoint)
            {
                return _table.Count;
            }
        }

        public grammar.BaseType Get(string fullName)
        {
            if (fullName is null)
                return null;

            lock (_syncpoint)
            {
                if (_table.TryGetValue(fullName, out grammar.BaseType value))
                    return value;
            }

            return null;
        }

        public IEnumerable<T> GetAll<T>()
            where T : grammar.BaseType
        {
            List<grammar.BaseType> copy;

            lock (_syncpoint)
            {
                copy = new List<grammar.BaseType>(_table.Count);

                foreach (var kvp in _table)
                {
                    copy.Add(kvp.Value);
                }
            }

            for (int i = 0; i < copy.Count; i += 1)
            {
                if (copy[i].GetType() != typeof(T))
                    continue;

                yield return copy[i] as T;
            }
        }

        IEnumerator<(string name, grammar.BaseType type)> IEnumerable<(string name, grammar.BaseType type)>.GetEnumerator()
        {
            List<(string, grammar.BaseType)> copy;

            lock (_syncpoint)
            {
                copy = new List<(string, grammar.BaseType)>(_table.Count);

                foreach (var kvp in _table)
                {
                    copy.Add((kvp.Key, kvp.Value));
                }
            }

            for (int i = 0; i < copy.Count; i += 1)
            {
                yield return copy[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
            => (this as IEnumerable<(string name, grammar.BaseType type)>).GetEnumerator();
    }
}
