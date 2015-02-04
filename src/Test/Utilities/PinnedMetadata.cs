// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace Roslyn.Test.Utilities
{
    internal class PinnedMetadata : IDisposable
    {
        private GCHandle bytes; // non-readonly as Free() mutates to prevent double-free.
        public readonly MetadataReader Reader;
        public readonly IntPtr Pointer;
        public readonly int Size;

        public unsafe PinnedMetadata(ImmutableArray<byte> metadata)
        {
            this.bytes = GCHandle.Alloc(metadata.DangerousGetUnderlyingArray(), GCHandleType.Pinned);
            this.Pointer = this.bytes.AddrOfPinnedObject();
            this.Size = metadata.Length;
            this.Reader = new MetadataReader((byte*)this.Pointer, this.Size, MetadataReaderOptions.None, null);
        }

        public void Dispose()
        {
            bytes.Free();
        }
    }
}
