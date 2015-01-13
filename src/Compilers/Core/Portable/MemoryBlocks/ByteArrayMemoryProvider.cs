// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    internal sealed class ByteArrayMemoryProvider : MemoryBlockProvider
    {
        internal readonly ImmutableArray<byte> array;
        private PinnedImmutableArray pinned;

        public ByteArrayMemoryProvider(ImmutableArray<byte> array)
        {
            this.array = array;
        }

        ~ByteArrayMemoryProvider()
        {
            Dispose(disposing: false);
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
            {
                GC.SuppressFinalize(this);
            }

            // TODO (bug 829217): PinnedImmutableArray.Dispose is not thread safe and might throw on double disposal. 
            var localPinned = Interlocked.Exchange(ref pinned, null);
            if (localPinned != null)
            {
                localPinned.Dispose();
            }
        }

        public override int Size
        {
            get
            {
                return array.Length;
            }
        }

        protected override AbstractMemoryBlock GetMemoryBlockImpl(int start, int size)
        {
            return new ByteArrayMemoryBlock(this, start, size);
        }

        internal IntPtr Pointer
        {
            get
            {
                if (pinned == null)
                {
                    var newPinned = PinnedImmutableArray.Create(array);
                    if (Interlocked.CompareExchange(ref pinned, newPinned, null) != null)
                    {
                        // another thread has already allocated the handle:
                        newPinned.Dispose();
                    }
                }

                return pinned.Pointer;
            }
        }
    }
}
