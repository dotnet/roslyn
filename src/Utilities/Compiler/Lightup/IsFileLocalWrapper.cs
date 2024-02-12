// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

#if HAS_IOPERATION

namespace Analyzer.Utilities.Lightup
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using Microsoft.CodeAnalysis;

    [SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Not a comparable instance.")]
    internal readonly struct IsFileLocalWrapper
    {
        private static readonly PropertyInfo? PropertyAccessor = typeof(INamedTypeSymbol).GetProperty("IsFileLocal", BindingFlags.Public | BindingFlags.Instance);

        private static readonly Func<INamedTypeSymbol, bool> MemberAccessor = PropertyAccessor is null ? _ => false : symbol => (bool)PropertyAccessor.GetValue(symbol);

        public INamedTypeSymbol WrappedSymbol { get; }

        private IsFileLocalWrapper(INamedTypeSymbol symbol)
        {
            WrappedSymbol = symbol;
        }

        public static IsFileLocalWrapper FromSymbol(INamedTypeSymbol symbol)
        {
            return new IsFileLocalWrapper(symbol);
        }

        public bool IsFileLocal => MemberAccessor(WrappedSymbol);
    }
}

#endif
