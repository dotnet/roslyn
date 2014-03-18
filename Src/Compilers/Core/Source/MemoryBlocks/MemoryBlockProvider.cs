// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.CodeAnalysis
{
    internal abstract class MemoryBlockProvider : IDisposable
    {
        /// <summary>
        /// Creates and hydrates a memory block representing all data.
        /// </summary>
        /// <exception cref="IOException">Error while reading from the memory source.</exception>
        public AbstractMemoryBlock GetMemoryBlock()
        {
            return GetMemoryBlockImpl(0, Size);
        }

        /// <summary>
        /// Creates and hydrates a memory block representing data in the specified range.
        /// </summary>
        /// <param name="start">Starting offset relative to the beginning of the data represented by this provider.</param>
        /// <param name="size">Size of the resulting block.</param>
        /// <exception cref="IOException">Error while reading from the memory source.</exception>
        public AbstractMemoryBlock GetMemoryBlock(int start, int size)
        {
            Debug.Assert(start >= 0 && size >= 0 && start < this.Size && start + size <= this.Size);
            return GetMemoryBlockImpl(start, size);
        }

        protected abstract AbstractMemoryBlock GetMemoryBlockImpl(int start, int size);

        /// <summary>
        /// The size of the data.
        /// </summary>
        public abstract int Size { get; }

        protected abstract void Dispose(bool disposing);

        public void Dispose()
        {
            Dispose(disposing: true);
        }
    }
}
