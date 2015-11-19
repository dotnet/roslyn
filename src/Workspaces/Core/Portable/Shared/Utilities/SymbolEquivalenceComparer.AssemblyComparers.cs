// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal partial class SymbolEquivalenceComparer
    {
        private sealed class SimpleNameAssemblyComparer : IEqualityComparer<IAssemblySymbol>
        {
            public static readonly IEqualityComparer<IAssemblySymbol> Instance = new SimpleNameAssemblyComparer();

            public bool Equals(IAssemblySymbol x, IAssemblySymbol y)
            {
                return AssemblyIdentityComparer.SimpleNameComparer.Equals(x.Name, y.Name);
            }

            public int GetHashCode(IAssemblySymbol obj)
            {
                return AssemblyIdentityComparer.SimpleNameComparer.GetHashCode(obj.Name);
            }
        }
    }
}
