// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Roslyn.Utilities
{
    internal class ObjectReaderData : IDisposable
    {
        internal static readonly ObjectPool<List<object>> ListPool =
            new ObjectPool<List<object>>(() => new List<object>(128), 2);

        private readonly ObjectReaderData _baseData;
        private readonly List<object> _values = ListPool.Allocate();
        private readonly int _baseDataCount;

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
                    _values.Add(value);
                }
            }
        }

        internal ObjectReaderData(ObjectReaderData baseData)
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
