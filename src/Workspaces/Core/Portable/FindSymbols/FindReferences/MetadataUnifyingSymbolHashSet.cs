// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.FindSymbols;

internal sealed class MetadataUnifyingSymbolHashSet : HashSet<ISymbol>
{
    private static readonly ObjectPool<MetadataUnifyingSymbolHashSet> s_metadataUnifyingSymbolHashSetPool = new(() => []);

    public MetadataUnifyingSymbolHashSet() : base(MetadataUnifyingEquivalenceComparer.Instance)
    {
    }

    public static MetadataUnifyingSymbolHashSet AllocateFromPool()
        => s_metadataUnifyingSymbolHashSetPool.Allocate();

    public static void ClearAndFree(MetadataUnifyingSymbolHashSet set)
    {
        set.Clear();
        s_metadataUnifyingSymbolHashSetPool.Free(set);
    }
}
