// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    /// <summary>
    /// Used when capturing output from a running test to prevent runaway
    /// output from allocating extreme amounts of memory.
    /// </summary>
    public sealed class CappedStringWriter : StringWriter
    {
        private readonly int _expectedLength;
        private int _remaining;

        public int Length => GetStringBuilder().Length;

        public CappedStringWriter(int expectedLength)
        {
            if (expectedLength < 0)
            {
                _expectedLength = _remaining = 1024 * 1024;
            }
            else
            {
                _expectedLength = expectedLength;
                _remaining = Math.Max(256, expectedLength * 4);
            }
        }

        private void CapReached()
        {
            throw new Exception($"Test produced more output than expected ({_expectedLength} characters). Is it in an infinite loop? Output so far:\r\n{GetStringBuilder()}");
        }

        public override void Write(char value)
        {
            if (1 <= _remaining)
            {
                _remaining--;
                base.Write(value);
            }
            else
            {
                CapReached();
            }
        }

        public override void Write(char[] buffer, int index, int count)
        {
            if (count <= _remaining)
            {
                _remaining -= count;
                base.Write(buffer, index, count);
            }
            else
            {
                CapReached();
            }
        }

        public override void Write(string value)
        {
            if (value.Length <= _remaining)
            {
                _remaining -= value.Length;
                base.Write(value);
            }
            else
            {
                CapReached();
            }
        }
    }
}
