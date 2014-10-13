// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        public unsafe PinnedMetadata(ImmutableArray<byte> metadata)
        {
            bytes = GCHandle.Alloc(ImmutableArrayInterop.DangerousGetUnderlyingArray(metadata), GCHandleType.Pinned);
            this.Reader = new MetadataReader((byte*)bytes.AddrOfPinnedObject(), metadata.Length, MetadataReaderOptions.None, null);
        }

        public void Dispose()
        {
            bytes.Free();
        }
    }
}
