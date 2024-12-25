// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Roslyn.Utilities;

internal sealed partial class ObjectWriter
{
    /// <summary>
    /// An object reference to reference-id map, that can share base data efficiently.
    /// </summary>
    private struct WriterReferenceMap
    {
        // PERF: Use segmented collection to avoid Large Object Heap allocations during serialization.
        // https://github.com/dotnet/roslyn/issues/43401
        private readonly SegmentedDictionary<string, int> _valueToIdMap;
        private int _nextId;

        private static readonly ObjectPool<SegmentedDictionary<string, int>> s_valueDictionaryPool = new(() => new(128));

        public WriterReferenceMap()
        {
            _valueToIdMap = s_valueDictionaryPool.Allocate();
            _nextId = 0;
        }

        public readonly void Dispose()
        {
            // If the map grew too big, don't return it to the pool.
            // When testing with the Roslyn solution, this dropped only 2.5% of requests.
            if (_valueToIdMap.Count > 1024)
            {
                s_valueDictionaryPool.ForgetTrackedObject(_valueToIdMap);
            }
            else
            {
                _valueToIdMap.Clear();
                s_valueDictionaryPool.Free(_valueToIdMap);
            }
        }

        public bool TryGetReferenceId(string value, out int referenceId)
            => _valueToIdMap.TryGetValue(value, out referenceId);

        public void Add(string value)
        {
            var id = _nextId++;
            _valueToIdMap.Add(value, id);
        }
    }
}
