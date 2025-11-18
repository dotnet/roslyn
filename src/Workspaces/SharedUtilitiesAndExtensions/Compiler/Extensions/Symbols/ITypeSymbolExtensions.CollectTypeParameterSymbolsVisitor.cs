// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class ITypeSymbolExtensions
{
    private sealed class CollectTypeParameterSymbolsVisitor(
        ArrayBuilder<ITypeParameterSymbol> typeParameters,
        bool onlyMethodTypeParameters) : SymbolVisitor, IDisposable
    {
        private readonly PooledHashSet<ISymbol> _visited = PooledHashSet<ISymbol>.GetInstance();

        public void Dispose()
        {
            _visited.Free();
        }

        public override void DefaultVisit(ISymbol node)
            => throw new NotImplementedException();

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

        public override void VisitFunctionPointerType(IFunctionPointerTypeSymbol symbol)
        {
            if (!_visited.Add(symbol))
            {
                return;
            }

            foreach (var parameter in symbol.Signature.Parameters)
            {
                parameter.Type.Accept(this);
            }

            symbol.Signature.ReturnType.Accept(this);
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
