// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        private BoundBlock RewriteNullChecking(BoundBlock block)
        {
            var statementList = ConstructNullCheckedStatementList(_factory.CurrentFunction.Parameters, block.Statements, _factory);
            if (statementList.IsDefaultOrEmpty)
            {
                return block;
            }
            return _factory.Block(block.Locals, statementList);
        }

        internal static ImmutableArray<BoundStatement> ConstructNullCheckedStatementList(ImmutableArray<ParameterSymbol> parameters,
                                                                                         ImmutableArray<BoundStatement> existingStatements,
                                                                                         SyntheticBoundNodeFactory factory)
        {
            ArrayBuilder<BoundStatement> statementList = null;
            foreach (ParameterSymbol x in parameters)
            {
                if (x is SourceParameterSymbolBase param
                    && param.IsNullChecked)
                {
                    if (param.Type.IsValueType && !param.Type.IsNullableTypeOrTypeParameter())
                    {
                        // PROTOTYPE : Warning or Error, see CodeGenNullCheckedParameterTests.TestNullCheckedSubstitution2
                        continue;
                    }
                    statementList ??= ArrayBuilder<BoundStatement>.GetInstance();
                    var constructedIf = ConstructIfStatementForParameter(param, factory);
                    statementList.Add(constructedIf);
                }
            }
            if (statementList is null)
            {
                return ImmutableArray<BoundStatement>.Empty;
            }

            statementList.AddRange(existingStatements);
            return statementList.ToImmutableAndFree();

        }

        private static BoundStatement ConstructIfStatementForParameter(SourceParameterSymbolBase parameter, SyntheticBoundNodeFactory factory)
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
            // PROTOTYPE : Make ArgumentNullException
            BoundThrowStatement throwArgNullStatement = factory.Throw(factory.New(factory.WellKnownType(WellKnownType.System_Exception)));

            return factory.HiddenSequencePoint(factory.If(paramIsNullCondition, throwArgNullStatement));
        }
    }
}
