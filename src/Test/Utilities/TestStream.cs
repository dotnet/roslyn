// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;

namespace Roslyn.Test.Utilities
{
    public class TestStream : Stream
    {
        private readonly bool _canRead, _canSeek, _canWrite;

        public TestStream(bool canRead = false, bool canSeek = false, bool canWrite = false)
        {
            _canRead = canRead;
            _canSeek = canSeek;
            _canWrite = canWrite;
        }

        public override bool CanRead
        {
            get
            {
                return _canRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return _canSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return _canWrite;
            }
        }

        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

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
