// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Roslyn.Test.Utilities
{
    internal unsafe class PinnedMetadata : IDisposable
    {
        private MemoryHandle _handle;

        public IntPtr Pointer => (IntPtr)_handle.Pointer;
        public readonly MetadataReader Reader;
        public readonly int Size;

        public unsafe PinnedMetadata(ImmutableArray<byte> metadata)
        {
            _handle = metadata.AsMemory().Pin();
            this.Size = metadata.Length;
            this.Reader = new MetadataReader((byte*)_handle.Pointer, Size, MetadataReaderOptions.None, null);
        }

        public void Dispose()
        {
            _handle.Dispose();
        }
    }
}
