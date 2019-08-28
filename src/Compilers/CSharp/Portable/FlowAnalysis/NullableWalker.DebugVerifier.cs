// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class NullableWalker
    {
#if DEBUG
        /// <summary>
        /// Verifies that all BoundExpressions in a tree have been visited by the nullable walker and have recorded updated types and nullabilities for rewriting purposes.
        /// </summary>
        private sealed class DebugVerifier : BoundTreeWalker
        {
            private readonly ImmutableDictionary<BoundExpression, (NullabilityInfo Info, TypeSymbol Type)> _analyzedNullabilityMap;
            private readonly ImmutableDictionary<(BoundNode, Symbol), Symbol> _updatedMethodSymbols;
            private readonly SnapshotManager _snapshotManager;
            private readonly HashSet<BoundExpression> _visitedExpressions = new HashSet<BoundExpression>();
            private int _recursionDepth;

            private DebugVerifier(ImmutableDictionary<BoundExpression, (NullabilityInfo Info, TypeSymbol Type)> analyzedNullabilityMap, ImmutableDictionary<(BoundNode, Symbol), Symbol> updatedMethodSymbols, SnapshotManager snapshotManager)
            {
                _analyzedNullabilityMap = analyzedNullabilityMap;
                _snapshotManager = snapshotManager;
                _updatedMethodSymbols = updatedMethodSymbols;
            }

            protected override bool ConvertInsufficientExecutionStackExceptionToCancelledByStackGuardException()
            {
                return false; // Same behavior as NullableWalker
            }

            public static void Verify(ImmutableDictionary<BoundExpression, (NullabilityInfo Info, TypeSymbol Type)> analyzedNullabilityMap, ImmutableDictionary<(BoundNode, Symbol), Symbol> updatedMethodSymbols, SnapshotManager snapshotManagerOpt, BoundNode node)
            {
                var verifier = new DebugVerifier(analyzedNullabilityMap, updatedMethodSymbols, snapshotManagerOpt);
                verifier.Visit(node);

                foreach (var ((expr, originalSymbol), updatedSymbol) in updatedMethodSymbols)
                {
                    Debug.Assert(originalSymbol.Equals(updatedSymbol, TypeCompareKind.AllNullableIgnoreOptions | TypeCompareKind.IgnoreTupleNames), @$"Symbol for `{expr.Syntax}` changed:
Was {originalSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}
Now {updatedSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
                }

                // Can't just remove nodes from _analyzedNullabilityMap and verify no nodes remaining because nodes can be reused.
                Debug.Assert(verifier._analyzedNullabilityMap.Count == verifier._visitedExpressions.Count, $"Visited {verifier._visitedExpressions.Count} nodes, expected to visit {verifier._analyzedNullabilityMap.Count}");
            }

            private void VerifyExpression(BoundExpression expression, bool overrideSkippedExpression = false)
            {
                if (overrideSkippedExpression || !s_skippedExpressions.Contains(expression.Kind))
                {
                    Debug.Assert(_analyzedNullabilityMap.ContainsKey(expression), $"Did not find {expression} `{expression.Syntax}` in the map.");
                    _visitedExpressions.Add(expression);
                }
            }

            protected override BoundExpression? VisitExpressionWithoutStackGuard(BoundExpression node)
            {
                VerifyExpression(node);
                return (BoundExpression)base.Visit(node);
            }

            public override BoundNode? Visit(BoundNode? node)
            {
                // Ensure that we always have a snapshot for every BoundExpression in the map
                // Re-enable of this assert is tracked by https://github.com/dotnet/roslyn/issues/36844
                //if (_snapshotManager != null && node != null)
                //{
                //    _snapshotManager.VerifyNode(node);
                //}

                if (node is BoundExpression expr)
                {
                    return VisitExpressionWithStackGuard(ref _recursionDepth, expr);
                }
                return base.Visit(node);
            }

            public override BoundNode? VisitCall(BoundCall node)
            {
                Debug.Assert(_updatedMethodSymbols.ContainsKey((node, node.Method)), $"Did not find updated method symbol for {node} `{node.Syntax}`.");
                return base.VisitCall(node);
            }

            public override BoundNode? VisitDeconstructionAssignmentOperator(BoundDeconstructionAssignmentOperator node)
            {
                // https://github.com/dotnet/roslyn/issues/35010: handle
                return null;
            }

            public override BoundNode? VisitBadExpression(BoundBadExpression node)
            {
                // Regardless of what the BadExpression is, we need to add all of its direct children to the visited map. They
                // could be things like object initializers (see New_01.F1).
                foreach (var child in node.ChildBoundNodes)
                {
                    if (!s_skippedExpressions.Contains(child.Kind))
                    {
                        Visit(child);
                    }
                    else
                    {
                        VerifyExpression(child, overrideSkippedExpression: true);
                    }
                }

                return null;
            }

            public override BoundNode? VisitQueryClause(BoundQueryClause node)
            {
                Visit(node.UnoptimizedForm ?? node.Value);
                return null;
            }

            public override BoundNode? VisitUnboundLambda(UnboundLambda node)
            {
                Visit(node.BindForErrorRecovery().Body);
                return null;
            }

            public override BoundNode? VisitForEachStatement(BoundForEachStatement node)
            {
                Visit(node.IterationVariableType);
                Visit(node.Expression);
                // https://github.com/dotnet/roslyn/issues/35010: handle the deconstruction
                //this.Visit(node.DeconstructionOpt);
                Visit(node.Body);
                return null;
            }

            public override BoundNode? VisitGotoStatement(BoundGotoStatement node)
            {
                // There's no need to verify the label children. They do not have types or nullabilities
                return null;
            }

            public override BoundNode? VisitTypeOrValueExpression(BoundTypeOrValueExpression node)
            {
                Visit(node.Data.ValueExpression);
                return base.VisitTypeOrValueExpression(node);
            }

            public override BoundNode? VisitDynamicCollectionElementInitializer(BoundDynamicCollectionElementInitializer node)
            {
                // https://github.com/dotnet/roslyn/issues/33441 dynamic collection initializers aren't being handled correctly
                VerifyExpression(node, overrideSkippedExpression: true);
                return null;
            }

            public override BoundNode? VisitBinaryOperator(BoundBinaryOperator node)
            {
                VisitBinaryOperatorChildren(node);
                return null;
            }

            public override BoundNode? VisitUserDefinedConditionalLogicalOperator(BoundUserDefinedConditionalLogicalOperator node)
            {
                VisitBinaryOperatorChildren(node);
                return null;
            }

            private void VisitBinaryOperatorChildren(BoundBinaryOperatorBase node)
            {
                // There can be deep recursion on the left side, so verify iteratively to avoid blowing the stack
                while (true)
                {
                    VerifyExpression(node);

                    Visit(node.Right);

                    if (!(node.Left is BoundBinaryOperatorBase child))
                    {
                        Visit(node.Left);
                        return;
                    }

                    node = child;
                }
            }

            public override BoundNode? VisitConvertedTupleLiteral(BoundConvertedTupleLiteral node)
            {
                Visit(node.SourceTuple);
                return base.VisitConvertedTupleLiteral(node);
            }

            public override BoundNode? VisitTypeExpression(BoundTypeExpression node)
            {
                // Ignore any dimensions
                VerifyExpression(node);
                Visit(node.BoundContainingTypeOpt);
                return null;
            }
        }
#endif
    }
}
