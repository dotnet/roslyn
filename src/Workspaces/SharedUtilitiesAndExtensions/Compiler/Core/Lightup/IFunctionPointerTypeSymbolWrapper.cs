// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Lightup
{
    internal readonly struct IFunctionPointerTypeSymbolWrapper : ISymbolWrapper<ITypeSymbol>
    {
        internal const string WrappedTypeName = "Microsoft.CodeAnalysis.IFunctionPointerTypeSymbol";
        private static readonly Type? s_wrappedType;

        private static readonly Func<ITypeSymbol, IMethodSymbol?> s_signatureAccessor;

        private readonly ITypeSymbol _symbol;

        static IFunctionPointerTypeSymbolWrapper()
        {
            s_wrappedType = WrapperHelper.GetWrappedType(typeof(IFunctionPointerTypeSymbolWrapper));
            s_signatureAccessor = LightupHelpers.CreateSymbolPropertyAccessor<ITypeSymbol, IMethodSymbol?>(s_wrappedType, nameof(Signature), null);
        }

        private IFunctionPointerTypeSymbolWrapper(ITypeSymbol symbol)
        {
            _symbol = symbol;
        }

        public ITypeSymbol Symbol => _symbol;

        public IMethodSymbol Signature => s_signatureAccessor(Symbol) ?? throw ExceptionUtilities.Unreachable;

        public static IFunctionPointerTypeSymbolWrapper FromSymbol(ISymbol symbol)
        {
            if (symbol is null)
                return default;

            if (!IsInstance(symbol))
                throw new InvalidCastException(string.Format(CompilerExtensionsResources.Cannot_cast_0_to_1, symbol.GetType().FullName, WrappedTypeName));

            return new IFunctionPointerTypeSymbolWrapper((ITypeSymbol)symbol);
        }

        public static bool IsInstance(ISymbol symbol)
        {
            return symbol is object && LightupHelpers.CanWrapSymbol(symbol, s_wrappedType);
        }
    }
}
