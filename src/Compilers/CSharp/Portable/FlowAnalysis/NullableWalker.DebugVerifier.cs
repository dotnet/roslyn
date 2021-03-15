﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

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
            private readonly ImmutableDictionary<BoundExpression, (NullabilityInfo Info, TypeSymbol? Type)> _analyzedNullabilityMap;
            private readonly SnapshotManager? _snapshotManager;
            private readonly HashSet<BoundExpression> _visitedExpressions = new HashSet<BoundExpression>();
            private int _recursionDepth;

            private DebugVerifier(ImmutableDictionary<BoundExpression, (NullabilityInfo Info, TypeSymbol? Type)> analyzedNullabilityMap, SnapshotManager? snapshotManager)
            {
                _analyzedNullabilityMap = analyzedNullabilityMap;
                _snapshotManager = snapshotManager;
            }

            protected override bool ConvertInsufficientExecutionStackExceptionToCancelledByStackGuardException()
            {
                return false; // Same behavior as NullableWalker
            }

            public static void Verify(ImmutableDictionary<BoundExpression, (NullabilityInfo Info, TypeSymbol? Type)> analyzedNullabilityMap, SnapshotManager? snapshotManagerOpt, BoundNode node)
            {
                var verifier = new DebugVerifier(analyzedNullabilityMap, snapshotManagerOpt);
                verifier.Visit(node);
                snapshotManagerOpt?.VerifyUpdatedSymbols();

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
                Visit(node.AwaitOpt);
                if (node.EnumeratorInfoOpt != null)
                {
                    Visit(node.EnumeratorInfoOpt.DisposeAwaitableInfo);
                    if (node.EnumeratorInfoOpt.GetEnumeratorInfo.Method.IsExtensionMethod)
                    {
                        foreach (var arg in node.EnumeratorInfoOpt.GetEnumeratorInfo.Arguments)
                        {
                            Visit(arg);
                        }
                    }
                }
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

            public override BoundNode? VisitAssignmentOperator(BoundAssignmentOperator node)
            {
                // We're not correctly visiting the right side of object creation initializers when
                // the symbol is null (such as for dynamic)
                // https://github.com/dotnet/roslyn/issues/45088
                if (node.Left is BoundObjectInitializerMember { MemberSymbol: null })
                {
                    VerifyExpression(node);
                    Visit(node.Left);
                    return null;
                }

                return base.VisitAssignmentOperator(node);
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

            public override BoundNode? VisitSwitchExpressionArm(BoundSwitchExpressionArm node)
            {
                this.Visit(node.Pattern);
                // If the constant value of a when clause is true, it can be skipped by the dag
                // generator as an optimization. In that case, it's a value type and will be set
                // as not nullable in the output.
                if (node.WhenClause?.ConstantValue != ConstantValue.True)
                {
                    this.Visit(node.WhenClause);
                }
                this.Visit(node.Value);
                return null;
            }

            public override BoundNode? VisitNoPiaObjectCreationExpression(BoundNoPiaObjectCreationExpression node)
            {
                // We're not handling nopia object creations correctly
                // https://github.com/dotnet/roslyn/issues/45082
                if (node.InitializerExpressionOpt is object)
                {
                    VerifyExpression(node.InitializerExpressionOpt, overrideSkippedExpression: true);
                }
                return null;
            }
        }
#endif
    }
}
