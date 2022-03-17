// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Buffers;
using System.Collections.Immutable;

namespace Roslyn.Test.Utilities
{
    internal class PinnedBlob : IDisposable
    {
        // non-readonly as Dispose() mutates
        private MemoryHandle _handle;
        public IntPtr Pointer;
        public int Size;

        public PinnedBlob(ImmutableArray<byte> blob)
            : this(blob.AsMemory())
        { }

        public PinnedBlob(byte[] blob)
            : this(blob.AsMemory())
        { }

        public unsafe PinnedBlob(ReadOnlyMemory<byte> blob)
        {
            _handle = blob.Pin();
            this.Size = blob.Length;
            this.Pointer = (IntPtr)_handle.Pointer;
        }

        public virtual void Dispose()
        {
            _handle.Dispose();
            Pointer = IntPtr.Zero;
            Size = 0;
        }
    }
}
