// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        delegate BoundBlock LambdaBodyResolver(LambdaSymbol lambdaSymbol, ExecutableCodeBinder lambdaBodyBinder, DiagnosticBag diagnostics);

        private class QueryUnboundLambdaState : UnboundLambdaState
        {
            private readonly ImmutableArray<RangeVariableSymbol> parameters;
            private readonly LambdaBodyResolver bodyResolver;
            private readonly RangeVariableMap rangeVariableMap;

            public QueryUnboundLambdaState(UnboundLambda unbound, Binder binder, RangeVariableMap rangeVariableMap, ImmutableArray<RangeVariableSymbol> parameters, LambdaBodyResolver bodyResolver)
                : base(unbound, binder)
            {
                this.parameters = parameters;
                this.bodyResolver = bodyResolver;
                this.rangeVariableMap = rangeVariableMap;
            }

            public QueryUnboundLambdaState(UnboundLambda unbound, Binder binder, RangeVariableMap rangeVariableMap, ImmutableArray<RangeVariableSymbol> parameters, ExpressionSyntax body, TypeSyntax castTypeSyntax, TypeSymbol castType)
                : this(unbound, binder, rangeVariableMap, parameters, (LambdaSymbol lambdaSymbol, ExecutableCodeBinder lambdaBodyBinder, DiagnosticBag diagnostics) =>
            {
                var expressionBinder = new ScopedExpressionBinder(lambdaBodyBinder, body);
                BoundExpression expression = expressionBinder.BindValue(body, diagnostics, BindValueKind.RValue);
                Debug.Assert((object)castType != null);
                Debug.Assert(castTypeSyntax != null);
                // We transform the expression from "expr" to "expr.Cast<castTypeOpt>()".
                expression = lambdaBodyBinder.MakeQueryInvocation(body, expression, "Cast", castTypeSyntax, castType, diagnostics);
                return lambdaBodyBinder.WrapExpressionLambdaBody(expressionBinder.Locals, expression, body, diagnostics);
            })
            { }

            public QueryUnboundLambdaState(UnboundLambda unbound, Binder binder, RangeVariableMap rangeVariableMap, ImmutableArray<RangeVariableSymbol> parameters, ExpressionSyntax body)
                : this(unbound, binder, rangeVariableMap, parameters, (LambdaSymbol lambdaSymbol, ExecutableCodeBinder lambdaBodyBinder, DiagnosticBag diagnostics) =>
            {
                return lambdaBodyBinder.BindExpressionLambdaBody(body, diagnostics);
            })
            { }

            internal void SetUnboundLambda(UnboundLambda unbound)
            {
                Debug.Assert(base.unboundLambda == null);
                base.unboundLambda = unbound;
            }

            public override string ParameterName(int index) { return parameters[index].Name; }
            public override bool HasSignature { get { return true; } }
            public override bool HasExplicitlyTypedParameterList { get { return false; } }
            public override int ParameterCount { get { return parameters.Length; } }
            public override bool IsAsync { get { return false; } }
            public override RefKind RefKind(int index) { return Microsoft.CodeAnalysis.RefKind.None; }
            public override MessageID MessageID { get { return MessageID.IDS_FeatureQueryExpression; } } // TODO: what is the correct ID here?
            public override Location ParameterLocation(int index) { return parameters[index].Locations[0]; }
            public override TypeSymbol ParameterType(int index) { throw new ArgumentException(); } // implicitly typed

            public override void GenerateAnonymousFunctionConversionError(DiagnosticBag diagnostics, TypeSymbol targetType)
            {
                // TODO: improved diagnostics for query expressions
                base.GenerateAnonymousFunctionConversionError(diagnostics, targetType);
            }

            public override Binder ParameterBinder(LambdaSymbol lambdaSymbol, Binder binder)
            {
                return new WithQueryLambdaParametersBinder(lambdaSymbol, rangeVariableMap, binder);
            }

            protected override BoundBlock BindLambdaBody(LambdaSymbol lambdaSymbol, ExecutableCodeBinder lambdaBodyBinder, DiagnosticBag diagnostics)
            {
                return bodyResolver(lambdaSymbol, lambdaBodyBinder, diagnostics);
            }
        }
    }
}