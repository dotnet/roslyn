// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Roslyn.Utilities
{
    internal class ObjectWriterData : IDisposable
    {
        internal static readonly ObjectPool<Dictionary<object, int>> DictionaryPool =
            new ObjectPool<Dictionary<object, int>>(() => new Dictionary<object, int>(128), 2);

        private readonly ObjectWriterData baseData;
        private readonly Dictionary<object, int> valueToIdMap = DictionaryPool.Allocate();
        private int nextId;

        internal ObjectWriterData(params object[] items)
            : this((IEnumerable<object>)items)
        {
        }

        internal ObjectWriterData(IEnumerable<object> items)
        {
            if (items != null)
            {
                foreach (var value in items)
                {
                    this.valueToIdMap.Add(value, this.valueToIdMap.Count);
                }
            }

            this.nextId = this.valueToIdMap.Count;
        }

        internal ObjectWriterData(ObjectWriterData baseData)
        {
            this.baseData = baseData;
            this.nextId = (baseData != null) ? baseData.nextId : 0;
        }

        public void Dispose()
        {
            // If the map grew too big, don't return it to the pool.
            // When testing with the Roslyn solution, this dropped only 2.5% of requests.
            if (this.valueToIdMap.Count > 1024)
            {
                DictionaryPool.ForgetTrackedObject(this.valueToIdMap);
                return;
            }

            this.valueToIdMap.Clear();
            DictionaryPool.Free(this.valueToIdMap);
        }

        public bool TryGetId(object value, out int id)
        {
            if (this.baseData != null && this.baseData.TryGetId(value, out id))
            {
                return true;
            }

            return this.valueToIdMap.TryGetValue(value, out id);
        }

        private int GetNextId()
        {
            var id = this.nextId;
            this.nextId++;
            return id;
        }

        public int Add(object value)
        {
            var id = this.GetNextId();
            this.valueToIdMap.Add(value, id);
            return id;
        }
    }
}