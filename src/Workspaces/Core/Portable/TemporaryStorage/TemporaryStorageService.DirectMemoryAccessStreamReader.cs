// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host;

internal sealed partial class TemporaryStorageService
{
    private sealed unsafe class DirectMemoryAccessStreamReader : TextReaderWithLength
    {
        private char* _position;
        private readonly char* _end;

        public DirectMemoryAccessStreamReader(char* src, int length)
            : base(length)
        {
            RoslynDebug.Assert(src != null);
            RoslynDebug.Assert(length >= 0);

            _position = src;
            _end = _position + length;
        }

        public override int Peek()
        {
            if (_position >= _end)
            {
                return -1;
            }

            return *_position;
        }

        public override int Read()
        {
            if (_position >= _end)
            {
                return -1;
            }

            return *_position++;
        }

        public override int Read(char[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (index < 0 || index >= buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (count < 0 || (index + count) > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            count = Math.Min(count, (int)(_end - _position));
            if (count > 0)
            {
                Marshal.Copy((IntPtr)_position, buffer, index, count);
                _position += count;
            }

            return count;
        }
    }
}
