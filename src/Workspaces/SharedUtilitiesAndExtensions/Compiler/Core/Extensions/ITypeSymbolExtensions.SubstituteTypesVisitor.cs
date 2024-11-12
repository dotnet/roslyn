// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class ITypeSymbolExtensions
{
    private sealed class SubstituteTypesVisitor<TType1, TType2> : SymbolVisitor<ITypeSymbol>
        where TType1 : ITypeSymbol
        where TType2 : ITypeSymbol
    {
        // private readonly Compilation compilation;
        private readonly IDictionary<TType1, TType2> _map;
        private readonly ITypeGenerator _typeGenerator;

        internal SubstituteTypesVisitor(
            IDictionary<TType1, TType2> map,
            ITypeGenerator typeGenerator)
        {
            _map = map;
            _typeGenerator = typeGenerator;
        }

        public override ITypeSymbol DefaultVisit(ISymbol node)
            => throw new NotImplementedException();

        private ITypeSymbol VisitType(ITypeSymbol symbol)
        {
            if (symbol is TType1 && _map.TryGetValue((TType1)symbol, out var converted))
            {
                return converted;
            }

            return symbol;
        }

        public override ITypeSymbol VisitDynamicType(IDynamicTypeSymbol symbol)
            => VisitType(symbol);

        public override ITypeSymbol VisitTypeParameter(ITypeParameterSymbol symbol)
            => VisitType(symbol);

        public override ITypeSymbol VisitFunctionPointerType(IFunctionPointerTypeSymbol symbol)
        {
            // TODO(https://github.com/dotnet/roslyn/issues/43890): also visit the underlying types of
            // the parameters and return value
            return VisitType(symbol);
        }

        public override ITypeSymbol VisitNamedType(INamedTypeSymbol symbol)
        {
            var mapped = VisitType(symbol);
            if (!Equals(mapped, symbol))
            {
                return mapped;
            }

            if (symbol.IsAnonymousType)
            {
                return symbol;
            }

            // If we don't even have any type arguments, then there's nothing to do.
            var allTypeArguments = symbol.GetAllTypeArguments().ToList();
            if (allTypeArguments.Count == 0)
            {
                return symbol;
            }

            // If we have a containing type, make sure its type arguments are updated as well.
            var updatedContainingType = symbol.ContainingType?.Accept(this);

            // If our containing type changed, then find us again in the new containing type.
            if (!Equals(updatedContainingType, symbol.ContainingType))
            {
                symbol = updatedContainingType.GetTypeMembers(symbol.Name, symbol.Arity).First(m => m.TypeKind == symbol.TypeKind);
            }

            var substitutedArguments = symbol.TypeArguments.Select(t => t.Accept(this));
            if (symbol.TypeArguments.SequenceEqual(substitutedArguments))
            {
                return symbol;
            }

            return _typeGenerator.Construct(symbol.OriginalDefinition, substitutedArguments.ToArray()).WithNullableAnnotation(symbol.NullableAnnotation);
        }

        public override ITypeSymbol VisitArrayType(IArrayTypeSymbol symbol)
        {
            var mapped = VisitType(symbol);
            if (!Equals(mapped, symbol))
            {
                return mapped;
            }

            var elementType = symbol.ElementType.Accept(this);
            if (elementType != null && elementType.Equals(symbol.ElementType))
            {
                return symbol;
            }

            return _typeGenerator.CreateArrayTypeSymbol(elementType, symbol.Rank);
        }

        public override ITypeSymbol VisitPointerType(IPointerTypeSymbol symbol)
        {
            var mapped = VisitType(symbol);
            if (!Equals(mapped, symbol))
            {
                return mapped;
            }

            var pointedAtType = symbol.PointedAtType.Accept(this);
            if (pointedAtType != null && pointedAtType.Equals(symbol.PointedAtType))
            {
                return symbol;
            }

            return _typeGenerator.CreatePointerTypeSymbol(pointedAtType);
        }
    }
}
