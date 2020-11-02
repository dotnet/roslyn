// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;

namespace Roslyn.Test.Utilities
{
    public class TestStream : Stream
    {
        private readonly bool? _canRead, _canSeek, _canWrite;
        private readonly Func<byte[], int, int, int> _readFunc;
        private readonly long? _length;
        private readonly Func<long> _getPosition;
        private readonly Action<long> _setPosition;
        private readonly Stream _backingStream;
        private readonly Action _dispose;

        public TestStream(
            bool? canRead = null,
            bool? canSeek = null,
            bool? canWrite = null,
            Func<byte[], int, int, int> readFunc = null,
            long? length = null,
            Func<long> getPosition = null,
            Action<long> setPosition = null,
            Stream backingStream = null,
            Action dispose = null)
        {
            _canRead = canRead;
            _canSeek = canSeek;
            _canWrite = canWrite;
            _readFunc = readFunc;
            _length = length;
            _getPosition = getPosition;
            _setPosition = setPosition;
            _backingStream = backingStream;
            _dispose = dispose;
        }

        public override bool CanRead => _canRead ?? _backingStream?.CanRead ?? false;

        public override bool CanSeek => _canSeek ?? _backingStream?.CanSeek ?? false;

        public override bool CanWrite => _canWrite ?? _backingStream?.CanWrite ?? false;

        public override long Length => _length ?? _backingStream?.Length ?? 0;

        public override long Position
        {
            get
            {
                if (!CanSeek)
                    throw new NotSupportedException();
                if (_getPosition != null)
                {
                    return _getPosition();
                }
                if (_backingStream != null)
                {
                    return _backingStream.Position;
                }
                throw new NotImplementedException();
            }

            set
            {
                if (!CanSeek)
                {
                    throw new NotSupportedException();
                }
                if (_setPosition != null)
                {
                    _setPosition(value);
                }
                else if (_backingStream != null)
                {
                    _backingStream.Position = value;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        public override void Flush()
        {
            if (_backingStream == null)
            {
                throw new NotSupportedException();
            }
            _backingStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_readFunc != null)
            {
                return _readFunc(buffer, offset, count);
            }
            else if (_backingStream != null)
            {
                return _backingStream.Read(buffer, offset, count);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!CanSeek)
            {
                throw new NotSupportedException();
            }

            return 0;
        }

        public override void SetLength(long value)
        {
            if (_backingStream == null)
            {
                throw new NotSupportedException();
            }
            _backingStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
            {
                throw new NotSupportedException();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_dispose != null)
            {
                _dispose();
            }
            else
            {
                _backingStream?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
