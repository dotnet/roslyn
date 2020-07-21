// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        private BoundBlock RewriteNullChecking(BoundBlock block)
        {
            if (block is null)
            {
                return null;
            }

            var statementList = ConstructNullCheckedStatementList(_factory.CurrentFunction.Parameters, block.Statements, _factory);
            if (statementList.IsDefault)
            {
                return null;
            }
            return _factory.Block(block.Locals, statementList);
        }

        internal static ImmutableArray<BoundStatement> ConstructNullCheckedStatementList(ImmutableArray<ParameterSymbol> parameters,
                                                                                         ImmutableArray<BoundStatement> existingStatements,
                                                                                         SyntheticBoundNodeFactory factory)
        {
            ArrayBuilder<BoundStatement> statementList = null;
            foreach (ParameterSymbol param in parameters)
            {
                if (param.IsNullChecked)
                {
                    Debug.Assert(!param.Type.IsValueType || param.Type.IsNullableTypeOrTypeParameter());
                    statementList ??= ArrayBuilder<BoundStatement>.GetInstance();
                    var constructedIf = ConstructIfStatementForParameter(param, factory);
                    statementList.Add(constructedIf);
                }
            }
            if (statementList is null)
            {
                return default;
            }

            statementList.AddRange(existingStatements);
            return statementList.ToImmutableAndFree();

        }

        private static BoundStatement ConstructIfStatementForParameter(ParameterSymbol parameter, SyntheticBoundNodeFactory factory)
        {
            BoundExpression paramIsNullCondition;
            var loweredLeft = factory.Parameter(parameter);

            if (loweredLeft.Type.IsNullableType())
            {
                paramIsNullCondition = factory.Not(factory.MakeNullableHasValue(loweredLeft.Syntax, loweredLeft));
            }
            else
            {
                paramIsNullCondition = factory.MakeNullCheck(loweredLeft.Syntax, loweredLeft, BinaryOperatorKind.Equal);
            }

            var argumentName = ImmutableArray.Create<BoundExpression>(factory.StringLiteral(parameter.Name));
            BoundObjectCreationExpression ex = factory.New(factory.WellKnownMethod(WellKnownMember.System_ArgumentNullException__ctorString), argumentName);
            BoundThrowStatement throwArgNullStatement = factory.Throw(ex);

            return factory.HiddenSequencePoint(factory.If(paramIsNullCondition, throwArgNullStatement));
        }
    }
}
