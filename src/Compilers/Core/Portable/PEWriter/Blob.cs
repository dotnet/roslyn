// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Cci
{
    internal struct Blob
    {
        private readonly byte[] _buffer;
        private readonly int _start;
        private readonly int _length;

        internal Blob(byte[] buffer, int start, int length)
        {
            _buffer = buffer;
            _start = start;
            _length = length;
        }

        public int Length => _length;

        public ArraySegment<byte> GetUnderlyingBuffer()
        {
            return new ArraySegment<byte>(_buffer, _start, _length);
        }
    }
}
