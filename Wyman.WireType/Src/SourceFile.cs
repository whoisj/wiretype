using System;
using System.IO;
using System.Runtime.ConstrainedExecution;

namespace Wyman.WireType
{
    class SourceFile : CriticalFinalizerObject, IDisposable
    {
        private SourceFile(string path)
        {
            if (path is null)
                throw new ArgumentNullException(nameof(path));

            var info = new FileInfo(path);
            if (!info.Exists)
                throw new FileNotFoundException("File not found", path);

            _path = info.FullName;
            _sourceStream = null;
            _syncpoint = new object();
        }

        ~SourceFile()
        {
            (this as IDisposable).Dispose();
        }

        private readonly string _path;
        private grammar.SourceStream _sourceStream;
        private readonly object _syncpoint;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public grammar.SourceStream GetSource()
        {
            lock (_syncpoint)
            {
                if (_sourceStream is null)
                {
                    var info = new FileInfo(_path);
                    if (!info.Exists)
                        throw new FileNotFoundException("File not found", _path);

                    using (var stream = info.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var reader = new StreamReader(stream))
                    {
                        string content = reader.ReadToEnd();
                        var array = content.ToCharArray();

                        _sourceStream = new grammar.SourceStream(array);
                    }
                }

                return _sourceStream;
            }
        }

        public static bool OpenFile(string path, out SourceFile result)
        {
            result = null;

            if (path is null)
                return false;

            var info = new FileInfo(path);
            if (!info.Exists)
                return false;

            result = new SourceFile(path);
            return true;
        }

        public string Path()
        {
            lock (_syncpoint)
            {
                return _path;
            }
        }

        void IDisposable.Dispose()
        {
            lock (_syncpoint)
            {
                if (_sourceStream is IDisposable disposable)
                {
                    disposable.Dispose();
                    _sourceStream = null;
                }
            }
        }
    }
}
