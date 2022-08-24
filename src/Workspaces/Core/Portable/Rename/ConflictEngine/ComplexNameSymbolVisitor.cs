// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    internal sealed class ComplexNameSymbolVisitor : SymbolVisitor
    {
        private readonly ImmutableHashSet<ISymbol>.Builder _allSymbols = ImmutableHashSet.CreateBuilder<ISymbol>();

        public static ImmutableHashSet<ISymbol> GetAllSymbolsInFullyQualifiedName(ISymbol symbol)
        {
            var visitor = new ComplexNameSymbolVisitor();
            visitor.Visit(symbol);
            return visitor._allSymbols.ToImmutableHashSet();
        }

        public override void DefaultVisit(ISymbol symbol)
            => _allSymbols.Add(symbol);

        public override void VisitEvent(IEventSymbol symbol)
        {
            VisitNamedType(symbol.ContainingType);
            DefaultVisit(symbol);
        }

        public override void VisitField(IFieldSymbol symbol)
        {
            VisitNamedType(symbol.ContainingType);
            DefaultVisit(symbol);
        }

        public override void VisitMethod(IMethodSymbol symbol)
        {
            DefaultVisit(symbol);
            VisitNamedType(symbol.ContainingType);
            foreach (var argument in symbol.TypeArguments)
            {
                Visit(argument);
            }
        }

        public override void VisitNamedType(INamedTypeSymbol symbol)
        {
            DefaultVisit(symbol);
            var containingType = symbol.ContainingType;
            while (containingType != null)
            {
                DefaultVisit(containingType);
                containingType = containingType.ContainingType;
            }

            var containingNamespace = symbol.ContainingNamespace;
            if (containingNamespace != null)
            {
                VisitNamespace(containingNamespace);
            }
        }

        public override void VisitNamespace(INamespaceSymbol symbol)
        {
            while (symbol != null)
            {
                DefaultVisit(symbol);
                symbol = symbol.ContainingNamespace;
            }
        }

        public override void VisitPointerType(IPointerTypeSymbol symbol)
        {
            DefaultVisit(symbol);
            var pointedAtType = symbol.PointedAtType;
            pointedAtType.Accept(this);
        }

        public override void VisitFunctionPointerType(IFunctionPointerTypeSymbol symbol)
        {
            DefaultVisit(symbol);
            VisitMethod(symbol.Signature);
        }

        public override void VisitProperty(IPropertySymbol symbol)
        {
            VisitNamedType(symbol.ContainingType);
            DefaultVisit(symbol);
        }
    }
}

