// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Class representing raw memory but not owning the memory.
    /// </summary>
    internal unsafe sealed class ExternalMemoryBlock : AbstractMemoryBlock
    {
        // keeps the owner of the memory alive as long as the block is alive:
        private readonly object memoryOwner;

        private IntPtr buffer;
        private int size;

        public ExternalMemoryBlock(object memoryOwner, IntPtr buffer, int size)
        {
            this.memoryOwner = memoryOwner;
            this.buffer = buffer;
            this.size = size;
        }

        protected override void Dispose(bool disposing)
        {
            this.buffer = IntPtr.Zero;
            this.size = 0;
        }

        public override IntPtr Pointer
        {
            get { return this.buffer; }
        }

        public override int Size
        {
            get { return this.size; }
        }

        public override ImmutableArray<byte> GetContent()
        {
            // TODO (tomat): use ImmutableArray.Create(IntPtr)
            byte[] bytes = new byte[this.size];
            Marshal.Copy(this.buffer, bytes, 0, bytes.Length);
            return bytes.AsImmutable();
        }
    }
}