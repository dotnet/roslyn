// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

#if HAS_IOPERATION

namespace Analyzer.Utilities.Lightup
{
    using System;
    using Microsoft.CodeAnalysis;

    internal static class IsFileLocalWrapper
    {
        private static readonly Func<INamedTypeSymbol, bool> s_isFileLocal = LightupHelpers.CreateSymbolPropertyAccessor<INamedTypeSymbol, bool>(typeof(INamedTypeSymbol), nameof(IsFileLocal), fallbackResult: false);

        public static bool IsFileLocal(this INamedTypeSymbol symbol) => s_isFileLocal(symbol);
    }
}

#endif
