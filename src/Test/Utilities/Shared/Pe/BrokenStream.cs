// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;

namespace Roslyn.Test.Utilities
{
    internal class BrokenStream : Stream
    {
        public enum BreakHowType
        {
            ThrowOnSetPosition,
            ThrowOnWrite,
            ThrowOnSetLength,
            ThrowOnWriteWithOperationCancelled,
        }

        public BreakHowType BreakHow;
        public Exception ThrownException { get; private set; }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void Flush()
        {
        }

        public override long Length
        {
            get
            {
                return 0;
            }
        }

        public override long Position
        {
            get
            {
                return 0;
            }
            set
            {
                if (BreakHow == BreakHowType.ThrowOnSetPosition)
                {
                    ThrownException = new NotSupportedException();
                    throw ThrownException;
                }
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return 0;
        }

        public override void SetLength(long value)
        {
            if (BreakHow == BreakHowType.ThrowOnSetLength)
            {
                ThrownException = new IOException();
                throw ThrownException;
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (BreakHow == BreakHowType.ThrowOnWrite)
            {
                ThrownException = new IOException();
                throw ThrownException;
            }
            else if (BreakHow == BreakHowType.ThrowOnWriteWithOperationCancelled)
            {
                ThrownException = new OperationCanceledException();
                throw new OperationCanceledException();
            }
        }
    }
}
