// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.FindSymbols;

internal sealed class MetadataUnifyingSymbolHashSet : HashSet<ISymbol>, IPooled
{
    private static readonly ObjectPool<MetadataUnifyingSymbolHashSet> s_metadataUnifyingSymbolHashSetPool = new(() => []);

    public MetadataUnifyingSymbolHashSet() : base(MetadataUnifyingEquivalenceComparer.Instance)
    {
    }

    public static PooledDisposer<MetadataUnifyingSymbolHashSet> GetInstance(out MetadataUnifyingSymbolHashSet instance)
    {
        instance = s_metadataUnifyingSymbolHashSetPool.Allocate();
        Debug.Assert(instance.Count == 0);

        return new PooledDisposer<MetadataUnifyingSymbolHashSet>(instance);
    }

    public void Free(bool discardLargeInstances)
    {
        // ignore discardLargeInstances as we don't limit our pooled hashset capacities
        Clear();

        s_metadataUnifyingSymbolHashSetPool.Free(this);
    }
}
