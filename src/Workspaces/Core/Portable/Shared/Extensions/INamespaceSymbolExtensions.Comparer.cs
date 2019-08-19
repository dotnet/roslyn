// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal partial class INamespaceSymbolExtensions
    {
        private class Comparer : IEqualityComparer<INamespaceSymbol?>
        {
            public bool Equals(INamespaceSymbol? x, INamespaceSymbol? y)
            {
                return GetNameParts(x).SequenceEqual(GetNameParts(y));
            }

            public int GetHashCode(INamespaceSymbol? obj)
            {
                return GetNameParts(obj).Aggregate(0, (a, v) => Hash.Combine(v, a));
            }
        }
    }
}
