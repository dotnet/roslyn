
using System;
using System.Diagnostics;
using System.IO;

namespace Roslyn.Utilities
{
    internal class NoThrowStreamDisposer : IDisposable
    {
        private bool? _failed; // Nullable to assert that this is only checked after dispose
        private readonly string _filePath;
        private readonly TextWriter _writer;
        private readonly Action<Exception, string, TextWriter> _exceptionHandler;

        public Stream Stream { get; }

        public bool Failed
        {
            get
            {
                Debug.Assert(_failed != null);
                return _failed.GetValueOrDefault();
            }
        }
        
        public NoThrowStreamDisposer(
            Stream stream,
            string filePath,
            TextWriter writer,
            Action<Exception, string, TextWriter> exceptionHandler)
        {
            Stream = stream;
            _failed = null;
            _filePath = filePath;
            _writer = writer;
            _exceptionHandler = exceptionHandler;
        }

        public void Dispose()
        {
            try
            {
                Stream.Dispose();
                _failed = false;
            }
            catch (Exception e)
            {
                _exceptionHandler(e, _filePath, _writer);
                _failed = true;
            }
        }
    }
}
