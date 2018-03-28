// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SymbolDisplay
{
    internal abstract partial class AbstractSymbolDisplayVisitor : SymbolVisitor
    {
        protected abstract bool ShouldRestrictMinimallyQualifyLookupToNamespacesAndTypes();

        protected bool IsMinimizing
        {
            get { return this.semanticModelOpt != null; }
        }

        protected bool NameBoundSuccessfullyToSameSymbol(INamedTypeSymbol symbol)
        {
            ImmutableArray<ISymbol> normalSymbols = ShouldRestrictMinimallyQualifyLookupToNamespacesAndTypes()
                ? semanticModelOpt.LookupNamespacesAndTypes(positionOpt, name: symbol.Name)
                : semanticModelOpt.LookupSymbols(positionOpt, name: symbol.Name);
            ISymbol normalSymbol = SingleSymbolWithArity(normalSymbols, symbol.Arity);

            if (normalSymbol == null)
            {
                return false;
            }

            // Binding normally ended up with the right symbol.  We can definitely use the
            // simplified name.
            if (normalSymbol.Equals(symbol.OriginalDefinition))
            {
                return true;
            }

            // Binding normally failed.  We may be in a "Color Color" situation where 'Color'
            // will bind to the field, but we could still allow simplification here.
            ImmutableArray<ISymbol> typeOnlySymbols = semanticModelOpt.LookupNamespacesAndTypes(positionOpt, name: symbol.Name);
            ISymbol typeOnlySymbol = SingleSymbolWithArity(typeOnlySymbols, symbol.Arity);

            if (typeOnlySymbol == null)
            {
                return false;
            }

            var type1 = GetSymbolType(normalSymbol);
            var type2 = GetSymbolType(typeOnlySymbol);

            return
                type1 != null &&
                type2 != null &&
                type1.Equals(type2) &&
                typeOnlySymbol.Equals(symbol.OriginalDefinition);
        }

        private static ISymbol SingleSymbolWithArity(ImmutableArray<ISymbol> candidates, int desiredArity)
        {
            ISymbol singleSymbol = null;
            foreach (ISymbol candidate in candidates)
            {
                int arity;
                switch (candidate.Kind)
                {
                    case SymbolKind.NamedType:
                        arity = ((INamedTypeSymbol)candidate).Arity;
                        break;
                    case SymbolKind.Method:
                        arity = ((IMethodSymbol)candidate).Arity;
                        break;
                    default:
                        arity = 0;
                        break;
                }

                if (arity == desiredArity)
                {
                    if (singleSymbol == null)
                    {
                        singleSymbol = candidate;
                    }
                    else
                    {
                        singleSymbol = null;
                        break;
                    }
                }
            }
            return singleSymbol;
        }

        protected static ITypeSymbol GetSymbolType(ISymbol symbol)
        {
            var localSymbol = symbol as ILocalSymbol;
            if (localSymbol != null)
            {
                return localSymbol.Type;
            }

            var fieldSymbol = symbol as IFieldSymbol;
            if (fieldSymbol != null)
            {
                return fieldSymbol.Type;
            }

            var propertySymbol = symbol as IPropertySymbol;
            if (propertySymbol != null)
            {
                return propertySymbol.Type;
            }

            var parameterSymbol = symbol as IParameterSymbol;
            if (parameterSymbol != null)
            {
                return parameterSymbol.Type;
            }

            var aliasSymbol = symbol as IAliasSymbol;
            if (aliasSymbol != null)
            {
                return aliasSymbol.Target as ITypeSymbol;
            }

            return symbol as ITypeSymbol;
        }
    }
}
