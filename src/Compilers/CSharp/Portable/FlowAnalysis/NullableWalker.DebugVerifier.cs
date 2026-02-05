// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                if (verifier._analyzedNullabilityMap.Count > verifier._visitedExpressions.Count)
                {
                    foreach (var analyzedNode in verifier._analyzedNullabilityMap.Keys)
                    {
                        if (!verifier._visitedExpressions.Contains(analyzedNode))
                        {
                            RoslynDebug.Assert(false, $"Analyzed {verifier._analyzedNullabilityMap.Count} nodes in NullableWalker, but DebugVerifier expects {verifier._visitedExpressions.Count}. Example of unverified node: {analyzedNode.GetDebuggerDisplay()}");
                        }
                    }
                }
                else if (verifier._analyzedNullabilityMap.Count < verifier._visitedExpressions.Count)
                {
                    foreach (var verifiedNode in verifier._visitedExpressions)
                    {
                        if (!verifier._analyzedNullabilityMap.ContainsKey(verifiedNode))
                        {
                            RoslynDebug.Assert(false, $"Analyzed {verifier._analyzedNullabilityMap.Count} nodes in NullableWalker, but DebugVerifier expects {verifier._visitedExpressions.Count}. Example of unanalyzed node: {verifiedNode.GetDebuggerDisplay()}");
                        }
                    }
                }
            }

            private void VerifyExpression(BoundExpression expression, bool overrideSkippedExpression = false)
            {
                if (expression.IsParamsArrayOrCollection)
                {
                    // Params collections are processed element wise. 
                    RoslynDebug.Assert(!_analyzedNullabilityMap.ContainsKey(expression), $"Found unexpected {expression} `{expression.Syntax}` in the map.");
                }
                else if (overrideSkippedExpression || !s_skippedExpressions.Contains(expression.Kind))
                {
                    RoslynDebug.Assert(_analyzedNullabilityMap.ContainsKey(expression), $"Did not find {expression} `{expression.Syntax}` in the map.");
                    _visitedExpressions.Add(expression);
                }
            }

            protected override BoundNode? VisitExpressionOrPatternWithoutStackGuard(BoundNode node)
            {
                if (node is BoundExpression expr)
                {
                    VerifyExpression(expr);
                }
                return base.Visit(node);
            }

            public override BoundNode? Visit(BoundNode? node)
            {
                // Ensure that we always have a snapshot for every BoundExpression in the map
                // Re-enable of this assert is tracked by https://github.com/dotnet/roslyn/issues/36844
                //if (_snapshotManager != null && node != null)
                //{
                //    _snapshotManager.VerifyNode(node);
                //}

                if (node is BoundExpression or BoundPattern)
                {
                    return VisitExpressionOrPatternWithStackGuard(ref _recursionDepth, node);
                }
                return base.Visit(node);
            }

            public override BoundNode? VisitArrayCreation(BoundArrayCreation node)
            {
                if (node.IsParamsArrayOrCollection)
                {
                    // Synthesized params array is processed element wise.
                    this.Visit(node.InitializerOpt);
                    return null;
                }

                return base.VisitArrayCreation(node);
            }

            public override BoundNode? VisitCollectionExpression(BoundCollectionExpression node)
            {
                if (node.IsParamsArrayOrCollection)
                {
                    // Synthesized params collection is processed element wise.
                    this.VisitList(node.UnconvertedCollectionExpression.Elements);
                    return null;
                }

                bool hasElementType = node.CollectionTypeKind is not CollectionExpressionTypeKind.None;
                foreach (var element in node.Elements)
                {
                    if (element is BoundCollectionExpressionSpreadElement spread)
                    {
                        Visit(spread.Expression);
                        Visit(spread.Conversion);
                        if (spread.EnumeratorInfoOpt != null)
                        {
                            VisitForEachEnumeratorInfo(spread.EnumeratorInfoOpt);
                        }
                        if (hasElementType)
                        {
                            Visit(((BoundExpressionStatement?)spread.IteratorBody)?.Expression);
                        }
                    }
                    else
                    {
                        Visit(element);
                    }
                }
                return null;
            }

            public override BoundNode? VisitCollectionExpressionSpreadElement(BoundCollectionExpressionSpreadElement node)
            {
                Visit(node.Expression);

                if (node.Conversion is BoundConversion conversion)
                {
                    Visit(conversion);
                }

                return null;
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

            public override BoundNode? VisitIfStatement(BoundIfStatement node)
            {
                while (true)
                {
                    this.Visit(node.Condition);
                    this.Visit(node.Consequence);

                    var alternative = node.AlternativeOpt;
                    if (alternative is null)
                    {
                        break;
                    }

                    if (alternative is BoundIfStatement elseIfStatement)
                    {
                        node = elseIfStatement;
                    }
                    else
                    {
                        this.Visit(alternative);
                        break;
                    }
                }

                return null;
            }

            public override BoundNode? VisitForEachStatement(BoundForEachStatement node)
            {
                Visit(node.IterationVariableType);
                if (node.EnumeratorInfoOpt != null)
                {
                    VisitForEachEnumeratorInfo(node.EnumeratorInfoOpt);
                }
                Visit(node.Expression);
                // https://github.com/dotnet/roslyn/issues/35010: handle the deconstruction
                //this.Visit(node.DeconstructionOpt);
                Visit(node.Body);
                return null;
            }

            private void VisitForEachEnumeratorInfo(ForEachEnumeratorInfo enumeratorInfo)
            {
                Visit(enumeratorInfo.MoveNextAwaitableInfo);
                Visit(enumeratorInfo.DisposeAwaitableInfo);
                if (enumeratorInfo.GetEnumeratorInfo.Method.IsExtensionMethod) // Tracked by https://github.com/dotnet/roslyn/issues/78828: Test this code path with new extensions
                {
                    foreach (var arg in enumeratorInfo.GetEnumeratorInfo.Arguments)
                    {
                        Visit(arg);
                    }
                }
            }

            public override BoundNode? VisitGotoStatement(BoundGotoStatement node)
            {
                // There's no need to verify the label children. They do not have types or nullabilities
                return null;
            }

            public override BoundNode? VisitTypeOrValueExpression(BoundTypeOrValueExpression node)
            {
                Debug.Assert(node is not BoundTypeOrValueExpression, "The Binder is expected to resolve the member access in the most appropriate way, even in an error scenario.");
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

            public override BoundNode? VisitCompoundAssignmentOperator(BoundCompoundAssignmentOperator node)
            {
                if (node.LeftConversion is BoundConversion leftConversion &&
                    !(node.Operator.Method is { IsStatic: false } method && method.IsExtensionBlockMember()))
                {
                    VerifyExpression(leftConversion);
                }

                Visit(node.Left);
                Visit(node.Right);
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

            public override BoundNode? VisitBinaryPattern(BoundBinaryPattern node)
            {
                // There can be deep recursion on the left side, so verify iteratively to avoid blowing the stack
                while (true)
                {
                    Visit(node.Right);

                    if (node.Left is not BoundBinaryPattern child)
                    {
                        Visit(node.Left);
                        return null;
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

            public override BoundNode? VisitListPattern(BoundListPattern node)
            {
                VisitList(node.Subpatterns);
                Visit(node.VariableAccess);
                if (!node.HasErrors &&
                    node.IndexerAccess is BoundIndexerAccess { Indexer: { } indexer } indexerAccess &&
                    indexer.IsExtensionBlockMember())
                {
                    VisitList(indexerAccess.Arguments);
                }
                return null;
            }

            public override BoundNode? VisitSlicePattern(BoundSlicePattern node)
            {
                this.Visit(node.Pattern);
                // Ignore indexer access (just a node to hold onto some symbols)
                return null;
            }

            public override BoundNode? VisitSwitchExpressionArm(BoundSwitchExpressionArm node)
            {
                this.Visit(node.Pattern);
                // If the constant value of a when clause is true, it can be skipped by the dag
                // generator as an optimization. In that case, it's a value type and will be set
                // as not nullable in the output.
                if (node.WhenClause?.ConstantValueOpt != ConstantValue.True)
                {
                    this.Visit(node.WhenClause);
                }
                this.Visit(node.Value);
                return null;
            }

            public override BoundNode? VisitUnconvertedObjectCreationExpression(BoundUnconvertedObjectCreationExpression node)
            {
                // These nodes are only involved in return type inference for unbound lambdas. We don't analyze their subnodes, and no
                // info is exposed to consumers.
                return null;
            }

            public override BoundNode? VisitImplicitIndexerAccess(BoundImplicitIndexerAccess node)
            {
                Visit(node.Receiver);
                Visit(node.Argument);
                Visit(node.IndexerOrSliceAccess);
                return null;
            }

            public override BoundNode? VisitConversion(BoundConversion node)
            {
                if (node.ConversionKind == ConversionKind.InterpolatedStringHandler)
                {
                    Visit(node.Operand.GetInterpolatedStringHandlerData().Construction);
                }
                else if (node.IsParamsArrayOrCollection)
                {
                    // Synthesized params collection is processed element wise.
                    this.Visit(node.Operand);
                    return null;
                }

                return base.VisitConversion(node);
            }
        }
#endif
    }
}
