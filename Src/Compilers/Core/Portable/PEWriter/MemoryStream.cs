// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.Cci
{
    internal sealed class MemoryStream
    {
        internal byte[] Buffer;
        internal uint Length;
        private uint position;

        internal MemoryStream()
        {
            this.Buffer = new byte[64];
        }

        internal MemoryStream(uint initialSize)
        {
            this.Buffer = new byte[initialSize];
        }

        internal MemoryStream(ObjectPool<MemoryStream> pool)
            : this()
        {
            this.pool = pool;
        }

        // Grows to at least m
        private void Grow(uint m)
        {
            var n2 = Math.Min(this.Length, uint.MaxValue / 2) * 2;
            n2 = Math.Max(n2, m);
            var newBuffer = new byte[n2];

            Array.Copy(this.Buffer, 0, newBuffer, 0, (int)this.Length);
            this.Buffer = newBuffer;
        }

        private uint Capacity
        {
            get
            {
                return (uint)Buffer.Length;
            }
        }

        internal uint Position
        {
            get
            {
                return this.position;
            }

            set
            {
                if (value > this.Capacity)
                {
                    this.Grow(value);
                }

                this.Length = Math.Max(this.Length, value);
                this.position = value;
            }
        }

        internal byte[] ToArray()
        {
            uint n = this.Length;
            byte[] source = this.Buffer;

            byte[] result = new byte[n];
            for (int i = 0; i < n; i++)
            {
                result[i] = source[i];
            }

            return result;
        }

        internal void Write(byte[] buffer, int index, int length)
        {
            int position = (int)this.position;

            // resize, if needed
            this.Position += (uint)length;

            System.Buffer.BlockCopy(buffer, index, this.Buffer, position, length);
        }

        internal void Write(ImmutableArray<byte> buffer, int index, int length)
        {
            int position = (int)this.position;

            // resize, if needed
            this.Position += (uint)length;

            buffer.CopyTo(index, this.Buffer, position, length);
        }

        internal void WriteTo(MemoryStream stream)
        {
            stream.Write(this.Buffer, 0, (int)this.Length);
        }

        internal void WriteTo(System.IO.Stream stream)
        {
            stream.Write(this.Buffer, 0, (int)this.Length);
        }

        // Reset to zero-length, but don't reduce or free the array.
        internal void Clear()
        {
            this.position = 0;
            this.Length = 0;
        }

        #region Poolable

        private readonly ObjectPool<MemoryStream> pool;

        //
        // To implement Poolable, you need two things:
        // 1) Expose Freeing primitive. 
        public void Free()
        {
            // Note that poolables are not finalizable. If one gets collected - no big deal.
            this.Clear();
            if (pool != null)
            {
                pool.Free(this);
            }
        }

        //2) Expose  the way to get an instance.
        private static readonly ObjectPool<MemoryStream> PoolInstance = CreatePool();

        public static MemoryStream GetInstance()
        {
            var stream = PoolInstance.Allocate();
            return stream;
        }

        public static ObjectPool<MemoryStream> CreatePool()
        {
            return CreatePool(128);
        }

        public static ObjectPool<MemoryStream> CreatePool(int size)
        {
            ObjectPool<MemoryStream> pool = null;
            pool = new ObjectPool<MemoryStream>(() => new MemoryStream(pool), size);
            return pool;
        }

        #endregion

    }
}