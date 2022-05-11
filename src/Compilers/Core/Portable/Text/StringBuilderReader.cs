// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Collections.Internal;

namespace Microsoft.CodeAnalysis.Text
{
    internal sealed class StringBuilderReader : TextReader
    {
        private readonly StringBuilder _stringBuilder;
        private int _position;

        public StringBuilderReader(StringBuilder stringBuilder)
        {
            _stringBuilder = stringBuilder;
            _position = 0;
        }

        public override int Peek()
        {
            if (_position == _stringBuilder.Length)
                return -1;

            return _stringBuilder[_position];
        }

        public override int Read()
        {
            if (_position == _stringBuilder.Length)
                return -1;

            return _stringBuilder[_position++];
        }

        public override int Read(char[] buffer, int index, int count)
        {
            var length = Math.Min(count, _stringBuilder.Length - _position);
            _stringBuilder.CopyTo(_position, buffer, index, length);
            _position += length;
            return length;
        }

        public override int ReadBlock(char[] buffer, int index, int count) =>
            Read(buffer, index, count);

#if NETCOREAPP
        public override int Read(Span<char> buffer)
        {
            var length = Math.Min(buffer.Length, _stringBuilder.Length - _position);
            _stringBuilder.CopyTo(_position, buffer, length);
            _position += length;
            return length;
        }

        public override int ReadBlock(Span<char> buffer) =>
            Read(buffer);
#endif

        public override string ReadToEnd()
        {
            var result = _position == 0
                ? _stringBuilder.ToString()
                : _stringBuilder.ToString(_position, _stringBuilder.Length - _position);

            _position = _stringBuilder.Length;
            return result;
        }
    }
}
