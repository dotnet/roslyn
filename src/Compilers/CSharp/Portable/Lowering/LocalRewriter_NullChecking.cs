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
        private BoundStatement RewriteNullChecking(BoundBlock block)
        {
            ArrayBuilder<BoundStatement> statementList = null;
            foreach (ParameterSymbol x in _factory.CurrentFunction.Parameters)
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
                    var constructedIf = ConstructIfStatementForParameter(param);
                    statementList.Add(constructedIf);
                }
            }
            if (statementList is null)
            {
                return block;
            }
            else
            {
                statementList.AddRange(block.Statements);
                return _factory.Block(block.Locals, statementList.ToImmutableAndFree());
            }
        }

        private BoundStatement ConstructIfStatementForParameter(SourceParameterSymbolBase parameter)
        {
            BoundExpression paramIsNullCondition;
            var loweredLeft = _factory.Parameter(parameter);

            if (loweredLeft.Type.IsNullableType())
            {
                paramIsNullCondition = _factory.Not(MakeNullableHasValue(loweredLeft.Syntax, loweredLeft));
            }
            else
            {
                paramIsNullCondition = MakeNullCheck(loweredLeft.Syntax, loweredLeft, BinaryOperatorKind.Equal);
            }
            // PROTOTYPE : Make ArgumentNullException
            BoundThrowStatement throwArgNullStatement = _factory.Throw(_factory.New(_factory.WellKnownType(WellKnownType.System_Exception)));

            return _factory.HiddenSequencePoint(_factory.If(paramIsNullCondition, throwArgNullStatement));
        }
    }
}
