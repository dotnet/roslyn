// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#pragma warning disable RS0042 // Do not copy value

using System;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.Remote
{
    internal readonly partial struct RemoteCallback<T>
        where T : class
    {
        // Copied from: https://github.com/dotnet/aspnetcore/blob/release/2.1/src/Servers/Kestrel/Transport.Abstractions/src/Internal/MemoryPoolSlab.cs

        /// <summary>
        /// Slab tracking object used by the byte buffer memory pool. A slab is a large allocation which is divided into smaller blocks. The
        /// individual blocks are then treated as independant array segments.
        /// </summary>
        private sealed class MemoryPoolSlab
        {
            /// <summary>
            /// This handle pins the managed array in memory until the slab is disposed. This prevents it from being
            /// relocated and enables any subsections of the array to be used as native memory pointers to P/Invoked API calls.
            /// </summary>
            private readonly GCHandle _gcHandle;
            private readonly IntPtr _nativePointer;
            private byte[] _data;

            private bool _isActive;
            private bool _disposedValue;

            public MemoryPoolSlab(byte[] data)
            {
                _data = data;
                _gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
                _nativePointer = _gcHandle.AddrOfPinnedObject();
                _isActive = true;
            }

            /// <summary>
            /// True as long as the blocks from this slab are to be considered returnable to the pool. In order to shrink the
            /// memory pool size an entire slab must be removed. That is done by (1) setting IsActive to false and removing the
            /// slab from the pool's _slabs collection, (2) as each block currently in use is Return()ed to the pool it will
            /// be allowed to be garbage collected rather than re-pooled, and (3) when all block tracking objects are garbage
            /// collected and the slab is no longer references the slab will be garbage collected and the memory unpinned will
            /// be unpinned by the slab's Dispose.
            /// </summary>
            public bool IsActive => _isActive;

            public IntPtr NativePointer => _nativePointer;

            public byte[] Array => _data;

            public int Length => _data.Length;

            public static MemoryPoolSlab Create(int length)
            {
                // allocate and pin requested memory length
                var array = new byte[length];

                // allocate and return slab tracking object
                return new MemoryPoolSlab(array);
            }

            private void DisposeImpl()
            {
                if (!_disposedValue)
                {
                    _isActive = false;

                    if (_gcHandle.IsAllocated)
                    {
                        _gcHandle.Free();
                    }

                    // set large fields to null.
                    _data = null;

                    _disposedValue = true;
                }
            }

            // override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
            ~MemoryPoolSlab()
            {
                DisposeImpl();
            }

            // This code added to correctly implement the disposable pattern.
            public void Dispose()
            {
                DisposeImpl();
                GC.SuppressFinalize(this);
            }
        }
    }
}
