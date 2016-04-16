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
            new TypeParameterChecker(acceptableTypeParameters).Visit(symbol);
        }

        [Conditional("DEBUG")]
        public static void Check(BoundNode node, ImmutableArray<TypeParameterSymbol> acceptableTypeParameters)
        {
            new BlockChecker(new TypeParameterChecker(acceptableTypeParameters)).Visit(node);
        }

        private TypeParameterChecker(ImmutableArray<TypeParameterSymbol> acceptableTypeParameters)
            : base(acceptableTypeParameters.As<ITypeParameterSymbol>())
        {
        }

        public override IParameterSymbol GetThisParameter(IMethodSymbol method)
        {
            ParameterSymbol thisParameter;
            return ((MethodSymbol)method).TryGetThisParameter(out thisParameter)
                ? thisParameter
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
                var expression = node as BoundExpression;
                if (expression != null)
                {
                    _typeParameterChecker.Visit(expression.ExpressionSymbol);
                }
                return base.Visit(node);
            }
        }
    }
}
