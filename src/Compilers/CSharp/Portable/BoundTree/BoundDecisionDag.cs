// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class BoundDecisionDag
    {
        public IEnumerable<BoundDecisionDag> Successors()
        {
            switch (this)
            {
                case BoundEvaluationPoint p:
                    yield return p.Next;
                    yield break;
                case BoundDecisionPoint p:
                    Debug.Assert(p.WhenFalse != null);
                    yield return p.WhenFalse;
                    Debug.Assert(p.WhenTrue != null);
                    yield return p.WhenTrue;
                    yield break;
                case BoundDecision d:
                    yield break;
                case BoundWhenClause w:
                    if (w.WhenFalse != null)
                    {
                        yield return w.WhenFalse;
                    }

                    Debug.Assert(w.WhenTrue != null);
                    yield return w.WhenTrue;
                    yield break;
            }
        }

        private HashSet<LabelSymbol> _reachableLabels;

        public HashSet<LabelSymbol> ReachableLabels
        {
            get
            {
                if (_reachableLabels == null)
                {
                    // Compute the set of reachable labels at the BoundDecision leaves.  We do this
                    // iteratively rather than recursively so that we do not overflow the stack on a
                    // large switch statement.
                    var workQueue = ArrayBuilder<BoundDecisionDag>.GetInstance();
                    var processed = PooledHashSet<BoundDecisionDag>.GetInstance();
                    var reachableLabels = new HashSet<LabelSymbol>();
                    workQueue.Add(this);
                    processWorkQueue();
                    workQueue.Free();
                    processed.Free();
                    _reachableLabels = reachableLabels;

                    void processWorkQueue()
                    {
                        while (workQueue.Count != 0)
                        {
                            BoundDecisionDag node = workQueue.Pop();
                            if (node == null || !processed.Add(node))
                            {
                                continue;
                            }

                            switch (node)
                            {
                                case BoundEvaluationPoint x:
                                    workQueue.Push(x.Next);
                                    break;
                                case BoundDecisionPoint x:
                                    workQueue.Push(x.WhenTrue);
                                    workQueue.Push(x.WhenFalse);
                                    break;
                                case BoundWhenClause x:
                                    workQueue.Push(x.WhenTrue);
                                    workQueue.Push(x.WhenFalse); // possibly null, handled later
                                    break;
                                case BoundDecision x:
                                    reachableLabels.Add(x.Label);
                                    break;
                                default:
                                    throw ExceptionUtilities.UnexpectedValue(node.Kind);
                            }
                        }
                    }
                }

                return _reachableLabels;
            }
        }

        /// <summary>
        /// Return a list of all the nodes reachable from this one, in a topologically sorted order.
        /// </summary>
        public ImmutableArray<BoundDecisionDag> TopologicallySortedNodes()
        {
            // We use an iterative topological sort to avoid overflowing the compiler's runtime stack for a large switch statement.
            return TopologicalSort.IterativeSort<BoundDecisionDag>(new[] { this }, successorFunction);

            IEnumerable<BoundDecisionDag> successorFunction(BoundDecisionDag dag)
            {
                switch (dag)
                {
                    case BoundEvaluationPoint p:
                        yield return p.Next;
                        yield break;
                    case BoundDecisionPoint p:
                        yield return p.WhenTrue;
                        yield return p.WhenFalse;
                        yield break;
                    case BoundWhenClause p:
                        yield return p.WhenTrue;
                        if (p.WhenFalse != null)
                        {
                            yield return p.WhenFalse;
                        }
                        yield break;
                    case BoundDecision p:
                        yield break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(dag);
                }
            }
        }

        /// <summary>
        /// Given a decision dag and a constant-valued input, produce a simplified decision dag that has removed all the
        /// tests that are unnecessary due to that constant value. This simplification affects flow analysis (reachability
        /// and definite assignment) and permits us to simplify the generated code.
        /// </summary>
        internal BoundDecisionDag SimplifyDecisionDagForConstantInput(
            BoundExpression input,
            Conversions conversions,
            DiagnosticBag diagnostics)
        {
            ConstantValue inputConstant = input.ConstantValue;
            Debug.Assert(inputConstant != null);

            // First, we topologically sort the nodes of the dag so that we can translate the nodes bottom-up.
            // This will avoid overflowing the compiler's runtime stack which would occur for a large switch
            // statement if we were using a recursive strategy.
            ImmutableArray<BoundDecisionDag> sortedNodes = this.TopologicallySortedNodes();

            // Cache simplified/translated replacement for each translated dag node
            var replacementCache = PooledDictionary<BoundDecisionDag, BoundDecisionDag>.GetInstance();

            // Loop backwards through the topologically sorted nodes to translate them, so that we always visit a node after its successors
            for (int i = sortedNodes.Length - 1; i >= 0; i--)
            {
                BoundDecisionDag node = sortedNodes[i];
                Debug.Assert(!replacementCache.ContainsKey(node));
                BoundDecisionDag newNode = makeReplacement(node);
                replacementCache.Add(node, newNode);
            }

            var result = replacement(this);
            replacementCache.Free();
            return result;

            // Return a cached replacement node. Since we always visit a node's succesors before the node,
            // the replacement should always be in the cache when we need it.
            BoundDecisionDag replacement(BoundDecisionDag dag)
            {
                if (replacementCache.TryGetValue(dag, out BoundDecisionDag knownReplacement))
                {
                    return knownReplacement;
                }

                throw ExceptionUtilities.Unreachable;
            }

            // Make a replacement for a given node, using the precomputed replacements for its successors.
            BoundDecisionDag makeReplacement(BoundDecisionDag dag)
            {
                switch (dag)
                {
                    case BoundEvaluationPoint p:
                        return p.Update(p.Evaluation, replacement(p.Next));
                    case BoundDecisionPoint p:
                        // This is the key to the optimization. The result of a top-level test might be known if the input is constant.
                        switch (knownResult(p.Decision))
                        {
                            case true:
                                return replacement(p.WhenTrue);
                            case false:
                                return replacement(p.WhenFalse);
                            default:
                                return p.Update(p.Decision, replacement(p.WhenTrue), replacement(p.WhenFalse));
                        }
                    case BoundWhenClause p:
                        if (p.WhenExpression == null || p.WhenExpression.ConstantValue == ConstantValue.True)
                        {
                            return p.Update(p.Bindings, p.WhenExpression, makeReplacement(p.WhenTrue), null);
                        }
                        else if (p.WhenExpression.ConstantValue == ConstantValue.False)
                        {
                            // It is possible in this case that we could eliminate some predecessor nodes, for example
                            // those that compute evaluations only needed to get to this decision. We do not bother,
                            // as that optimization would only be likely to affect test code.
                            return replacement(p.WhenFalse);
                        }
                        else
                        {
                            return p.Update(p.Bindings, p.WhenExpression, replacement(p.WhenTrue), replacement(p.WhenFalse));
                        }
                    case BoundDecision p:
                        return p;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(dag);
                }
            }

            // Is the decision's result known because the input is a constant?
            bool? knownResult(BoundDagDecision choice)
            {
                if (choice.Input.Source != null)
                {
                    // This is a test of something other than the main input; result unknown
                    return null;
                }

                switch (choice)
                {
                    case BoundNullValueDecision d:
                        return inputConstant.IsNull;
                    case BoundNonNullDecision d:
                        return !inputConstant.IsNull;
                    case BoundNonNullValueDecision d:
                        return d.Value == inputConstant;
                    case BoundTypeDecision d:
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
