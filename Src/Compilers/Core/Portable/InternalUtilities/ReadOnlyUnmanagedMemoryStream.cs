// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis
{
    internal sealed class ReadOnlyUnmanagedMemoryStream : Stream
    {
        private readonly object memoryOwner;
        private readonly IntPtr data;
        private readonly int length;
        private int position;

        public ReadOnlyUnmanagedMemoryStream(object memoryOwner, IntPtr data, int length)
        {
            this.memoryOwner = memoryOwner;
            this.data = data;
            this.length = length;
        }

        public unsafe override int ReadByte()
        {
            if (position == length)
            {
                return -1;
            }

            return ((byte*)data)[position++];
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = Math.Min(count, length - position);
            Marshal.Copy(this.data + this.position, buffer, offset, bytesRead);
            this.position += bytesRead;
            return bytesRead;
        }

        public override void Flush()
        {
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override long Length
        {
            get
            {
                return length;
            }
        }

        public override long Position
        {
            get
            {
                return position;
            }

            set
            {
                Seek(value, SeekOrigin.Begin);
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long target;
            try
            {
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        target = offset;
                        break;

                    case SeekOrigin.Current:
                        target = checked(offset + position);
                        break;

                    case SeekOrigin.End:
                        target = checked(offset + length);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException("origin");
                }
            }
            catch (OverflowException)
            {
                throw new ArgumentOutOfRangeException("offset");
            }

            if (target < 0 || target >= length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }

            position = (int)target;
            return target;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}