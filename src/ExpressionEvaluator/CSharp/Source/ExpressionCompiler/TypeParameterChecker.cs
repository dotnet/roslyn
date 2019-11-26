// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.ExpressionEvaluator;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class TypeParameterChecker : AbstractTypeParameterChecker
    {
        [Conditional("DEBUG")]
        public static void Check(Symbol symbol, ImmutableArray<TypeParameterSymbol> acceptableTypeParameters)
        {
            new TypeParameterChecker(acceptableTypeParameters).Visit(symbol.GetPublicSymbol());
        }

        [Conditional("DEBUG")]
        public static void Check(BoundNode node, ImmutableArray<TypeParameterSymbol> acceptableTypeParameters)
        {
            new BlockChecker(new TypeParameterChecker(acceptableTypeParameters)).Visit(node);
        }

        private TypeParameterChecker(ImmutableArray<TypeParameterSymbol> acceptableTypeParameters)
            : base(acceptableTypeParameters.GetPublicSymbols())
        {
        }

        public override IParameterSymbol GetThisParameter(IMethodSymbol method)
        {
            ParameterSymbol thisParameter;
            return method.GetSymbol().TryGetThisParameter(out thisParameter)
                ? thisParameter.GetPublicSymbol()
                : null;
        }

        private class BlockChecker : BoundTreeWalkerWithStackGuard
        {
            private readonly TypeParameterChecker _typeParameterChecker;

            public BlockChecker(TypeParameterChecker typeParameterChecker)
            {
                _typeParameterChecker = typeParameterChecker;
            }

            public override BoundNode Visit(BoundNode node)
            {
                if (node is BoundExpression expression)
                {
                    _typeParameterChecker.Visit(expression.ExpressionSymbol.GetPublicSymbol());
                }
                return base.Visit(node);
            }
        }
    }
}
