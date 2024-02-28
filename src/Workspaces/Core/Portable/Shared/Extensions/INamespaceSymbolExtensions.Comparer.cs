// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal partial class INamespaceSymbolExtensions
{
    private class Comparer : IEqualityComparer<INamespaceSymbol?>
    {
        public bool Equals(INamespaceSymbol? x, INamespaceSymbol? y)
            => GetNameParts(x).SequenceEqual(GetNameParts(y));

        public int GetHashCode(INamespaceSymbol? obj)
            => GetNameParts(obj).Aggregate(0, (a, v) => Hash.Combine(v, a));
    }
}
