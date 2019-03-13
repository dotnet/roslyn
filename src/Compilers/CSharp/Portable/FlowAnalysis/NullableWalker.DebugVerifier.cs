// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
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
            private static readonly ImmutableArray<BoundKind> s_skippedExpression = ImmutableArray.Create(BoundKind.ArrayInitialization, BoundKind.ObjectInitializerExpression, BoundKind.CollectionInitializerExpression, BoundKind.DynamicCollectionElementInitializer);
            private readonly PooledDictionary<BoundExpression, (NullabilityInfo Info, TypeSymbol Type)> _analyzedNullabilityMap;
            private readonly HashSet<BoundExpression> _visitedNodes = new HashSet<BoundExpression>();
            private int _recursionDepth;

            private DebugVerifier(PooledDictionary<BoundExpression, (NullabilityInfo Info, TypeSymbol Type)> analyzedNullabilityMap)
            {
                _analyzedNullabilityMap = PooledDictionary<BoundExpression, (NullabilityInfo Info, TypeSymbol Type)>.GetInstance();
                foreach (var (key, value) in analyzedNullabilityMap)
                {
                    _analyzedNullabilityMap[key] = value;
                }
            }

            protected override bool ConvertInsufficientExecutionStackExceptionToCancelledByStackGuardException()
            {
                return false; // Same behavior as NullableWalker
            }

            public static void Verify(PooledDictionary<BoundExpression, (NullabilityInfo Info, TypeSymbol Type)> analyzedNullabilityMap, BoundNode node)
            {
                var verifier = new DebugVerifier(analyzedNullabilityMap);
                verifier.Visit(node);
                // Can't just remove nodes from _topLevelNullabilityMap because nodes can be reused.
                Debug.Assert(verifier._analyzedNullabilityMap.Count == verifier._visitedNodes.Count, $"Visited {verifier._visitedNodes.Count} nodes, expected to visit {verifier._analyzedNullabilityMap.Count}");
                verifier.Free();
            }

            private void Free()
            {
                _analyzedNullabilityMap.Free();
            }

            protected override BoundExpression VisitExpressionWithoutStackGuard(BoundExpression node)
            {
                VerifyNode(node);
                return (BoundExpression)base.Visit(node);
            }

            public override BoundNode Visit(BoundNode node)
            {
                if (node is BoundExpression expr)
                {
                    return VisitExpressionWithStackGuard(ref _recursionDepth, expr);
                }
                return base.Visit(node);
            }

            public override BoundNode VisitDeconstructionAssignmentOperator(BoundDeconstructionAssignmentOperator node)
            {
                // PROTOTYPE(nullable-api): handle
                return null;
            }

            public override BoundNode VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
            {
                // If the delegate creation was resolved to a single static method, we do not examine the
                // receiver of the method group as it's completely unused.
                if (node.Argument is BoundMethodGroup group)
                {
                    VerifyNode(group);
                    if (node.MethodOpt?.IsStatic == false)
                    {
                        Visit(group.ReceiverOpt);
                    }

                    return null;
                }
                else
                {
                    return base.VisitDelegateCreationExpression(node);
                }
            }

            public override BoundNode VisitNewT(BoundNewT node)
            {
                // https://github.com/dotnet/roslyn/issues/33387 We're not currently
                // examining child nodes correctly
                if (node.InitializerExpressionOpt != null)
                {
                    VerifyNode(node.InitializerExpressionOpt, overrideSkippedExpression: true);
                }

                return null;
            }

            public override BoundNode VisitBadExpression(BoundBadExpression node)
            {
                // Regardless of what the BadExpression is, we need to add all of its direct children to the visited map. They
                // could be things like object initializers (see New_01.F1).
                foreach (var child in node.ChildBoundNodes)
                {
                    if (!s_skippedExpression.Contains(child.Kind))
                    {
                        Visit(child);
                    }
                    else
                    {
                        VerifyNode(child, overrideSkippedExpression: true);
                    }
                }

                return null;
            }

            public override BoundNode VisitQueryClause(BoundQueryClause node)
            {
                Visit(node.UnoptimizedForm ?? node.Value);
                return null;
            }

            public override BoundNode VisitUnboundLambda(UnboundLambda node)
            {
                Visit(node.BindForErrorRecovery().Body);
                return null;
            }

            public override BoundNode VisitForEachStatement(BoundForEachStatement node)
            {
                this.Visit(node.IterationVariableType);
                this.Visit(node.Expression);
                // PROTOTYPE(nullable-api): handle the deconstruction
                //this.Visit(node.DeconstructionOpt);
                this.Visit(node.Body);
                return null;
            }

            public override BoundNode VisitGotoStatement(BoundGotoStatement node)
            {
                // There's no need to verify the label children. They do not have types or nullabilities
                return null;
            }

            public override BoundNode VisitTypeOrValueExpression(BoundTypeOrValueExpression node)
            {
                Visit(node.Data.ValueExpression);
                return base.VisitTypeOrValueExpression(node);
            }

            public override BoundNode VisitDynamicCollectionElementInitializer(BoundDynamicCollectionElementInitializer node)
            {
                // https://github.com/dotnet/roslyn/issues/33441 dynamic collection initializers aren't being handled correctly
                VerifyNode(node, overrideSkippedExpression: true);
                return null;
            }

            public override BoundNode VisitBinaryOperator(BoundBinaryOperator node)
            {
                VisitBinaryOperatorChildren(node);
                return null;
            }

            public override BoundNode VisitUserDefinedConditionalLogicalOperator(BoundUserDefinedConditionalLogicalOperator node)
            {
                VisitBinaryOperatorChildren(node);
                return null;
            }

            private void VisitBinaryOperatorChildren(BoundBinaryOperatorBase node)
            {
                // There can be deep recursion on the left side, so verify iteratively to avoid blowing the stack
                // PROTOTYPE(nullable-api): Handle in rewriter as well
                while (true)
                {
                    VerifyNode(node);

                    Visit(node.Right);

                    if (!(node.Left is BoundBinaryOperatorBase child))
                    {
                        Visit(node.Left);
                        return;
                    }

                    node = child;
                }
            }

            private void VerifyNode(BoundNode node, bool overrideSkippedExpression = false)
            {
                if (node is BoundExpression expression && (overrideSkippedExpression || !s_skippedExpression.Contains(expression.Kind)))
                {
                    Debug.Assert(_analyzedNullabilityMap.ContainsKey(expression), $"Did not find {expression} `{expression.Syntax}` in the map.");
                    TypeSymbol.Equals(expression.Type, _analyzedNullabilityMap[expression].Type, TypeCompareKind.ConsiderEverything | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes);
                    _visitedNodes.Add(expression);
                }
            }
        }
#endif
    }
}
