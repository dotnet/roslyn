// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.Razor.Logging;

internal partial class AbstractMemoryLoggerProvider
{
    /// <summary>
    /// A circular in memory buffer to store logs in memory.
    /// </summary>
    private class Buffer(int bufferSize)
    {
        private string[] _memory = new string[bufferSize];

        // Start at -1 because append always increments, so we want to start at value 0
        private int _head = -1;

        public void Append(string s)
        {
            var position = Math.Abs(Interlocked.Increment(ref _head) % _memory.Length);
            _memory[position] = s;
        }
    }
}
