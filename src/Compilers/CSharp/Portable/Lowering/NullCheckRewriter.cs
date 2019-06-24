// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Linq;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        internal BoundStatement RewriteNullChecking(BoundStatement body)
        {
            if (_factory.CurrentFunction.Parameters.Any(x => x is SourceParameterSymbolBase param
                                            && param.IsNullChecked)
                && ((BoundStatement)Visit(body) is BoundBlock block))
            {
                return (BoundStatement)AddNullChecksToBody(block);
            }
            return body;
        }

        private BoundNode AddNullChecksToBody(BoundBlock body)
        {
            var statementList = ArrayBuilder<BoundStatement>.GetInstance();
            foreach (ParameterSymbol x in _factory.TopLevelMethod.Parameters)
            {
                if (x is SourceParameterSymbolBase param
                    && param.IsNullChecked)
                {
                    if (param.Type.IsValueType && !param.Type.IsNullableTypeOrTypeParameter())
                    {
                        // PROTOTYPE : Warning or Error, see CodeGenNullCheckedParameterTests.TestNullCheckedSubstitution2
                        continue;
                    }
                    var constructedIf = ConstructIfStatementForParameter(body, param);
                    statementList.Add(constructedIf);
                }
            }
            statementList.AddRange(body.Statements);

            return _factory.Block(body.Locals, statementList.ToImmutableAndFree());
        }

        private BoundStatement ConstructIfStatementForParameter(BoundBlock body, SourceParameterSymbolBase parameter)
        {
            BoundExpression paramIsNullCondition;
            var loweredLeft = _factory.Parameter(parameter);
            var loweredRight = _factory.Literal(ConstantValue.Null, parameter.Type);

            if (loweredLeft.Type.IsNullableType())
            {
                paramIsNullCondition = MakeNullableHasValue(body.Syntax, loweredLeft);
            }
            else
            {
                paramIsNullCondition = MakeNullCheck(body.Syntax, loweredLeft, BinaryOperatorKind.Equal);
                //paramIsNullCondition = _factory.ObjectEqual(_factory.Convert(_factory.SpecialType(SpecialType.System_Object), loweredLeft), loweredRight);
            }
            // PROTOTYPE : Make ArgumentNullException
            BoundThrowStatement throwArgNullStatement = _factory.Throw(_factory.New(_factory.WellKnownType(WellKnownType.System_Exception)));
            return _factory.HiddenSequencePoint(_factory.If(paramIsNullCondition, throwArgNullStatement));
        }
    }
}
