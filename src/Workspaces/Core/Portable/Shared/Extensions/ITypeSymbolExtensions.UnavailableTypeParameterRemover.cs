// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class ITypeSymbolExtensions
    {
        private class UnavailableTypeParameterRemover : SymbolVisitor<ITypeSymbol>
        {
            private readonly Compilation _compilation;
            private readonly ISet<string> _availableTypeParameterNames;

            public UnavailableTypeParameterRemover(Compilation compilation, ISet<string> availableTypeParameterNames)
            {
                _compilation = compilation;
                _availableTypeParameterNames = availableTypeParameterNames;
            }

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
                if (_availableTypeParameterNames.Contains(symbol.Name))
                {
                    return symbol;
                }

                return _compilation.ObjectType;
            }
        }
    }
}
