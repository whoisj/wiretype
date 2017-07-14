using System;
using System.Collections;
using System.Collections.Generic;

namespace Wyman.WireType
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    class TypeList<T> : IEnumerable<T>
        where T : grammar.BaseType
    {
        public TypeList()
        {
            _array = new T[32];
            _count = 0;
            _syncpoint = new object();
        }

        public TypeList(IEnumerable<T> collection)
            : this()
        {
            Add(collection);
        }

        private T[] _array;
        private int _count;
        private readonly object _syncpoint;

        internal string DebuggerDisplay
        {
            get
            {
                var c = Count();

                return $"{typeof(T).Name}: {c}";
            }
        }

        public void Add(T value)
        {
            lock (_syncpoint)
            {
                if (_count + 1 == _array.Length)
                {
                    Resize();
                }

                _array[_count] = value;

                _count += 1;
            }
        }

        public void Add(IEnumerable<T> collection)
        {
            foreach (var item in collection)
            {
                Add(item);
            }
        }

        public int Count()
        {
            lock (_syncpoint)
            {
                return _count;
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            T[] copy;

            lock (_syncpoint)
            {
                copy = new T[_count];
                Array.Copy(_array, copy, _count);
            }

            for (int i = 0; i < copy.Length; i += 1)
            {
                yield return copy[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
            => (this as IEnumerable<T>).GetEnumerator();

        private void Resize()
        {
            Array.Resize(ref _array, _array.Length * 2);
        }
    }

    class TypeList : TypeList<grammar.BaseType>
    { }
}
