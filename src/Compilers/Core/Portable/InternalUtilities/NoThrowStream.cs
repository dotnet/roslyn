
using System;
using System.IO;

namespace Roslyn.Utilities
{
    struct NoThrowStreamDisposer : IDisposable
    {
        public Stream Stream { get; }

        private readonly Action<Exception> _exceptionHandler;
        
        public NoThrowStreamDisposer(Stream stream, Action<Exception> exceptionHandler)
        {
            Stream = stream;
            _exceptionHandler = exceptionHandler;
        }

        public void Dispose()
        {
            try
            {
                Stream.Dispose();
            }
            catch (Exception e)
            {
                _exceptionHandler(e);
            }
        }
    }
}
