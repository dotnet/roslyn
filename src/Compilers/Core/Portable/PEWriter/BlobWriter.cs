// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.Cci
{
    internal sealed partial class BlobWriter
    {
        internal byte[] Buffer;
        internal uint Length;
        private uint _position;

        internal BlobWriter()
        {
            this.Buffer = new byte[64];
        }

        internal BlobWriter(uint initialSize)
        {
            this.Buffer = new byte[initialSize];
        }

        internal BlobWriter(ObjectPool<BlobWriter> pool)
            : this()
        {
            _pool = pool;
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
                return _position;
            }

            set
            {
                if (value > this.Capacity)
                {
                    this.Grow(value);
                }

                this.Length = Math.Max(this.Length, value);
                _position = value;
            }
        }

        internal byte[] ToArray()
        {
            if (this.Length == 0)
            {
                return SpecializedCollections.EmptyArray<byte>();
            }

            byte[] result = new byte[this.Length];
            Array.Copy(this.Buffer, result, result.Length);
            return result;
        }

        internal ImmutableArray<byte> ToImmutableArray()
        {
            return ImmutableArray.Create(this.Buffer, 0, (int)this.Length);
        }

        internal void Write(byte value, int count)
        {
            int position = (int)_position;

            // resize, if needed
            this.Position += (uint)count;

            for (int i = 0; i < count; i++)
            {
                this.Buffer[position + i] = value;
            }
        }

        internal void Write(byte[] buffer, int index, int length)
        {
            int position = (int)_position;

            // resize, if needed
            this.Position += (uint)length;

            System.Buffer.BlockCopy(buffer, index, this.Buffer, position, length);
        }

        internal void Write(ImmutableArray<byte> buffer, int index, int length)
        {
            int position = (int)_position;

            // resize, if needed
            this.Position += (uint)length;

            buffer.CopyTo(index, this.Buffer, position, length);
        }

        internal void WriteTo(BlobWriter stream)
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
            _position = 0;
            this.Length = 0;
        }

        #region Poolable

        private readonly ObjectPool<BlobWriter> _pool;

        //
        // To implement Poolable, you need two things:
        // 1) Expose Freeing primitive. 
        public void Free()
        {
            // Note that poolables are not finalizable. If one gets collected - no big deal.
            this.Clear();
            if (_pool != null)
            {
                if (this.Capacity < 1024)
                {
                    _pool.Free(this);
                }
                else
                {
                    _pool.ForgetTrackedObject(this);
                }
            }
        }

        //2) Expose  the way to get an instance.
        private static readonly ObjectPool<BlobWriter> s_poolInstance = CreatePool();

        public static BlobWriter GetInstance()
        {
            var stream = s_poolInstance.Allocate();
            return stream;
        }

        public static ObjectPool<BlobWriter> CreatePool()
        {
            return CreatePool(32);
        }

        public static ObjectPool<BlobWriter> CreatePool(int size)
        {
            ObjectPool<BlobWriter> pool = null;
            pool = new ObjectPool<BlobWriter>(() => new BlobWriter(pool), size);
            return pool;
        }
        #endregion
    }
}
