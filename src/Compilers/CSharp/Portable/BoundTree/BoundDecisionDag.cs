// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundDecisionDag
    {
        private HashSet<LabelSymbol> _reachableLabels;
        private ImmutableArray<BoundDecisionDagNode> _topologicallySortedNodes;

        private static IEnumerable<BoundDecisionDagNode> Successors(BoundDecisionDagNode node)
        {
            switch (node)
            {
                case BoundEvaluationDecisionDagNode p:
                    yield return p.Next;
                    yield break;
                case BoundTestDecisionDagNode p:
                    Debug.Assert(p.WhenFalse != null);
                    yield return p.WhenFalse;
                    Debug.Assert(p.WhenTrue != null);
                    yield return p.WhenTrue;
                    yield break;
                case BoundLeafDecisionDagNode d:
                    yield break;
                case BoundWhenDecisionDagNode w:
                    Debug.Assert(w.WhenTrue != null);
                    yield return w.WhenTrue;
                    if (w.WhenFalse != null)
                    {
                        yield return w.WhenFalse;
                    }

                    yield break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind);
            }
        }

        public HashSet<LabelSymbol> ReachableLabels
        {
            get
            {
                if (_reachableLabels == null)
                {
                    // PROTOTYPE(patterns2): avoid linq?
                    _reachableLabels = new HashSet<LabelSymbol>(
                        this.TopologicallySortedNodes.OfType<BoundLeafDecisionDagNode>().Select(d => d.Label));
                }

                return _reachableLabels;
            }
        }

        /// <summary>
        /// A list of all the nodes reachable from the root node, in a topologically sorted order.
        /// </summary>
        public ImmutableArray<BoundDecisionDagNode> TopologicallySortedNodes
        {
            get
            {
                if (_topologicallySortedNodes.IsDefault)
                {
                    // We use an iterative topological sort to avoid overflowing the compiler's runtime stack for a large switch statement.
                    _topologicallySortedNodes = TopologicalSort.IterativeSort<BoundDecisionDagNode>(new[] { this.RootNode }, Successors);
                }

                return _topologicallySortedNodes;
            }
        }

        /// <summary>
        /// Given a decision dag and a constant-valued input, produce a simplified decision dag that has removed all the
        /// tests that are unnecessary due to that constant value. This simplification affects flow analysis (reachability
        /// and definite assignment) and permits us to simplify the generated code.
        /// </summary>
        public BoundDecisionDag SimplifyDecisionDagForConstantInput(
            BoundExpression input,
            Conversions conversions,
            DiagnosticBag diagnostics)
        {
            ConstantValue inputConstant = input.ConstantValue;
            Debug.Assert(inputConstant != null);

            // First, we topologically sort the nodes of the dag so that we can translate the nodes bottom-up.
            // This will avoid overflowing the compiler's runtime stack which would occur for a large switch
            // statement if we were using a recursive strategy.
            ImmutableArray<BoundDecisionDagNode> sortedNodes = this.TopologicallySortedNodes;

            // Cache simplified/translated replacement for each translated dag node. Since we always visit
            // a node's successors before the node, the replacement should always be in the cache when we need it.
            var replacement = PooledDictionary<BoundDecisionDagNode, BoundDecisionDagNode>.GetInstance();

            // Loop backwards through the topologically sorted nodes to translate them, so that we always visit a node after its successors
            for (int i = sortedNodes.Length - 1; i >= 0; i--)
            {
                BoundDecisionDagNode node = sortedNodes[i];
                Debug.Assert(!replacement.ContainsKey(node));
                BoundDecisionDagNode newNode = makeReplacement(node);
                replacement.Add(node, newNode);
            }

            // Return the computed replacement root node
            var newRoot = replacement[this.RootNode];
            replacement.Free();
            return this.Update(newRoot);

            // Make a replacement for a given node, using the precomputed replacements for its successors.
            BoundDecisionDagNode makeReplacement(BoundDecisionDagNode dag)
            {
                switch (dag)
                {
                    case BoundEvaluationDecisionDagNode p:
                        return p.Update(p.Evaluation, replacement[p.Next]);
                    case BoundTestDecisionDagNode p:
                        // This is the key to the optimization. The result of a top-level test might be known if the input is constant.
                        switch (knownResult(p.Test))
                        {
                            case true:
                                return replacement[p.WhenTrue];
                            case false:
                                return replacement[p.WhenFalse];
                            default:
                                return p.Update(p.Test, replacement[p.WhenTrue], replacement[p.WhenFalse]);
                        }
                    case BoundWhenDecisionDagNode p:
                        if (p.WhenExpression == null || p.WhenExpression.ConstantValue == ConstantValue.True)
                        {
                            return p.Update(p.Bindings, p.WhenExpression, replacement[p.WhenTrue], null);
                        }
                        else if (p.WhenExpression.ConstantValue == ConstantValue.False)
                        {
                            // It is possible in this case that we could eliminate some predecessor nodes, for example
                            // those that compute evaluations only needed to get to this decision. We do not bother,
                            // as that optimization would only be likely to affect test code.
                            return replacement[p.WhenFalse];
                        }
                        else
                        {
                            return p.Update(p.Bindings, p.WhenExpression, replacement[p.WhenTrue], replacement[p.WhenFalse]);
                        }
                    case BoundLeafDecisionDagNode p:
                        return p;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(dag);
                }
            }

            // Is the decision's result known because the input is a constant?
            bool? knownResult(BoundDagTest choice)
            {
                if (!choice.Input.IsOriginalInput)
                {
                    // This is a test of something other than the main input; result unknown
                    return null;
                }

                switch (choice)
                {
                    case BoundDagNullTest d:
                        return inputConstant.IsNull;
                    case BoundDagNonNullTest d:
                        return !inputConstant.IsNull;
                    case BoundDagValueTest d:
                        return d.Value == inputConstant;
                    case BoundDagTypeTest d:
                        HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                        bool? known = Binder.ExpressionOfTypeMatchesPatternType(conversions, input.Type, d.Type, ref useSiteDiagnostics, out Conversion conversion, inputConstant, inputConstant.IsNull);
                        diagnostics.Add(d.Syntax, useSiteDiagnostics);
                        return known;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(choice);
                }
            }

        }
    }
}
