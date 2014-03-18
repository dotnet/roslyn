// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents raw memory owned by an external object. 
    /// </summary>
    internal sealed class ExternalMemoryBlockProvider : MemoryBlockProvider
    {
        private readonly IntPtr memory;
        private readonly int size;

        public ExternalMemoryBlockProvider(IntPtr memory, int size)
        {
            this.memory = memory;
            this.size = size;
        }

        public override int Size
        {
            get
            {
                return size;
            }
        }

        protected override AbstractMemoryBlock GetMemoryBlockImpl(int start, int size)
        {
            return new ExternalMemoryBlock(this, memory + start, size);
        }

        protected override void Dispose(bool disposing)
        {
            // no-op, we don't own the memory
        }

        public IntPtr Pointer
        {
            get
            {
                return memory;
            }
        }
    }
}
