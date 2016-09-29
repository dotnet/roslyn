// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Microsoft.CodeAnalysis.Text
{
    /// <summary>
    /// A read-only, non-seekable <see cref="Stream"/> over a <see cref="SourceText"/>.
    /// </summary>
    internal sealed class SourceTextStream : Stream
    {
        private readonly SourceText _source;
        private readonly Encoding _encoding;
        private readonly Encoder _encoder;

        private readonly int _minimumTargetBufferCount;
        private int _position;
        private int _sourceOffset;
        private readonly char[] _charBuffer;
        private int _bufferOffset;
        private int _bufferUnreadChars;
        private bool _preambleWritten;

        private static readonly Encoding s_utf8EncodingWithNoBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

        public SourceTextStream(SourceText source, int bufferSize = 2048, bool useDefaultEncodingIfNull = false)
        {
            Debug.Assert(source.Encoding != null || useDefaultEncodingIfNull);

            _source = source;
            _encoding = source.Encoding ?? s_utf8EncodingWithNoBOM;
            _encoder = _encoding.GetEncoder();
            _minimumTargetBufferCount = _encoding.GetMaxByteCount(charCount: 1);
            _sourceOffset = 0;
            _position = 0;
            _charBuffer = new char[Math.Min(bufferSize, _source.Length)];
            _bufferOffset = 0;
            _bufferUnreadChars = 0;
            _preambleWritten = false;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { return _position; }
            set { throw new NotSupportedException(); }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count < _minimumTargetBufferCount)
            {
                // The buffer must be able to hold at least one character from the 
                // SourceText stream.  Returning 0 for that case isn't correct because
                // that indicates end of stream vs. insufficient buffer. 
                throw new ArgumentException(nameof(count));
            }

            int originalCount = count;

            if (!_preambleWritten)
            {
                int bytesWritten = WritePreamble(buffer, offset, count);
                offset += bytesWritten;
                count -= bytesWritten;
            }

            while (count >= _minimumTargetBufferCount && _position < _source.Length)
            {
                if (_bufferUnreadChars == 0)
                {
                    FillBuffer();
                }

                int charsUsed, bytesUsed;
                bool ignored;
                _encoder.Convert(_charBuffer, _bufferOffset, _bufferUnreadChars, buffer, offset, count, flush: false, charsUsed: out charsUsed, bytesUsed: out bytesUsed, completed: out ignored);
                _position += charsUsed;
                _bufferOffset += charsUsed;
                _bufferUnreadChars -= charsUsed;
                offset += bytesUsed;
                count -= bytesUsed;
            }

            // Return value is the number of bytes read
            return originalCount - count;
        }

        private int WritePreamble(byte[] buffer, int offset, int count)
        {
            _preambleWritten = true;
            byte[] preambleBytes = _encoding.GetPreamble();
            if (preambleBytes == null)
            {
                return 0;
            }

            int length = Math.Min(count, preambleBytes.Length);
            Array.Copy(preambleBytes, 0, buffer, offset, length);
            return length;
        }

        private void FillBuffer()
        {
            int charsToRead = Math.Min(_charBuffer.Length, _source.Length - _sourceOffset);
            _source.CopyTo(_sourceOffset, _charBuffer, 0, charsToRead);
            _sourceOffset += charsToRead;
            _bufferOffset = 0;
            _bufferUnreadChars = charsToRead;
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
            throw new NotSupportedException();
        }
    }
}
