// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        private BoundBlock RewriteNullChecking(BoundBlock block)
        {
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
                if (GetParameterIsNullChecked(param))
                {
                    if (param.Type.IsValueType && !param.Type.IsNullableTypeOrTypeParameter())
                    {
                        factory.Diagnostics.Add(ErrorCode.ERR_NonNullableValueTypeIsNullChecked, param.Locations.FirstOrNone(), new object[] { param });
                        continue;
                    }
                    if (param.ExplicitDefaultConstantValue?.IsNull ?? false)
                    {
                        factory.Diagnostics.Add(ErrorCode.WRN_NullCheckedHasDefaultNull, param.Locations.FirstOrNone(), new object[] { param });
                    }
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

        private static bool GetParameterIsNullChecked(ParameterSymbol x)
        {
            if (x is SourceParameterSymbolBase param && param.IsNullChecked)
                return true;

            else if (x is SynthesizedParameterSymbolBase synthedParam && synthedParam.IsNullChecked)
                return true;

            return false;
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
            // PROTOTYPE : Make ArgumentNullException
            BoundThrowStatement throwArgNullStatement = factory.Throw(factory.New(factory.WellKnownType(WellKnownType.System_ArgumentNullException)));

            return factory.HiddenSequencePoint(factory.If(paramIsNullCondition, throwArgNullStatement));
        }
    }
}
