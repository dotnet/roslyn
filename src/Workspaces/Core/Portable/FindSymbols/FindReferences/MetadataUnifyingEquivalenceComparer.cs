// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal sealed class MetadataUnifyingEquivalenceComparer : IEqualityComparer<ISymbol>
    {
        public static readonly IEqualityComparer<ISymbol> Instance = new MetadataUnifyingEquivalenceComparer();

        private MetadataUnifyingEquivalenceComparer()
        {
        }

        public bool Equals(ISymbol x, ISymbol y)
        {
            // If either symbol is from source, then we must do stricter equality. Consider this:
            //
            //     S1 <-> M <-> S2     (where S# = source symbol, M = some metadata symbol)
            //
            // In this case, imagine that both the comparisons denoted by <-> were done with the
            // SymbolEquivalenceComparer, and returned true. If S1 and S2 were from different projects,
            // they would compare false but transitivity would say they must be true. Another way to think
            // of this is any use of a source symbol "poisons" the comparison and requires it to be stricter.
            if (x == null || y == null || IsInSource(x) || IsInSource(y))
            {
                return object.Equals(x, y);
            }

            // Both of the symbols are from metadata, so defer to the equivalence comparer
            return SymbolEquivalenceComparer.Instance.Equals(x, y);
        }

        public int GetHashCode(ISymbol obj)
        {
            if (IsInSource(obj))
            {
                return obj.GetHashCode();
            }
            else
            {
                return SymbolEquivalenceComparer.Instance.GetHashCode(obj);
            }
        }

        private static bool IsInSource(ISymbol symbol)
        {
            return symbol.Locations.Any(l => l.IsInSource);
        }
    }
}
