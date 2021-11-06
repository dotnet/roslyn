// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        private BoundBlock? RewriteNullChecking(BoundBlock? block)
        {
            if (block is null)
            {
                return null;
            }

            Debug.Assert(_factory.CurrentFunction is not null);
            var statementList = TryConstructNullCheckedStatementList(_factory.CurrentFunction.Parameters, block.Statements, _factory);
            if (statementList.IsDefault)
            {
                return null;
            }
            return _factory.Block(block.Locals, statementList);
        }

        internal static ImmutableArray<BoundStatement> TryConstructNullCheckedStatementList(ImmutableArray<ParameterSymbol> parameters,
                                                                                         ImmutableArray<BoundStatement> existingStatements,
                                                                                         SyntheticBoundNodeFactory factory)
        {
            ArrayBuilder<BoundStatement>? statementList = null;
            MethodSymbol? throwIfNullMethod = null;
            foreach (ParameterSymbol param in parameters)
            {
                if (param.IsNullChecked)
                {
                    Debug.Assert(!param.Type.IsValueType || param.Type.IsNullableTypeOrTypeParameter());

                    if (statementList is null)
                    {
                        statementList = ArrayBuilder<BoundStatement>.GetInstance();

                        Debug.Assert(throwIfNullMethod is null);
                        var module = factory.ModuleBuilderOpt!;
                        var diagnosticSyntax = factory.CurrentFunction.GetNonNullSyntaxNode();
                        var diagnostics = factory.Diagnostics.DiagnosticBag;
                        Debug.Assert(diagnostics is not null);
                        throwIfNullMethod = module.EnsureThrowIfNullFunctionExists(diagnosticSyntax, factory, diagnostics);
                    }
                    Debug.Assert(throwIfNullMethod is not null);
                    var constructedIf = ConstructNullCheck(param, throwIfNullMethod, factory);
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

        private static BoundStatement ConstructNullCheck(ParameterSymbol parameter, MethodSymbol throwIfNullMethod, SyntheticBoundNodeFactory factory)
        {
            var call = factory.Call(
                receiver: null,
                throwIfNullMethod,
                arg0: factory.Convert(factory.SpecialType(SpecialType.System_Object), factory.Parameter(parameter)),
                arg1: factory.StringLiteral(parameter.Name));
            return factory.HiddenSequencePoint(factory.ExpressionStatement(call));
        }
    }
}
