// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class SolutionState
    {
        private readonly struct UnrootedSymbolSet
        {
            public readonly WeakReference<IAssemblySymbol> PrimaryAssemblySymbol;
            public readonly WeakReference<ITypeSymbol?> PrimaryDynamicSymbol;
            public readonly WeakSet<ISymbol> SecondaryReferencedSymbols;

            public UnrootedSymbolSet(WeakReference<IAssemblySymbol> primaryAssemblySymbol, WeakReference<ITypeSymbol?> primaryDynamicSymbol, WeakSet<ISymbol> secondaryReferencedSymbols)
            {
                PrimaryAssemblySymbol = primaryAssemblySymbol;
                PrimaryDynamicSymbol = primaryDynamicSymbol;
                SecondaryReferencedSymbols = secondaryReferencedSymbols;
            }
        }
    }
}
