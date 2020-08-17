// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        private BoundBlock? RewriteNullChecking(BoundBlock block)
        {
            if (block is null || _factory.CurrentFunction is null || EmitModule is null)
            {
                return null;
            }

            var statementList = TryConstructNullCheckedStatementList(_factory.CurrentFunction.Parameters,
                block.Statements,
                _factory,
                _compilation,
                EmitModule,
                _diagnostics);

            if (statementList.IsDefault)
            {
                return null;
            }
            return _factory.Block(block.Locals, statementList);
        }

        internal static ImmutableArray<BoundStatement> TryConstructNullCheckedStatementList(ImmutableArray<ParameterSymbol> parameters,
                                                                                         ImmutableArray<BoundStatement> existingStatements,
                                                                                         SyntheticBoundNodeFactory factory,
                                                                                         CSharpCompilation compilation,
                                                                                         PEModuleBuilder module,
                                                                                         DiagnosticBag diagnostics)
        {
            ArrayBuilder<BoundStatement>? statementList = null;
            foreach (ParameterSymbol param in parameters)
            {
                if (param.IsNullChecked)
                {
                    Debug.Assert(!param.Type.IsValueType || param.Type.IsNullableTypeOrTypeParameter());
                    statementList ??= ArrayBuilder<BoundStatement>.GetInstance();
                    if (ConstructIfStatementForParameter(param, factory, compilation, module, diagnostics) is BoundStatement constructedIf)
                    {
                        statementList.Add(constructedIf);
                    }
                }
            }
            if (statementList is null)
            {
                return default;
            }

            statementList.AddRange(existingStatements);
            return statementList.ToImmutableAndFree();

        }

        private static BoundStatement? ConstructIfStatementForParameter(ParameterSymbol parameter,
                                                                        SyntheticBoundNodeFactory factory,
                                                                        CSharpCompilation compilation,
                                                                        PEModuleBuilder module,
                                                                        DiagnosticBag diagnostics)
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

            if (module is null)
            {
                return null;
            }

            var argumentName = ImmutableArray.Create<BoundExpression>(factory.StringLiteral(parameter.Name));
            var privateImplClass = module.GetPrivateImplClass(loweredLeft.Syntax, diagnostics);
            var nullCheckMethod = compilation.NullCheckManager.GetNullCheckMethod(privateImplClass);

            BoundExpressionStatement callThrow = factory.ExpressionStatement(factory.Call(receiver: null, method: nullCheckMethod, args: argumentName));

            return factory.HiddenSequencePoint(factory.If(paramIsNullCondition, callThrow));
        }
    }
}
