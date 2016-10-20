// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Roslyn.Utilities
{
    internal sealed partial class StreamObjectReader
    {
        private class ReaderData : IDisposable
        {
            internal static readonly ObjectPool<List<object>> ListPool =
                new ObjectPool<List<object>>(() => new List<object>(128), 2);

            private readonly ReaderData _baseData;
            private readonly List<object> _values = ListPool.Allocate();
            private readonly int _baseDataCount;

            private ReaderData(ObjectData data)
            {
                if (data != null)
                {
                    foreach (var value in data.Objects)
                    {
                        _values.Add(value);
                    }
                }
            }

            private ReaderData(ReaderData baseData)
            {
                Debug.Assert(baseData?._baseData == null, "Should be <= 1 level deep");
                _baseData = baseData;
                _baseDataCount = baseData?._values.Count ?? 0;
            }

            public void Dispose()
            {
                _values.Clear();
                ListPool.Free(_values);
            }

            public static ReaderData Create(ObjectData data = null)
            {
                if (data != null)
                {
                    return new ReaderData(GetBaseReaderData(data));
                }
                else
                {
                    return new ReaderData(data);
                }
            }

            private static readonly ConditionalWeakTable<ObjectData, ReaderData> s_baseDataMap
                = new ConditionalWeakTable<ObjectData, ReaderData>();

            private static ReaderData GetBaseReaderData(ObjectData data)
            {
                ReaderData baseData;
                if (!s_baseDataMap.TryGetValue(data, out baseData))
                {
                    baseData = s_baseDataMap.GetValue(data, _data => new ReaderData(_data));
                }

                return baseData;
            }


            public int GetNextId()
            {
                _values.Add(null);
                return _baseDataCount + _values.Count - 1;
            }

            public void AddValue(int id, object value)
            {
                _values[id - _baseDataCount] = value;
            }

            public object GetValue(int id)
            {
                if (_baseData != null)
                {
                    if (id < _baseDataCount)
                    {
                        return _baseData.GetValue(id);
                    }
                    else
                    {
                        return _values[id - _baseDataCount];
                    }
                }
                else
                {
                    return _values[id];
                }
            }
        }
    }
}