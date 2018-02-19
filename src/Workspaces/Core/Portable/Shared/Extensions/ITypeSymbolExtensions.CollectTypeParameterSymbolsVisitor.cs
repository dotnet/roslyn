// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal partial class ITypeSymbolExtensions
    {
        private class CollectTypeParameterSymbolsVisitor : SymbolVisitor
        {
            private readonly HashSet<ISymbol> _visited = new HashSet<ISymbol>();
            private readonly bool _onlyMethodTypeParameters;
            private readonly IList<ITypeParameterSymbol> _typeParameters;

            public CollectTypeParameterSymbolsVisitor(
                 IList<ITypeParameterSymbol> typeParameters,
                bool onlyMethodTypeParameters)
            {
                _onlyMethodTypeParameters = onlyMethodTypeParameters;
                _typeParameters = typeParameters;
            }

            public override void DefaultVisit(ISymbol node)
            {
                throw new NotImplementedException();
            }

            public override void VisitDynamicType(IDynamicTypeSymbol symbol)
            {
            }

            public override void VisitArrayType(IArrayTypeSymbol symbol)
            {
                if (!_visited.Add(symbol))
                {
                    return;
                }

                symbol.ElementType.Accept(this);
            }

            public override void VisitNamedType(INamedTypeSymbol symbol)
            {
                if (_visited.Add(symbol))
                {
                    foreach (var child in symbol.GetAllTypeArguments())
                    {
                        child.Accept(this);
                    }
                }
            }

            public override void VisitPointerType(IPointerTypeSymbol symbol)
            {
                if (!_visited.Add(symbol))
                {
                    return;
                }

                symbol.PointedAtType.Accept(this);
            }

            public override void VisitTypeParameter(ITypeParameterSymbol symbol)
            {
                if (_visited.Add(symbol))
                {
                    if (symbol.TypeParameterKind == TypeParameterKind.Method || !_onlyMethodTypeParameters)
                    {
                        if (!_typeParameters.Contains(symbol))
                        {
                            _typeParameters.Add(symbol);
                        }
                    }

                    foreach (var constraint in symbol.ConstraintTypes)
                    {
                        constraint.Accept(this);
                    }
                }
            }
        }
    }
}
