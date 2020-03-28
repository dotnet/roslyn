// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Roslyn.Test.Utilities
{
    internal class PinnedMetadata : IDisposable
    {
        private GCHandle _bytes; // non-readonly as Free() mutates to prevent double-free.
        public readonly MetadataReader Reader;
        public readonly IntPtr Pointer;
        public readonly int Size;

        public unsafe PinnedMetadata(ImmutableArray<byte> metadata)
        {
            _bytes = GCHandle.Alloc(metadata.DangerousGetUnderlyingArray(), GCHandleType.Pinned);
            this.Pointer = _bytes.AddrOfPinnedObject();
            this.Size = metadata.Length;
            this.Reader = new MetadataReader((byte*)this.Pointer, this.Size, MetadataReaderOptions.None, null);
        }

        public void Dispose()
        {
            _bytes.Free();
        }
    }
}
