// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

#if HAS_IOPERATION

namespace Analyzer.Utilities.Lightup
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.CodeAnalysis;

    [SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Not a comparable instance.")]
    internal readonly struct IsFileLocalWrapper
    {
        private static readonly Func<INamedTypeSymbol, bool> PropertyAccessor = LightupHelpers.CreateSymbolPropertyAccessor<INamedTypeSymbol, bool>(typeof(INamedTypeSymbol), nameof(IsFileLocal), fallbackResult: false);

        public INamedTypeSymbol WrappedSymbol { get; }

        private IsFileLocalWrapper(INamedTypeSymbol symbol)
        {
            WrappedSymbol = symbol;
        }

        public static IsFileLocalWrapper FromSymbol(INamedTypeSymbol symbol)
        {
            return new IsFileLocalWrapper(symbol);
        }

        public bool IsFileLocal => PropertyAccessor(WrappedSymbol);
    }
}

#endif
