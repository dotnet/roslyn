// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;

namespace Roslyn.Test.Utilities
{
    public class TestStream : Stream
    {
        private readonly bool _canRead, _canSeek, _canWrite;
        private readonly Func<byte[], int, int, int> _readFunc;
        private readonly long _length;
        private readonly Func<long> _getPosition;
        private readonly Action<long> _setPosition;

        public TestStream(
            bool canRead = false,
            bool canSeek = false,
            bool canWrite = false,
            Func<byte[], int, int, int> readFunc = null,
            long length = 0,
            Func<long> getPosition = null,
            Action<long> setPosition = null)
        {
            _canRead = canRead;
            _canSeek = canSeek;
            _canWrite = canWrite;
            _readFunc = readFunc != null
                ? readFunc
                : (_1, _2, _3) => { throw new NotImplementedException(); };
            _length = length;
            _getPosition = getPosition != null
                ? getPosition
                : () => { throw new NotImplementedException(); };
            _setPosition = setPosition != null
                ? setPosition
                : (_1) => { throw new NotImplementedException(); };
        }

        public override bool CanRead => _canRead;

        public override bool CanSeek => _canSeek;

        public override bool CanWrite => _canWrite;

        public override long Length => _length;

        public override long Position
        {
            get { return _getPosition(); }
            set { _setPosition(value); }
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => _readFunc(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
            {
                throw new NotSupportedException();
            }
        }
    }
}
