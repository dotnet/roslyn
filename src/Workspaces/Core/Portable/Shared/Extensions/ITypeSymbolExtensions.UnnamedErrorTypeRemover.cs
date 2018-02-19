// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal partial class ITypeSymbolExtensions
    {
        private class UnnamedErrorTypeRemover : SymbolVisitor<ITypeSymbol>
        {
            private readonly Compilation _compilation;

            public UnnamedErrorTypeRemover(Compilation compilation)
            {
                _compilation = compilation;
            }

            public override ITypeSymbol DefaultVisit(ISymbol node)
            {
                throw new NotImplementedException();
            }

            public override ITypeSymbol VisitDynamicType(IDynamicTypeSymbol symbol)
            {
                return symbol;
            }

            public override ITypeSymbol VisitArrayType(IArrayTypeSymbol symbol)
            {
                var elementType = symbol.ElementType.Accept(this);
                if (elementType != null && elementType.Equals(symbol.ElementType))
                {
                    return symbol;
                }

                return _compilation.CreateArrayTypeSymbol(elementType, symbol.Rank);
            }

            public override ITypeSymbol VisitNamedType(INamedTypeSymbol symbol)
            {
                if (symbol.IsErrorType() && symbol.Name == string.Empty)
                {
                    return _compilation.ObjectType;
                }

                var arguments = symbol.TypeArguments.Select(t => t.Accept(this)).ToArray();
                if (arguments.SequenceEqual(symbol.TypeArguments))
                {
                    return symbol;
                }

                return symbol.ConstructedFrom.Construct(arguments.ToArray());
            }

            public override ITypeSymbol VisitPointerType(IPointerTypeSymbol symbol)
            {
                var elementType = symbol.PointedAtType.Accept(this);
                if (elementType != null && elementType.Equals(symbol.PointedAtType))
                {
                    return symbol;
                }

                return _compilation.CreatePointerTypeSymbol(elementType);
            }

            public override ITypeSymbol VisitTypeParameter(ITypeParameterSymbol symbol)
            {
                return symbol;
            }
        }
    }
}
