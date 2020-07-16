// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Roslyn.Test.Utilities
{
    internal class PinnedBlob : IDisposable
    {
        private GCHandle _bytes; // non-readonly as Free() mutates to prevent double-free.
        public readonly IntPtr Pointer;
        public readonly int Size;

        public PinnedBlob(ImmutableArray<byte> blob)
            : this(blob.DangerousGetUnderlyingArray())
        {
        }

        public unsafe PinnedBlob(byte[] blob)
        {
            _bytes = GCHandle.Alloc(blob, GCHandleType.Pinned);
            Pointer = _bytes.AddrOfPinnedObject();
            Size = blob.Length;
        }

        public void Dispose()
        {
            _bytes.Free();
        }
    }
}
