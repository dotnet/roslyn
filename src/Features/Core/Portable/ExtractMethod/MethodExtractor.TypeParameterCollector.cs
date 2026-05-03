// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal abstract partial class AbstractExtractMethodService<
    TStatementSyntax,
    TExecutableStatementSyntax,
    TExpressionSyntax>
{
    internal abstract partial class MethodExtractor
    {
        protected sealed class TypeParameterCollector : SymbolVisitor
        {
            private readonly List<ITypeParameterSymbol> _typeParameters = [];

            public static IEnumerable<ITypeParameterSymbol> Collect(ITypeSymbol? typeSymbol)
            {
                var collector = new TypeParameterCollector();
                typeSymbol?.Accept(collector);

                return collector._typeParameters;
            }

            public override void DefaultVisit(ISymbol node)
                => throw new NotImplementedException();

            public override void VisitDynamicType(IDynamicTypeSymbol dynamicTypeSymbol)
            {
            }

            public override void VisitFunctionPointerType(IFunctionPointerTypeSymbol symbol)
            {
                symbol.Signature.ReturnType.Accept(this);
                foreach (var param in symbol.Signature.Parameters)
                {
                    param.Type.Accept(this);
                }
            }

            public override void VisitArrayType(IArrayTypeSymbol arrayTypeSymbol)
                => arrayTypeSymbol.ElementType.Accept(this);

            public override void VisitPointerType(IPointerTypeSymbol pointerTypeSymbol)
                => pointerTypeSymbol.PointedAtType.Accept(this);

            public override void VisitNamedType(INamedTypeSymbol namedTypeSymbol)
            {
                foreach (var argument in namedTypeSymbol.GetAllTypeArguments())
                {
                    argument.Accept(this);
                }
            }

            public override void VisitTypeParameter(ITypeParameterSymbol typeParameterTypeSymbol)
                => _typeParameters.Add(typeParameterTypeSymbol);
        }
    }
}
