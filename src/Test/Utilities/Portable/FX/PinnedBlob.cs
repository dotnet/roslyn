// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Roslyn.Test.Utilities
{
    internal unsafe class PinnedBlob : IDisposable
    {
        private MemoryHandle _handle;

        public IntPtr Pointer => (IntPtr)_handle.Pointer;
        public readonly int Size;

        public PinnedBlob(ReadOnlyMemory<byte> blob)
        {
            _handle = blob.Pin();
            Size = blob.Length;
        }

        public PinnedBlob(ImmutableArray<byte> blob)
            : this(blob.AsMemory())
        { }

        public PinnedBlob(byte[] blob)
            : this(blob.AsMemory())
        { }

        public void Dispose()
        {
            _handle.Dispose();
        }
    }
}
