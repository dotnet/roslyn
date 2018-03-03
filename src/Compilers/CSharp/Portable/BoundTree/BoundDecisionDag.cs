// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class BoundDecisionDag
    {
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
    }

}
