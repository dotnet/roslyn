// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Text;

namespace Microsoft.CodeAnalysis.PooledObjects
{
    /// <summary>
    /// The usage is:
    ///        var inst = PooledStringBuilder.GetInstance();
    ///        var sb = inst.builder;
    ///        ... Do Stuff...
    ///        ... sb.ToString() ...
    ///        inst.Free();
    /// </summary>
    internal class PooledStringBuilder
    {
        public readonly StringBuilder Builder = new StringBuilder();
        private readonly ObjectPool<PooledStringBuilder> _pool;

        private PooledStringBuilder(ObjectPool<PooledStringBuilder> pool)
        {
            Debug.Assert(pool != null);
            _pool = pool;
        }

        public int Length
        {
            get { return this.Builder.Length; }
        }

        public void Free()
        {
            var builder = this.Builder;

            // do not store builders that are too large.
            if (builder.Capacity <= 1024)
            {
                builder.Clear();
                _pool.Free(this);
            }
            else
            {
                _pool.ForgetTrackedObject(this);
            }
        }

        [System.Obsolete("Consider calling ToStringAndFree instead.")]
        public new string ToString()
        {
            return this.Builder.ToString();
        }

        public string ToStringAndFree()
        {
            var result = this.Builder.ToString();
            this.Free();

            return result;
        }

        public string ToStringAndFree(int startIndex, int length)
        {
            var result = this.Builder.ToString(startIndex, length);
            this.Free();

            return result;
        }

        // global pool
        private static readonly ObjectPool<PooledStringBuilder> s_poolInstance = CreatePool();

        // if someone needs to create a private pool;
        /// <summary>
        /// If someone need to create a private pool
        /// </summary>
        /// <param name="size">The size of the pool.</param>
        /// <returns></returns>
        public static ObjectPool<PooledStringBuilder> CreatePool(int size = 32)
        {
            ObjectPool<PooledStringBuilder> pool = null;
            pool = new ObjectPool<PooledStringBuilder>(() => new PooledStringBuilder(pool), size);
            return pool;
        }

        public static PooledStringBuilder GetInstance()
        {
            var builder = s_poolInstance.Allocate();
            Debug.Assert(builder.Builder.Length == 0);
            return builder;
        }

        public static PooledStringBuilderDisposer GetInstance(out PooledStringBuilder instance)
        {
            instance = GetInstance();
            return new PooledStringBuilderDisposer(instance);
        }

        public static implicit operator StringBuilder(PooledStringBuilder obj)
        {
            return obj.Builder;
        }

        internal struct PooledStringBuilderDisposer : IDisposable
        {
            private PooledStringBuilder _pooledObject;

            public PooledStringBuilderDisposer(PooledStringBuilder instance)
            {
                _pooledObject = instance;
            }

            public void Dispose()
            {
                var pooledObject = _pooledObject;
                if (pooledObject != null)
                {
                    pooledObject.Free();
                    _pooledObject = null;
                }
            }
        }
    }
}
