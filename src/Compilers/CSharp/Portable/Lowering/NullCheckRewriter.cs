// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class NullCheckRewriter : BoundTreeRewriterWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
    {
        private readonly MethodSymbol _method;
        private readonly DiagnosticBag _diagnostics;
        private readonly SyntheticBoundNodeFactory _fact;
        private NullCheckRewriter(
            MethodSymbol method,
            SyntaxNode syntax,
            TypeCompilationState compilationState,
            DiagnosticBag diagnostics)
        {
            _method = method;
            _diagnostics = diagnostics;
            _fact = new SyntheticBoundNodeFactory(method, syntax, compilationState, diagnostics);
        }

        internal static BoundStatement Rewrite(
            BoundStatement body,
            MethodSymbol method,
            TypeCompilationState compilationState,
            DiagnosticBag diagnostics)
        {
            if (!method.Parameters.Any(x => x is SourceParameterSymbolBase param
                                            && param.IsNullChecked))
            {
                return body;
            }

            var rewriter = new NullCheckRewriter(method, body.Syntax, compilationState, diagnostics);
            return (BoundStatement)rewriter.Visit(body);
        }

        public override BoundNode VisitBlock(BoundBlock node)
        {
            return AddNullChecksToBody(node);
        }

        private BoundNode AddNullChecksToBody(BoundBlock body)
        {
            var statementList = ArrayBuilder<BoundStatement>.GetInstance();
            foreach (ParameterSymbol x in _method.Parameters)
            {
                if (x is SourceParameterSymbolBase param
                    && param.IsNullChecked)
                {
                    if (param.Type.IsValueType && !param.Type.IsNullableType())
                    {
                        continue;
                    }
                    var constructedIf = ConstructIfStatementForParameter(body, param);
                    statementList.Add(constructedIf);
                }
            }
            statementList.AddRange(body.Statements);

            return _fact.Block(body.Locals, statementList.ToImmutableAndFree());
        }

        private BoundStatement ConstructIfStatementForParameter(BoundBlock body, SourceParameterSymbolBase parameter)
        {
            var loweredLeft = _fact.Parameter(parameter);
            var loweredRight = _fact.Literal(ConstantValue.Null, parameter.Type);
            BoundExpression paramIsNullCondition = parameter.Type.IsNullableType()
                                                        ? BoundCall.Synthesized(body.Syntax,
                                                            loweredLeft,
                                                            LocalRewriter.UnsafeGetNullableMethod(
                                                                body.Syntax,
                                                                loweredLeft.Type as NamedTypeSymbol,
                                                                SpecialMember.System_Nullable_T_get_HasValue,
                                                                _method.DeclaringCompilation,
                                                                _diagnostics)
                                                            );
                                                        :_fact.ObjectEqual(loweredLeft, loweredRight);


            // PROTOTYPE : Make ArgumentNullException
            BoundThrowStatement throwArgNullStatement = _fact.Throw(_fact.New(_fact.WellKnownType(WellKnownType.System_Exception)));
            return _fact.HiddenSequencePoint(_fact.If(paramIsNullCondition, throwArgNullStatement));
        }
    }
}
