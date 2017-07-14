using System;
using System.Collections;
using System.Collections.Generic;

namespace Wyman.WireType
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    class TypeSet<T> : IEnumerable<(int ordinal, T definition)>
        where T : grammar.BaseType
    {
        public TypeSet()
        {
            _array = new (int, T)[32];
            _count = 0;
            _syncpoint = new object();
        }

        public TypeSet(IEnumerable<(int ordinal, T definition)> collection)
            : this()
        {
            Add(collection);
        }

        private (int, T)[] _array;
        private int _count;
        private readonly object _syncpoint;

        private string DebuggerDisplay
        {
            get
            {
                var c = Count();

                return $"{typeof(T).Name}: {c}";
            }
        }

        public void Add(int oridinal, T member)
        {
            lock (_syncpoint)
            {
                if (_count + 1 == _array.Length)
                {
                    Resize();
                }

                _array[_count] = (oridinal, member);

                _count += 1;
            }
        }

        public void Add(IEnumerable<(int ordinal, T definition)> collection)
        {
            foreach (var item in collection)
            {
                Add(item.ordinal, item.definition);
            }
        }

        public int Count()
        {
            lock (_syncpoint)
            {
                return _count;
            }
        }

        IEnumerator<(int ordinal, T definition)> IEnumerable<(int ordinal, T definition)>.GetEnumerator()
        {
            (int, T)[] copy;

            lock (_syncpoint)
            {
                copy = new(int, T)[_count];

                Array.Copy(_array, 0, copy, 0, _count);
            }

            for (int i = 0; i < copy.Length; i += 1)
            {
                yield return copy[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
            => (this as IEnumerable<(int ordinal, T definition)>).GetEnumerator();

        private void Resize()
        {
            Array.Resize(ref _array, _array.Length * 2);
        }
    }

    class TypeSet : TypeSet<grammar.MemberType>
    { }
}
