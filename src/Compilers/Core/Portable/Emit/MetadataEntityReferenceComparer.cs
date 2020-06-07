// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.Emit
{
    internal sealed class MetadataEntityReferenceComparer : IEqualityComparer<object>
    {
        internal static readonly MetadataEntityReferenceComparer ConsiderEverything = new MetadataEntityReferenceComparer(TypeCompareKind.ConsiderEverything);

        private readonly TypeCompareKind _compareKind;

        private MetadataEntityReferenceComparer(TypeCompareKind compareKind)
        {
            _compareKind = compareKind;
        }

        public new bool Equals(object x, object y)
        {
            if (x is null)
            {
                return y is null;
            }
            else if (ReferenceEquals(x, y))
            {
                return true;
            }
            else if (x is ISymbolInternal sx && y is ISymbolInternal sy)
            {
                return sx.Equals(sy, _compareKind);
            }
            else if (x is ISymbolCompareKindComparableInternal cx && y is ISymbolCompareKindComparableInternal cy)
            {
                return cx.Equals(cy, _compareKind);
            }
            else
            {
                return x.Equals(y);
            }
        }

        public int GetHashCode(object obj)
        {
            return obj?.GetHashCode() ?? 0;
        }
    }
}
