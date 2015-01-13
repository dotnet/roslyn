// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Roslyn.Utilities
{
    internal class ObjectReaderData : IDisposable
    {
        internal static readonly ObjectPool<List<object>> ListPool =
            new ObjectPool<List<object>>(() => new List<object>(128), 2);

        private readonly ObjectReaderData baseData;
        private readonly List<object> values = ListPool.Allocate();
        private readonly int baseDataCount;

        internal ObjectReaderData(params object[] items)
            : this((IEnumerable<object>)items)
        {
        }

        internal ObjectReaderData(IEnumerable<object> items)
        {
            if (items != null)
            {
                foreach (var value in items)
                {
                    this.values.Add(value);
                }
            }
        }

        internal ObjectReaderData(ObjectReaderData baseData)
        {
            Debug.Assert(baseData == null || baseData.baseData == null, "Should be <= 1 level deep");
            this.baseData = baseData;
            this.baseDataCount = (baseData != null) ? baseData.values.Count : 0;
        }

        public void Dispose()
        {
            this.values.Clear();
            ListPool.Free(this.values);
        }

        public int GetNextId()
        {
            this.values.Add(null);
            return baseDataCount + this.values.Count - 1;
        }

        public void AddValue(int id, object value)
        {
            this.values[id - baseDataCount] = value;
        }

        public object GetValue(int id)
        {
            if (this.baseData != null)
            {
                if (id < baseDataCount)
                {
                    return this.baseData.GetValue(id);
                }
                else
                {
                    return this.values[id - baseDataCount];
                }
            }
            else
            {
                return this.values[id];
            }
        }
    }
}