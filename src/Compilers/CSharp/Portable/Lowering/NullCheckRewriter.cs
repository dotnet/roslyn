// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Lowering
{
    internal sealed class NullCheckRewriter : BoundTreeRewriterWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
    {
        private readonly MethodSymbol _method;
        private readonly SyntheticBoundNodeFactory _fact;
        private NullCheckRewriter(
            MethodSymbol method,
            SyntaxNode syntax,
            TypeCompilationState compilationState,
            DiagnosticBag diagnostics)
        {
            _method = method;
            _fact = new SyntheticBoundNodeFactory(method, syntax, compilationState, diagnostics);
        }

        internal static BoundStatement Rewrite(
            BoundStatement body,
            MethodSymbol method,
            TypeCompilationState compilationState,
            DiagnosticBag diagnostics)
        {
            var rewriter = new NullCheckRewriter(method, body.Syntax, compilationState, diagnostics);
            return (BoundStatement)rewriter.Visit(body);
        }

        public override BoundNode VisitBlock(BoundBlock node)
        {
            return AddNullChecksToBody(node);
        }

        private BoundNode AddNullChecksToBody(BoundBlock body)
        {
            BoundBlock prependedBody = body;
            var statementList = new List<BoundStatement>();
            foreach (SourceParameterSymbolBase param in _method.Parameters)
            {
                if (param.IsNullChecked)
                {
                    var constructedIf = ConstructIfStatementForParameter(body, param);
                    if (!(constructedIf is null))
                    {
                        statementList.Add(constructedIf);
                    }
                }
            }
            statementList.AddRange(body.Statements);

            return _fact.Block(body.Locals, statementList.ToImmutableArray());
        }

        private BoundStatement ConstructIfStatementForParameter(BoundBlock body, SourceParameterSymbolBase parameter)
        {
            BoundExpression paramIsNullCondition = _fact.ObjectEqual(_fact.Parameter(parameter), _fact.Literal(ConstantValue.Null, parameter.Type));

            // PROTOTYPE : Make ArgumentNullException
            BoundThrowStatement throwArgNullStatement = _fact.Throw(_fact.New(_fact.WellKnownType(WellKnownType.System_Exception)));

            return _fact.HiddenSequencePoint(_fact.If(paramIsNullCondition, body.Locals, throwArgNullStatement));
        }
    }
}
