// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents memory block allocated on native heap.
    /// </summary>
    /// <remarks>
    /// Owns the native memory resource.
    /// </remarks>
    internal sealed class NativeHeapMemoryBlock : AbstractMemoryBlock
    {
        private IntPtr pointer;
        private readonly int size;

        internal NativeHeapMemoryBlock(int size)
        {
            this.pointer = Marshal.AllocHGlobal(size);
            this.size = size;
        }

        ~NativeHeapMemoryBlock()
        {
            Dispose(disposing: false);
        }

        public override IntPtr Pointer
        {
            get { return pointer; }
        }

        public override int Size
        {
            get { return size; }
        }

        public override ImmutableArray<byte> GetContent()
        {
            // TODO (tomat): use ImmutableArray.Create(IntPtr)
            byte[] bytes = new byte[this.size];
            Marshal.Copy(this.pointer, bytes, 0, bytes.Length);
            GC.KeepAlive(this);
            return bytes.AsImmutable();
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
            {
                GC.SuppressFinalize(this);
            }

            Marshal.FreeHGlobal(Interlocked.Exchange(ref pointer, IntPtr.Zero));
        }
    }
}
