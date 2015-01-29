// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        private delegate BoundBlock LambdaBodyFactory(LambdaSymbol lambdaSymbol, ref Binder lambdaBodyBinder, DiagnosticBag diagnostics);

        private class QueryUnboundLambdaState : UnboundLambdaState
        {
            private readonly ImmutableArray<RangeVariableSymbol> parameters;
            private readonly LambdaBodyFactory bodyFactory;
            private readonly RangeVariableMap rangeVariableMap;

            public QueryUnboundLambdaState(Binder binder, RangeVariableMap rangeVariableMap, ImmutableArray<RangeVariableSymbol> parameters, LambdaBodyFactory bodyFactory)
                : base(binder, unboundLambdaOpt: null)
            {
                this.parameters = parameters;
                this.rangeVariableMap = rangeVariableMap;
                this.bodyFactory = bodyFactory;
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

            protected override BoundBlock BindLambdaBody(LambdaSymbol lambdaSymbol, ref Binder lambdaBodyBinder, DiagnosticBag diagnostics)
            {
                return bodyFactory(lambdaSymbol, ref lambdaBodyBinder, diagnostics);
            }
        }
    }
}
