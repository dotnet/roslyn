// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
    internal sealed partial class PooledStringBuilder
#if !MICROSOFT_CODEANALYSIS_POOLEDOBJECTS_NO_POOLED_DISPOSER
        : IPooled
#endif
    {
        public readonly StringBuilder Builder = new();
        private readonly ObjectPool<PooledStringBuilder> _pool;

        private PooledStringBuilder(ObjectPool<PooledStringBuilder> pool)
        {
            Debug.Assert(pool != null);
            _pool = pool!;
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
        public static ObjectPool<PooledStringBuilder> CreatePool(int size = 32)
        {
            ObjectPool<PooledStringBuilder>? pool = null;
            pool = new ObjectPool<PooledStringBuilder>(() => new PooledStringBuilder(pool!), size);
            return pool;
        }

        public static PooledStringBuilder GetInstance()
        {
            var builder = s_poolInstance.Allocate();
            Debug.Assert(builder.Builder.Length == 0);
            return builder;
        }

        public static implicit operator StringBuilder(PooledStringBuilder obj)
        {
            return obj.Builder;
        }

#if !MICROSOFT_CODEANALYSIS_POOLEDOBJECTS_NO_POOLED_DISPOSER
        private static readonly ObjectPool<PooledStringBuilder> s_keepLargeInstancesPool = CreatePool();

        public static PooledDisposer<PooledStringBuilder> GetInstance(out StringBuilder instance)
            => GetInstance(discardLargeInstances: true, out instance);

        public static PooledDisposer<PooledStringBuilder> GetInstance(bool discardLargeInstances, out StringBuilder instance)
        {
            // If we're discarding large instances (the default behavior), then just use the normal pool.  If we're not, use
            // a specific pool so that *other* normal callers don't accidentally get it and discard it.
            var pooledInstance = discardLargeInstances ? GetInstance() : s_keepLargeInstancesPool.Allocate();
            instance = pooledInstance;
            return new PooledDisposer<PooledStringBuilder>(pooledInstance, discardLargeInstances);
        }

        void IPooled.Free(bool discardLargeInstances)
        {
            // If we're discarding large instances, use the default behavior (which already does that).  Otherwise, always
            // clear and free the instance back to its originating pool.
            if (discardLargeInstances)
            {
                Free();
            }
            else
            {
                this.Builder.Clear();
                _pool.Free(this);
            }
        }
#endif
    }
}
