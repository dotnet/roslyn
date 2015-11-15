// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DiaSymReader.PortablePdb
{
    internal struct BlobWriter
    {
        private byte[] _buffer;
        private int _position;

        public BlobWriter(int initialCapacity = 16)
        {
            _buffer = new byte[initialCapacity];
            _position = 0;
        }

        private void EnsureCapacity(int size)
        {
            if (_position + size > _buffer.Length)
            {
                Array.Resize(ref _buffer, Math.Min(_position + size, _buffer.Length * 2 + 1));
            }
        }

        public void Write(byte value)
        {
            EnsureCapacity(1);
            _buffer[_position] = value;
            _position++;
        }

        public void Write(byte b1, byte b2)
        {
            EnsureCapacity(2);
            _buffer[_position] = b1;
            _buffer[_position + 1] = b2;
            _position += 2;
        }

        public void Write(byte b1, byte b2, byte b3, byte b4)
        {
            EnsureCapacity(4);
            _buffer[_position] = b1;
            _buffer[_position + 1] = b2;
            _buffer[_position + 2] = b3;
            _buffer[_position + 3] = b4;
            _position += 4;
        }

        internal void Write(byte[] buffer)
        {
            Write(buffer, 0, buffer.Length);
        }

        internal void Write(byte[] buffer, int index, int length)
        {
            EnsureCapacity(length);
            Buffer.BlockCopy(buffer, index, _buffer, _position, length);
            _position += length;
        }

        public void WriteCompressedInteger(int value)
        {
            unchecked
            {
                if (value <= 0x7f)
                {
                    Write((byte)value);
                }
                else if (value <= 0x3fff)
                {
                    Write((byte)(0x80 | (value >> 8)), (byte)value);
                }
                else
                {
                    Debug.Assert(value <= 0x1fffffff);

                    Write(
                        (byte)(0xc0 | (value >> 24)),
                        (byte)(value >> 16),
                        (byte)(value >> 8),
                        (byte)value);
                }
            }
        }

        public byte[] ToArray()
        {
            var buffer = _buffer;
            Array.Resize(ref buffer, _position);

            _buffer = null;
            _position = -1;
            return buffer;
        }
    }
}
