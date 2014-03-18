// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal partial class ITypeSymbolExtensions
    {
        private class CollectTypeParameterSymbolsVisitor : SymbolVisitor
        {
            private readonly HashSet<ISymbol> visited = new HashSet<ISymbol>();
            private readonly bool onlyMethodTypeParameters;
            private readonly IList<ITypeParameterSymbol> typeParameters;

            public CollectTypeParameterSymbolsVisitor(
                 IList<ITypeParameterSymbol> typeParameters,
                bool onlyMethodTypeParameters)
            {
                this.onlyMethodTypeParameters = onlyMethodTypeParameters;
                this.typeParameters = typeParameters;
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
                if (!visited.Add(symbol))
                {
                    return;
                }

                symbol.ElementType.Accept(this);
            }

            public override void VisitNamedType(INamedTypeSymbol symbol)
            {
                if (visited.Add(symbol))
                {
                    foreach (var child in symbol.GetAllTypeArguments())
                    {
                        child.Accept(this);
                    }
                }
            }

            public override void VisitPointerType(IPointerTypeSymbol symbol)
            {
                if (!visited.Add(symbol))
                {
                    return;
                }

                symbol.PointedAtType.Accept(this);
            }

            public override void VisitTypeParameter(ITypeParameterSymbol symbol)
            {
                if (visited.Add(symbol))
                {
                    if (symbol.TypeParameterKind == TypeParameterKind.Method || !onlyMethodTypeParameters)
                    {
                        if (!typeParameters.Contains(symbol))
                        {
                            typeParameters.Add(symbol);
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