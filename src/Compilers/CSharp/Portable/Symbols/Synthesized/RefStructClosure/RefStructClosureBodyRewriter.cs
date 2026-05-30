// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Helpers for lowering lambdas that target a ref struct closure type (csharplang#10209).
    /// </summary>
    internal static class RefStructClosureCaptureAnalyzer
    {
        /// <summary>
        /// Walks the lambda body to find references to outer locals and parameters. Anything
        /// declared inside the body (including the lambda's own parameters) is excluded.
        /// </summary>
        internal static (ImmutableArray<LocalSymbol> capturedLocals, ImmutableArray<ParameterSymbol> capturedParameters) Analyze(BoundLambda lambda)
        {
            var walker = new Walker(lambda.Symbol);
            walker.Visit(lambda.Body);
            return walker.GetCaptures();
        }

        private sealed class Walker : BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
        {
            private readonly HashSet<ParameterSymbol> _lambdaParameters;
            private readonly HashSet<LocalSymbol> _declaredLocals = new HashSet<LocalSymbol>();
            private readonly SetWithInsertionOrder<LocalSymbol> _capturedLocals = new SetWithInsertionOrder<LocalSymbol>();
            private readonly SetWithInsertionOrder<ParameterSymbol> _capturedParameters = new SetWithInsertionOrder<ParameterSymbol>();

            internal Walker(MethodSymbol lambda)
            {
                _lambdaParameters = new HashSet<ParameterSymbol>(lambda.Parameters);
            }

            internal (ImmutableArray<LocalSymbol>, ImmutableArray<ParameterSymbol>) GetCaptures()
                => (_capturedLocals.AsImmutable(), _capturedParameters.AsImmutable());

            public override BoundNode Visit(BoundNode node)
            {
                if (node is BoundLocalDeclaration { LocalSymbol: { } local })
                {
                    _declaredLocals.Add(local);
                }
                else if (node is BoundCatchBlock { Locals: { } catchLocals })
                {
                    foreach (var l in catchLocals)
                    {
                        _declaredLocals.Add(l);
                    }
                }
                else if (node is BoundBlock { Locals: { } blockLocals })
                {
                    foreach (var l in blockLocals)
                    {
                        _declaredLocals.Add(l);
                    }
                }
                else if (node is BoundSequence { Locals: { } seqLocals })
                {
                    foreach (var l in seqLocals)
                    {
                        _declaredLocals.Add(l);
                    }
                }

                return base.Visit(node);
            }

            public override BoundNode VisitLocal(BoundLocal node)
            {
                var local = node.LocalSymbol;
                if (!_declaredLocals.Contains(local))
                {
                    _capturedLocals.Add(local);
                }
                return base.VisitLocal(node);
            }

            public override BoundNode VisitParameter(BoundParameter node)
            {
                var p = node.ParameterSymbol;
                if (!_lambdaParameters.Contains(p))
                {
                    _capturedParameters.Add(p);
                }
                return base.VisitParameter(node);
            }
        }
    }

    /// <summary>
    /// Rewrites a lambda body so it can serve as the synthesized closure's <c>Invoke</c> method
    /// body: lambda parameters are replaced with the Invoke method's parameters, and captured
    /// outer locals/parameters are replaced with field accesses on the closure receiver.
    /// </summary>
    internal sealed class RefStructClosureBodyRewriter : BoundTreeRewriterWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
    {
        private readonly ParameterSymbol _thisParameter;
        private readonly Dictionary<ParameterSymbol, ParameterSymbol> _lambdaParameterMap;
        private readonly Dictionary<LocalSymbol, SynthesizedRefStructClosureCaptureField> _localCaptureFields;
        private readonly Dictionary<ParameterSymbol, SynthesizedRefStructClosureCaptureField> _parameterCaptureFields;

        internal RefStructClosureBodyRewriter(
            ParameterSymbol thisParameter,
            Dictionary<ParameterSymbol, ParameterSymbol> lambdaParameterMap,
            Dictionary<LocalSymbol, SynthesizedRefStructClosureCaptureField> localCaptureFields,
            Dictionary<ParameterSymbol, SynthesizedRefStructClosureCaptureField> parameterCaptureFields)
        {
            _thisParameter = thisParameter;
            _lambdaParameterMap = lambdaParameterMap;
            _localCaptureFields = localCaptureFields;
            _parameterCaptureFields = parameterCaptureFields;
        }

        public override BoundNode VisitLocal(BoundLocal node)
        {
            if (_localCaptureFields.TryGetValue(node.LocalSymbol, out var field))
            {
                return MakeFieldAccess(node, field);
            }

            return base.VisitLocal(node);
        }

        public override BoundNode VisitParameter(BoundParameter node)
        {
            if (_lambdaParameterMap.TryGetValue(node.ParameterSymbol, out var mapped))
            {
                return new BoundParameter(node.Syntax, mapped, node.HasErrors) { WasCompilerGenerated = true };
            }

            if (_parameterCaptureFields.TryGetValue(node.ParameterSymbol, out var field))
            {
                return MakeFieldAccess(node, field);
            }

            return base.VisitParameter(node);
        }

        private BoundFieldAccess MakeFieldAccess(BoundExpression original, SynthesizedRefStructClosureCaptureField field)
        {
            var receiver = new BoundParameter(original.Syntax, _thisParameter) { WasCompilerGenerated = true };
            return new BoundFieldAccess(original.Syntax, receiver, field, constantValueOpt: null) { WasCompilerGenerated = true };
        }
    }
}
