using System;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace Wyman.WireType.grammar
{
    [DebuggerDisplay("{DebuggerDisplay, nq}")]
    unsafe class SourceStream : CriticalFinalizerObject, IDisposable
    {
        public SourceStream(char[] content)
        {
            char[] array = new char[content.Length];

            Array.Copy(content, 0, array, 0, content.Length);

            _handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            _content = (char*)_handle.AddrOfPinnedObject();
            _count = content.Length;
            _index = 0;
            _parent = null;
        }

        ~SourceStream()
        {
            (this as IDisposable).Dispose();
        }

        private SourceStream(SourceStream parent, int index, int count)
        {
            _content = parent._content + index;
            _count = count;
            _index = 0;
            _parent = parent;
        }

        private char* _content;
        private int _count;
        private GCHandle _handle;
        private int _index;
        private SourceStream _parent;

        public char this[int index]
        {
            get
            {
                int idx = _index + index;

                if (!IsValid() || idx < 0 || idx > _count)
                    return '\0';

                return _content[idx];
            }
        }

        internal string DebuggerDisplay
        {
            get
            {
                const int visibility_window = 64;

                var visible_after = Math.Min(visibility_window, _count - _index);

                string data_view = new string(_content, _index, visible_after);

                return $"\"{data_view}\"";
            }
        }

        public string AsString()
        {
            return new string(_content, _index, _count - _index);
        }

        public int Count()
        {
            return _count;
        }

        public int FindNext(string value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            if (!IsValid())
                return -1;

            fixed (char* find = value)
            {
                var src = _content + _index;
                var tgt = strstr(src, find);
                var cmp = tgt - src;

                return (cmp < 0)
                    ? -1
                    : (int)cmp;
            }
        }

        public SourceStream GetSlice(int index, int count)
        {
            int idx = _index + index;
            int last = _index + index + count;

            if (idx < 0 || idx > _count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0 || last > _count)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (!IsValid())
                return null;

            return new SourceStream(this, idx, count);
        }

        public SourceStream GetSlice(int count)
            => GetSlice(0, count);

        public SourceStream GetSlice()
            => GetSlice(0, _count - _index);

        public int Index()
        {
            return _index;
        }

        public bool IsValid()
        {
            return _content != null
                && _index < _count;
        }

        public SourceSpan Join(SourceStream slice)
        {
            if (slice is null)
                throw new ArgumentNullException(nameof(slice));
            if (slice._parent != this)
                throw new ArgumentException($"Can only join children to parent `{nameof(SourceStream)}` instances.", nameof(slice));

            if (slice._index == 0)
                return new SourceSpan(this, _index, 0);

            var span = new SourceSpan(this, _index, slice._index - 1);

            _index += slice._index;

            return span;
        }

        public bool MatchString(string value, bool advance = true)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            if (!IsValid())
                return false;

            fixed (char* find = value)
            {
                if (strstr(_content + _index, find) != _content + _index)
                    return false;
            }

            if (advance)
            {
                _index += value.Length;
            }

            return true;
        }

        public void MoveBy(int count)
        {
            if (_index + count > _count || _index + count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            _index += count;
        }

        public void MoveTo(int index)
        {
            if (index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));

            _index = index;
        }

        public bool ReadAbsolute(int index, int count, out string value)
        {
            if (_content == null
                || index < 0 || count < 0
                || index + count > _count
                || index + count < 0)
            {
                value = null;
            }
            else
            {
                value = new string(_content, index, count);
            }

            return !(value is null);
        }

        public bool ReadWord(out string word, bool advance = true)
        {
            word = null;

            for (int i = _index; i < _count; i += 1)
            {
                if (!_content[i].IsNameLegal())
                {
                    int length = i - _index;

                    if (length <= 0)
                        break;

                    word = new string(_content, _index, length);
                    break;
                }
            }

            if (word is null)
                return false;

            if (advance)
            {
                _index += word.Length;
            }

            return true;
        }

        public void SkipWhitespace()
        {
            if (!IsValid())
                return;

            while (_content[_index].IsWhitespace())
            {
                _index += 1;
            }
        }

        void IDisposable.Dispose()
        {
            if (_handle.IsAllocated)
            {
                _content = null;
                _handle.Free();
            }
        }

        [DllImport("shlwapi.dll", BestFitMapping = true, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "StrChrW", ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = true)]
        private static extern char* strchr(
            [In] char* source,
            [In] char find);

        [DllImport("shlwapi.dll", BestFitMapping = true, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "StrCmpW", ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = true)]
        [return: MarshalAs(UnmanagedType.I4)]
        private static extern int strcmp(
            [In] char* source,
            [In] char* find);

        [DllImport("shlwapi.dll", BestFitMapping = true, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "StrStrW", ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = true)]
        private static extern char* strstr(
            [In] char* source,
            [In] char* find);
    }
}
