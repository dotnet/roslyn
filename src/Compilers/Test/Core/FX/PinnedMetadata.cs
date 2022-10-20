// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Roslyn.Test.Utilities
{
    internal sealed class PinnedMetadata : PinnedBlob, IDisposable
    {
        public MetadataReader Reader;

        public unsafe PinnedMetadata(ImmutableArray<byte> metadata)
            : base(metadata)
        {
            this.Reader = new MetadataReader((byte*)Pointer, this.Size, MetadataReaderOptions.None, null);
        }

        public override void Dispose()
        {
            base.Dispose();
            Reader = null;
        }
    }
}
