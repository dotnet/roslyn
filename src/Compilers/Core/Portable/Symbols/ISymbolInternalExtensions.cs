// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Symbols
{
    internal static class ISymbolInternalExtensions
    {
        extension(ISymbolInternal symbol)
        {
            internal INamedTypeSymbolInternal RequiredContainingType
            {
                get
                {
                    var containingType = symbol.ContainingType;
                    Debug.Assert(containingType is not null, $"'{symbol.Name}': Unexpected null ContainingType");
                    return containingType;
                }
            }
        }
    }
}
