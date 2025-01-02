// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Roslyn.Utilities;

internal sealed partial class ObjectReader
{
    /// <summary>
    /// A reference-id to object map, that can share base data efficiently.
    /// </summary>
    private readonly struct ReaderReferenceMap : IDisposable
    {
        private readonly SegmentedList<string> _values;

        private static readonly ObjectPool<SegmentedList<string>> s_objectListPool
            = new(() => new SegmentedList<string>(20));

        private ReaderReferenceMap(SegmentedList<string> values)
            => _values = values;

        public static ReaderReferenceMap Create()
            => new(s_objectListPool.Allocate());

        public void Dispose()
        {
            _values.Clear();
            s_objectListPool.Free(_values);
        }

        public void AddValue(string value)
            => _values.Add(value);

        public string GetValue(int referenceId)
            => _values[referenceId];
    }
}
