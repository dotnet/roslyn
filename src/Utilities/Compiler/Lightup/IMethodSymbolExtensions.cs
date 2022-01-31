// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.Lightup
{
    internal static class IMethodSymbolExtensions
    {
        private static readonly Func<IMethodSymbol, bool> s_isInitOnly
            = LightupHelpers.CreateSymbolPropertyAccessor<IMethodSymbol, bool>(typeof(IMethodSymbol), "IsInitOnly", false);

        public static bool IsInitOnly(this IMethodSymbol methodSymbol)
            => s_isInitOnly(methodSymbol);
    }
}
