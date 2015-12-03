// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    internal class LargeTextWriter : SourceTextWriter
    {
        private readonly ArrayBuilder<char[]> _chunks;

        private int _currentUsed;
        private char[] _buffer;
        private int _currentPosition;

        private Encoding _encoding;
        private SourceHashAlgorithm _checksumAlgorithm;

        public LargeTextWriter(Encoding encoding, SourceHashAlgorithm checksumAlgorithm, int capacity)
        {
            _encoding = encoding;
            _checksumAlgorithm = checksumAlgorithm;

            _chunks = ArrayBuilder<char[]>.GetInstance(1 + capacity / LargeEncodedText.ChunkSize);

            var bufferSize = Math.Min(LargeEncodedText.ChunkSize, capacity);
            _buffer = new char[bufferSize];
        }

        public override SourceText ToSourceText()
        {
            this.Flush();
            return new LargeEncodedText(_chunks.ToImmutableAndFree(), _encoding, default(ImmutableArray<byte>), _checksumAlgorithm);
        }

        public override Encoding Encoding
        {
            get { return _encoding; }
        }

        public override void Write(char value)
        {
            if (_buffer != null && _currentUsed < _buffer.Length)
            {
                _buffer[_currentUsed] = value;
                _currentUsed++;
            }
            else
            {
                Write(new char[] { value }, 0, 1);
            }
        }

        public override void Write(string value)
        {
            if (value != null)
            {
                var count = value.Length;
                int index = 0;

                while (count > 0)
                {
                    var remaining = _buffer.Length - _currentUsed;
                    var copy = Math.Min(remaining, count);

                    value.CopyTo(index, _buffer, _currentUsed, copy);

                    _currentUsed += copy;
                    index += copy;
                    count -= copy;

                    if (_currentUsed == _buffer.Length)
                    {
                        Flush();
                    }
                }
            }
        }

        public override void Write(char[] buffer, int index, int count)
        {
            if (index < 0 || index >= buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (count < 0 || count > buffer.Length - index)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            while (count > 0)
            {
                var remaining = _buffer.Length - _currentUsed;
                var copy = Math.Min(remaining, count);

                Array.Copy(buffer, index, _buffer, _currentUsed, copy);
                _currentUsed += copy;
                index += copy;
                count -= copy;

                if (_currentUsed == _buffer.Length)
                {
                    Flush();
                }
            }
        }

        /// <summary>
        /// Append chunk to writer (may reuse char array)
        /// </summary>
        internal void AppendChunk(char[] chunk)
        {
            var remaining = _buffer.Length - _currentUsed;
            if (chunk.Length < remaining)
            {
                this.Write(chunk, 0, chunk.Length);
            }
            else
            {
                this.Flush();
                _chunks.Add(chunk);
                _currentPosition += chunk.Length;
            }
        }

        public override void Flush()
        {
            if (_currentUsed > 0)
            {
                var text = new char[_currentUsed];
                Array.Copy(_buffer, text, _currentUsed);
                _chunks.Add(text);
                Array.Clear(_buffer, 0, _currentUsed);
                _currentPosition += _currentUsed;
                _currentUsed = 0;
            }
        }
    }
}