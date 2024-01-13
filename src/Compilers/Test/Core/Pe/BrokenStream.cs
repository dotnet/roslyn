// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
            CancelOnWrite
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
            else if (BreakHow == BreakHowType.CancelOnWrite)
            {
                ThrownException = new OperationCanceledException();
                throw ThrownException;
            }
        }
    }
}
