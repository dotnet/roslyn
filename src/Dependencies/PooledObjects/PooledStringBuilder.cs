// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Collections
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
            string result = this.Builder.ToString();
            this.Free();

            return result;
        }

        public string ToStringAndFree(int startIndex, int length)
        {
            string result = this.Builder.ToString(startIndex, length);
            this.Free();

            return result;
        }

        // global pool
        private static readonly ObjectPool<PooledStringBuilder> s_poolInstance = CreatePool();

        // if someone needs to create a private pool;
        public static ObjectPool<PooledStringBuilder> CreatePool()
        {
            ObjectPool<PooledStringBuilder> pool = null;
            pool = new ObjectPool<PooledStringBuilder>(() => new PooledStringBuilder(pool), 32);
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
    }
}
