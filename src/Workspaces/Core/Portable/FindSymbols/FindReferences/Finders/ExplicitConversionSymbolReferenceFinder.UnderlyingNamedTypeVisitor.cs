// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal partial class ExplicitConversionSymbolReferenceFinder
    {
        private class UnderlyingNamedTypeVisitor : SymbolVisitor<INamedTypeSymbol?>
        {
            public static readonly UnderlyingNamedTypeVisitor Instance = new();

            private UnderlyingNamedTypeVisitor()
            {
            }

            public override INamedTypeSymbol? VisitArrayType(IArrayTypeSymbol symbol)
                => Visit(symbol.ElementType);

            public override INamedTypeSymbol? VisitDynamicType(IDynamicTypeSymbol symbol)
                => null;

            public override INamedTypeSymbol? VisitFunctionPointerType(IFunctionPointerTypeSymbol symbol)
                => null;

            public override INamedTypeSymbol? VisitPointerType(IPointerTypeSymbol symbol)
                => Visit(symbol.PointedAtType);

            public override INamedTypeSymbol? VisitTypeParameter(ITypeParameterSymbol symbol)
                => null;

            public override INamedTypeSymbol? VisitNamedType(INamedTypeSymbol symbol)
                => symbol;

            public override INamedTypeSymbol? DefaultVisit(ISymbol symbol)
            {
                Debug.Fail($"Symbol case not handled: {symbol.Kind}");
                return base.DefaultVisit(symbol);
            }
        }
    }
}
