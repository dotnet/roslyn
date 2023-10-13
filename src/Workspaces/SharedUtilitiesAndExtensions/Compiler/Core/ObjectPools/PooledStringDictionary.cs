// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.PooledObjects;

internal sealed partial class PooledStringDictionary<V>
{
    private static ObjectPool<PooledDictionary<string, V>>? s_poolCaseInsensitiveInstance;

    public static PooledDisposer<PooledDictionary<string, V>> GetInstance(bool caseSensitive, out PooledDictionary<string, V> instance)
    {
        if (caseSensitive)
            return PooledDictionary<string, V>.GetInstance(out instance);

        s_poolCaseInsensitiveInstance ??= PooledDictionary<string, V>.CreatePool(StringComparer.OrdinalIgnoreCase);
        instance = s_poolCaseInsensitiveInstance.Allocate();
        Debug.Assert(instance.Count == 0);
        return new PooledDisposer<PooledDictionary<string, V>>(instance);
    }
}
