// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a memory block backed by an array of bytes.
    /// </summary>
    internal sealed class ByteArrayMemoryBlock : AbstractMemoryBlock
    {
        private ByteArrayMemoryProvider provider;
        private readonly int start;
        private readonly int size;

        internal ByteArrayMemoryBlock(ByteArrayMemoryProvider provider, int start, int size)
        {
            this.provider = provider;
            this.size = size;
            this.start = start;
        }

        protected override void Dispose(bool disposing)
        {
            provider = null;
        }

        public override IntPtr Pointer
        {
            get
            {
                return provider.Pointer + start;
            }
        }

        public override int Size
        {
            get
            {
                return size;
            }
        }

        public override ImmutableArray<byte> GetContent()
        {
            return ImmutableArray.Create(provider.array, start, size);
        }
    }
}