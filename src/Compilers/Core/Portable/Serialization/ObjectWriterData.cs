// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Roslyn.Utilities
{
    internal class ObjectWriterData : IDisposable
    {
        internal static readonly ObjectPool<Dictionary<object, int>> DictionaryPool =
            new ObjectPool<Dictionary<object, int>>(() => new Dictionary<object, int>(128), 2);

        private readonly ObjectWriterData _baseData;
        private readonly Dictionary<object, int> _valueToIdMap = DictionaryPool.Allocate();
        private int _nextId;

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
                    _valueToIdMap.Add(value, _valueToIdMap.Count);
                }
            }

            _nextId = _valueToIdMap.Count;
        }

        internal ObjectWriterData(ObjectWriterData baseData)
        {
            _baseData = baseData;
            _nextId = baseData?._nextId ?? 0;
        }

        public void Dispose()
        {
            // If the map grew too big, don't return it to the pool.
            // When testing with the Roslyn solution, this dropped only 2.5% of requests.
            if (_valueToIdMap.Count > 1024)
            {
                DictionaryPool.ForgetTrackedObject(_valueToIdMap);
                return;
            }

            _valueToIdMap.Clear();
            DictionaryPool.Free(_valueToIdMap);
        }

        public bool TryGetId(object value, out int id)
        {
            if (_baseData != null && _baseData.TryGetId(value, out id))
            {
                return true;
            }

            return _valueToIdMap.TryGetValue(value, out id);
        }

        private int GetNextId()
        {
            var id = _nextId;
            _nextId++;
            return id;
        }

        public int Add(object value)
        {
            var id = this.GetNextId();
            _valueToIdMap.Add(value, id);
            return id;
        }
    }
}
