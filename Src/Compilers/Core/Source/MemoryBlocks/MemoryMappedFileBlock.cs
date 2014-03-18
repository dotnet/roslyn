// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    internal sealed class MemoryMappedFileBlock : AbstractMemoryBlock
    {
        private MemoryMappedViewAccessor accessor;
        private IntPtr pointer;

        internal unsafe MemoryMappedFileBlock(MemoryMappedViewAccessor accessor)
        {
            byte* ptr = null;
            accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
            ptr += GetPrivateOffset(accessor);

            this.accessor = accessor;
            this.pointer = (IntPtr)ptr;
        }

        ~MemoryMappedFileBlock()
        {
            Dispose(disposing: false);
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
            {
                GC.SuppressFinalize(this);
            }

            var accessor = Interlocked.Exchange(ref this.accessor, null);
            if (accessor != null)
            {
                accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                accessor.Dispose();                
            }

            pointer = IntPtr.Zero;
        }

        public override IntPtr Pointer
        {
            get { return this.pointer; }
        }

        public override int Size
        {
            get { return (int)accessor.Capacity; }
        }

        public override ImmutableArray<byte> GetContent()
        {
            byte[] bytes = new byte[Size];
            Marshal.Copy(this.pointer, bytes, 0, bytes.Length);
            GC.KeepAlive(this);
            return bytes.AsImmutable();
        }

        // This is a Reflection hack to workaround clr bug 6441.  
        //
        // MemoryMappedViewAccessor.SafeMemoryMappedViewHandle.AcquirePointer returns a pointer
        // that has been aligned to SYSTEM_INFO.dwAllocationGranularity.  Unfortunately the 
        // offset from this pointer to our requested offset into the MemoryMappedFile is only
        // available through the UnmanagedMemoryStream._offset field which is not exposed publicly.
        //
        private long GetPrivateOffset(MemoryMappedViewAccessor stream)
        {
            System.Reflection.FieldInfo unmanagedMemoryStreamOffset = typeof(MemoryMappedViewAccessor).GetField("m_view", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.GetField);
            object memoryMappedView = unmanagedMemoryStreamOffset.GetValue(stream);
            System.Reflection.PropertyInfo memoryMappedViewPointerOffset = memoryMappedView.GetType().GetProperty("PointerOffset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.GetProperty);
            return (long)memoryMappedViewPointerOffset.GetValue(memoryMappedView);
        }
    }
}