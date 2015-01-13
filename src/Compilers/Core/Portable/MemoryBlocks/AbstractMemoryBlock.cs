// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a disposable blob of memory accessed via unsafe pointer.
    /// </summary>
    internal abstract class AbstractMemoryBlock : IDisposable
    {
        /// <summary>
        /// Pointer to the underlying data or <see cref="IntPtr.Zero"/> if the data have been disposed.
        /// </summary>
        public abstract IntPtr Pointer
        {
            get;
        }

        public bool IsDisposed
        {
            get { return Pointer == IntPtr.Zero; }
        }

        public abstract int Size
        {
            get;
        }

        /// <summary>
        /// Returns the content of the memory block. 
        /// </summary>
        /// <remarks>
        /// Only creates a copy of the data if they are not represented by a managed byte array.
        /// </remarks>
        public abstract ImmutableArray<byte> GetContent();

        protected abstract void Dispose(bool disposing);

        /// <summary>
        /// Disposes the block. The operation is thread-safe and idempotent.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
        }

    }
}
