// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.PooledObjects;

internal class PooledStringHashSet
{
    private static ObjectPool<PooledHashSet<string>>? s_poolCaseInsensitiveInstance;

    public static PooledDisposer<PooledHashSet<string>> GetInstance(bool caseSensitive, out PooledHashSet<string> instance)
    {
        if (caseSensitive)
            return PooledHashSet<string>.GetInstance(out instance);

        s_poolCaseInsensitiveInstance ??= PooledHashSet<string>.CreatePool(StringComparer.OrdinalIgnoreCase);
        instance = s_poolCaseInsensitiveInstance.Allocate();
        Debug.Assert(instance.Count == 0);
        return new PooledDisposer<PooledHashSet<string>>(instance);
    }
}
