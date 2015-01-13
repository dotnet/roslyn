// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;

namespace Microsoft.CodeAnalysis.Collections
{
    internal sealed class ImmutableMemoryStream : Stream
    {
        private readonly ImmutableArray<byte> array;
        private int position;

        internal ImmutableMemoryStream(ImmutableArray<byte> array)
        {
            Debug.Assert(!array.IsDefault);
            this.array = array;
        }

        public ImmutableArray<byte> GetBuffer()
        {
            return array;
        }

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
            get { return false; }
        }

        public override long Length
        {
            get { return array.Length; }
        }

        public override long Position
        {
            get
            {
                return position;
            }
            set
            {
                if (value < 0 || value >= array.Length)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                position = (int)value;
            }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int result = Math.Min(count, array.Length - position);
            array.CopyTo(position, buffer, offset, result);
            position += result;
            return result;
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
                        target = checked(offset + array.Length);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException("origin");
                }
            }
            catch (OverflowException)
            {
                throw new ArgumentOutOfRangeException("offset");
            }

            if (target < 0 || target >= array.Length)
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
