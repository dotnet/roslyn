﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        private delegate BoundBlock LambdaBodyFactory(LambdaSymbol lambdaSymbol, Binder lambdaBodyBinder, DiagnosticBag diagnostics);

        private sealed class QueryUnboundLambdaState : UnboundLambdaState
        {
            private readonly ImmutableArray<RangeVariableSymbol> _parameters;
            private readonly LambdaBodyFactory _bodyFactory;
            private readonly RangeVariableMap _rangeVariableMap;

            public QueryUnboundLambdaState(Binder binder, RangeVariableMap rangeVariableMap, ImmutableArray<RangeVariableSymbol> parameters, LambdaBodyFactory bodyFactory, bool includeCache = true)
                : base(binder, unboundLambdaOpt: null, includeCache)
            {
                _parameters = parameters;
                _rangeVariableMap = rangeVariableMap;
                _bodyFactory = bodyFactory;
            }

            public override string ParameterName(int index) { return _parameters[index].Name; }
            public override bool ParameterIsDiscard(int index) { return false; }
            public override bool HasNames { get { return true; } }
            public override bool HasSignature { get { return true; } }
            public override bool HasExplicitlyTypedParameterList { get { return false; } }
            public override int ParameterCount { get { return _parameters.Length; } }
            public override bool IsAsync { get { return false; } }
            public override RefKind RefKind(int index) { return Microsoft.CodeAnalysis.RefKind.None; }
            public override MessageID MessageID { get { return MessageID.IDS_FeatureQueryExpression; } } // TODO: what is the correct ID here?
            public override Location ParameterLocation(int index) { return _parameters[index].Locations[0]; }
            public override TypeWithAnnotations ParameterTypeWithAnnotations(int index) { throw new ArgumentException(); } // implicitly typed

            public override void GenerateAnonymousFunctionConversionError(DiagnosticBag diagnostics, TypeSymbol targetType)
            {
                // TODO: improved diagnostics for query expressions
                base.GenerateAnonymousFunctionConversionError(diagnostics, targetType);
            }

            public override Binder ParameterBinder(LambdaSymbol lambdaSymbol, Binder binder)
            {
                return new WithQueryLambdaParametersBinder(lambdaSymbol, _rangeVariableMap, binder);
            }

            protected override UnboundLambdaState WithCachingCore(bool includeCache)
            {
                return new QueryUnboundLambdaState(Binder, _rangeVariableMap, _parameters, _bodyFactory, includeCache);
            }

            protected override BoundBlock BindLambdaBody(LambdaSymbol lambdaSymbol, Binder lambdaBodyBinder, DiagnosticBag diagnostics)
            {
                return _bodyFactory(lambdaSymbol, lambdaBodyBinder, diagnostics);
            }
        }
    }
}
