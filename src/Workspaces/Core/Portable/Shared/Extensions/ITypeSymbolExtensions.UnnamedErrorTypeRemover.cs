﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
                => _compilation = compilation;

            public override ITypeSymbol DefaultVisit(ISymbol node)
                => throw new NotImplementedException();

            public override ITypeSymbol VisitDynamicType(IDynamicTypeSymbol symbol)
                => symbol;

            public override ITypeSymbol VisitArrayType(IArrayTypeSymbol symbol)
            {
                var elementType = symbol.ElementType.Accept(this);
                if (elementType != null && elementType.Equals(symbol.ElementType))
                {
                    return symbol;
                }

                return _compilation.CreateArrayTypeSymbol(elementType, symbol.Rank);
            }

            public override ITypeSymbol VisitFunctionPointerType(IFunctionPointerTypeSymbol symbol)
            {
                // TODO(https://github.com/dotnet/roslyn/issues/43890): implement this
                return symbol;
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
                => symbol;
        }
    }
}
