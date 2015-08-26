// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal abstract partial class MethodExtractor
    {
        protected class TypeParameterCollector : SymbolVisitor
        {
            private readonly List<ITypeParameterSymbol> _typeParameters = new List<ITypeParameterSymbol>();

            public static IEnumerable<ITypeParameterSymbol> Collect(ITypeSymbol typeSymbol)
            {
                var collector = new TypeParameterCollector();
                typeSymbol.Accept(collector);

                return collector._typeParameters;
            }

            public override void DefaultVisit(ISymbol node)
            {
                throw new NotImplementedException();
            }

            public override void VisitDynamicType(IDynamicTypeSymbol dynamicTypeSymbol)
            {
            }

            public override void VisitArrayType(IArrayTypeSymbol arrayTypeSymbol)
            {
                arrayTypeSymbol.ElementType.Accept(this);
            }

            public override void VisitPointerType(IPointerTypeSymbol pointerTypeSymbol)
            {
                pointerTypeSymbol.PointedAtType.Accept(this);
            }

            public override void VisitNamedType(INamedTypeSymbol namedTypeSymbol)
            {
                foreach (var argument in namedTypeSymbol.GetAllTypeArguments())
                {
                    argument.Accept(this);
                }
            }

            public override void VisitTypeParameter(ITypeParameterSymbol typeParameterTypeSymbol)
            {
                _typeParameters.Add(typeParameterTypeSymbol);
            }
        }
    }
}
