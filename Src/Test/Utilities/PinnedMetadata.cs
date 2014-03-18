// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace Roslyn.Test.Utilities
{
    internal class PinnedMetadata : IDisposable
    {
        private readonly PinnedImmutableArray bytes;
        public readonly MetadataReader Reader;

        public PinnedMetadata(ImmutableArray<byte> metadata)
        {
            bytes = PinnedImmutableArray.Create(metadata);
            this.Reader = new MetadataReader(bytes.Pointer, metadata.Length, MetadataReaderOptions.None, null);
        }

        public void Dispose()
        {
            bytes.Dispose();
        }
    }
}
