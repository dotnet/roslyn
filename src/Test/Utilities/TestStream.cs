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

        public override bool CanRead => _canRead;

        public override bool CanSeek => _canSeek;

        public override bool CanWrite => _canWrite;

        public override long Length => 0L;

        public override long Position
        {
            get
            {
                return 0L;
            }

            set
            {
                if (!CanSeek)
                {
                    throw new NotSupportedException();
                }
            }
        }

        public override void Flush()
        {
        }

        public override int ReadByte()
        {
            if (!CanRead)
            {
                throw new NotSupportedException();
            }

            return -1;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!CanRead)
            {
                throw new NotSupportedException();
            }

            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!CanSeek)
            {
                throw new NotSupportedException();
            }

            return 0L;
        }

        public override void SetLength(long value)
        {
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
