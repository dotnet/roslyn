// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Roslyn.Utilities
{
    internal sealed partial class StreamObjectWriter
    {
        private class WriterData : IDisposable
        {
            internal static readonly ObjectPool<Dictionary<object, int>> DictionaryPool =
                new ObjectPool<Dictionary<object, int>>(() => new Dictionary<object, int>(128), 2);

            private readonly WriterData _baseData;
            private readonly Dictionary<object, int> _valueToIdMap = DictionaryPool.Allocate();
            private int _nextId;

            private WriterData(ObjectData data)
            {
                if (data != null)
                {
                    foreach (var value in data.Objects)
                    {
                        _valueToIdMap.Add(value, _valueToIdMap.Count);
                    }
                }

                _nextId = _valueToIdMap.Count;
            }

            private WriterData(WriterData baseData)
            {
                _baseData = baseData;
                _nextId = baseData?._nextId ?? 0;
            }

            public static WriterData Create(ObjectData data = null)
            {
                if (data != null)
                {
                    return new WriterData(GetBaseWriterData(data));
                }
                else
                {
                    return new WriterData(data);
                }
            }

            private static readonly ConditionalWeakTable<ObjectData, WriterData> s_baseDataMap
                = new ConditionalWeakTable<ObjectData, WriterData>();

            private static WriterData GetBaseWriterData(ObjectData data)
            {
                WriterData baseData;
                if (!s_baseDataMap.TryGetValue(data, out baseData))
                {
                    baseData = s_baseDataMap.GetValue(data, _data => new WriterData(_data));
                }

                return baseData;
            }

            public void Dispose()
            {
                // If the map grew too big, don't return it to the pool.
                // When testing with the Roslyn solution, this dropped only 2.5% of requests.
                if (_valueToIdMap.Count > 1024)
                {
                    DictionaryPool.ForgetTrackedObject(_valueToIdMap);
                }
                else
                {
                    _valueToIdMap.Clear();
                    DictionaryPool.Free(_valueToIdMap);
                }
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
}