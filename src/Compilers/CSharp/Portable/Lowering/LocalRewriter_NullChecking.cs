// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        internal BoundStatement RewriteNullChecking(ImmutableArray<BoundStatement> statements, ImmutableArray<LocalSymbol> locals)
        {
            if (_factory.CurrentFunction.Parameters.Any(x => x is SourceParameterSymbolBase param
                                                             && param.IsNullChecked))
            {
                return (BoundStatement)AddNullChecksToBody(statements, locals);
            }
            return _factory.Block(locals, statements);
        }

        private BoundNode AddNullChecksToBody(ImmutableArray<BoundStatement> bodyStatements, ImmutableArray<LocalSymbol> locals)
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
                    var constructedIf = ConstructIfStatementForParameter(param);
                    statementList.Add(constructedIf);
                }
            }
            statementList.AddRange(bodyStatements);

            return _factory.Block(locals, statementList.ToImmutableAndFree());
        }

        private BoundStatement ConstructIfStatementForParameter(SourceParameterSymbolBase parameter)
        {
            BoundExpression paramIsNullCondition;
            var loweredLeft = _factory.Parameter(parameter);
            var loweredRight = _factory.Literal(ConstantValue.Null, parameter.Type);

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
