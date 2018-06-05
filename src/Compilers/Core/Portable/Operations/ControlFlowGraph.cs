// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Control flow graph representation for a given executable code block <see cref="OriginalOperation"/>.
    /// This graph contains a set of <see cref="BasicBlock"/>s, with an entry block, zero
    /// or more intermediate basic blocks and an exit block.
    /// Each basic block contains zero or more <see cref="BasicBlock.Operations"/> and
    /// explicit <see cref="ControlFlowBranch"/>(s) to other basic block(s).
    /// </summary>
    public sealed partial class ControlFlowGraph
    {
        private readonly ImmutableDictionary<IMethodSymbol, (ControlFlowRegion region, IOperation operation, int ordinal)> _methodsMap;
        private ControlFlowGraph[] _lazySubGraphs;

        internal ControlFlowGraph(IOperation originalOperation,
                                  ImmutableArray<BasicBlock> blocks, ControlFlowRegion root,
                                  ImmutableArray<IMethodSymbol> nestedMethods,
                                  ImmutableDictionary<IMethodSymbol, (ControlFlowRegion region, IOperation operation, int ordinal)> methodsMap)
        {
            Debug.Assert(!blocks.IsDefault);
            Debug.Assert(blocks.First().Kind == BasicBlockKind.Entry);
            Debug.Assert(blocks.Last().Kind == BasicBlockKind.Exit);
            Debug.Assert(root != null);
            Debug.Assert(root.Kind == ControlFlowRegionKind.Root);
            Debug.Assert(root.FirstBlockOrdinal == 0);
            Debug.Assert(root.LastBlockOrdinal == blocks.Length - 1);
            Debug.Assert(!nestedMethods.IsDefault);
            Debug.Assert(methodsMap != null);
            Debug.Assert(methodsMap.Count == nestedMethods.Length);
            Debug.Assert(nestedMethods.Distinct().Count() == nestedMethods.Length);
#if DEBUG
            foreach (IMethodSymbol method in nestedMethods)
            {
                Debug.Assert(method.MethodKind == MethodKind.AnonymousFunction || method.MethodKind == MethodKind.LocalFunction);
                Debug.Assert(methodsMap.ContainsKey(method));
            }
#endif 

            OriginalOperation = originalOperation;
            Blocks = blocks;
            Root = root;
            NestedMethods = nestedMethods;
            _methodsMap = methodsMap;
        }

        /// <summary>
        /// Original operation, representing an executable code block, from which this control flow graph was generated.
        /// Note that <see cref="BasicBlock.Operations"/> in the control flow graph are not in the same operation tree as
        /// the original operation.
        /// </summary>
        public IOperation OriginalOperation { get; }

        /// <summary>
        /// Basic blocks for the control flow graph.
        /// </summary>
        public ImmutableArray<BasicBlock> Blocks { get; }

        /// <summary>
        /// Root (<see cref="ControlFlowRegionKind.Root"/>) region for the graph.
        /// </summary>
        public ControlFlowRegion Root { get; }

        /// <summary>
        /// Lambdas and local functions declared within <see cref="OriginalOperation"/>.
        /// </summary>
        public ImmutableArray<IMethodSymbol> NestedMethods { get; }

        /// <summary>
        /// Creates a control flow graph for the given <paramref name="nestedMethod"/>.
        /// </summary>
        /// <param name="nestedMethod">Nested method from <see cref="NestedMethods"/>.</param>
        public ControlFlowGraph GetNestedControlFlowGraph(IMethodSymbol nestedMethod)
        {
            // PROTOTYPE(dataflow): It looks like the indexing below will throw right exceptions on all invalid inputs.
            //                      However, we should consider if we want to perform our own validation for the input.
            //                      In any case, we need to add unit tests to cover behavior of this API with respect to
            //                      an invalid input (including null).

            (ControlFlowRegion enclosing, IOperation operation, int ordinal) = _methodsMap[nestedMethod];
            Debug.Assert(nestedMethod == NestedMethods[ordinal]);

            if (_lazySubGraphs == null)
            {
                Interlocked.CompareExchange(ref _lazySubGraphs, new ControlFlowGraph[NestedMethods.Length], null);
            }

            if (_lazySubGraphs[ordinal] == null)
            {
                ControlFlowGraph graph;

                switch (operation.Kind)
                {
                    case OperationKind.LocalFunction:
                        Debug.Assert(nestedMethod == ((ILocalFunctionOperation)operation).Symbol);
                        graph = ControlFlowGraphBuilder.Create(operation, enclosing);
                        break;
                    
                    case OperationKind.AnonymousFunction:
                        Debug.Assert(nestedMethod == ((IAnonymousFunctionOperation)operation).Symbol);
                        graph = ControlFlowGraphBuilder.Create(operation, enclosing);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(operation.Kind);
                }

                Interlocked.CompareExchange(ref _lazySubGraphs[ordinal], graph, null);
            }

            return _lazySubGraphs[ordinal];
        }
    }
}
