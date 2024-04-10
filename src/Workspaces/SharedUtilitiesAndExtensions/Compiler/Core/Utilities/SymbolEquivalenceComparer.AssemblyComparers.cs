// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Shared.Utilities;

internal partial class SymbolEquivalenceComparer
{
    private sealed class SimpleNameAssemblyComparer : IEqualityComparer<IAssemblySymbol?>
    {
        public static readonly IEqualityComparer<IAssemblySymbol> Instance = new SimpleNameAssemblyComparer();

        public bool Equals(IAssemblySymbol? x, IAssemblySymbol? y)
            => AssemblyIdentityComparer.SimpleNameComparer.Equals(x?.Name, y?.Name);

        public int GetHashCode(IAssemblySymbol? obj)
            => obj is null ? 0 : AssemblyIdentityComparer.SimpleNameComparer.GetHashCode(obj.Name);
    }
}
