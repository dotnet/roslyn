// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen;

/// <summary>
/// Handles storage of strings referenced via tokens in metadata. When values are stored
/// they are uniquely "associated" with fake tokens, which are basically sequential numbers.
/// IL gen will use these fake tokens during codegen and later, when actual values
/// are known, the method bodies will be patched.
/// </summary>
internal sealed class StringTokenMap(int initialHeapSize)
{
    private readonly ConcurrentDictionary<string, uint> _valueToToken = new ConcurrentDictionary<string, uint>(ReferenceEqualityComparer.Instance);
    private readonly ArrayBuilder<string> _uniqueValues = [];
    private int _heapSize = initialHeapSize;

    public bool TryGetOrAddToken(string value, out uint token)
    {
        // NOTE: cannot use GetOrAdd here since _uniqueValues and _valueToToken must be in sync
        // so if we do need to add we have to take a lock and modify both collections.
        if (_valueToToken.TryGetValue(value, out token))
        {
            return true;
        }

        lock (_uniqueValues)
        {
            if (_valueToToken.TryGetValue(value, out token))
            {
                return true;
            }

            // String can span beyond the heap limit, but must start within the limit.
            if (_heapSize > MetadataHelpers.UserStringHeapCapacity)
            {
                return false;
            }

            _heapSize += MetadataHelpers.GetUserStringBlobSize(value);

            token = (uint)_uniqueValues.Count;
            _uniqueValues.Add(value);
            _valueToToken.Add(value, token);

            return true;
        }
    }

    public string GetValue(uint token)
    {
        lock (_uniqueValues)
        {
            return _uniqueValues[(int)token];
        }
    }

    public string[] CopyValues()
    {
        lock (_uniqueValues)
        {
            return _uniqueValues.ToArray();
        }
    }
}
