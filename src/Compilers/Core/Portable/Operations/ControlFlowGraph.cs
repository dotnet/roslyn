// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// PROTOTYPE(dataflow): Add documentation
    /// </summary>
    public sealed partial class ControlFlowGraph
    {
        // PROTOTYPE(dataflow): This dictionary will hold on to the original IOperation tree and that will keep SemanticModel alive too.
        //                      Is this a problem? Should we simply build all sub-graphs eagerly?
        private readonly ImmutableDictionary<IMethodSymbol, (Region region, IOperation operation, int ordinal)> _methodsMap;
        private ControlFlowGraph[] _lazySubGraphs;

        internal ControlFlowGraph(ImmutableArray<BasicBlock> blocks, Region root,
                                  ImmutableArray<IMethodSymbol> methods,
                                  ImmutableDictionary<IMethodSymbol, (Region region, IOperation operation, int ordinal)> methodsMap)
        {
            Debug.Assert(!blocks.IsDefault);
            Debug.Assert(blocks.First().Kind == BasicBlockKind.Entry);
            Debug.Assert(blocks.Last().Kind == BasicBlockKind.Exit);
            Debug.Assert(root != null);
            Debug.Assert(root.Kind == RegionKind.Root);
            Debug.Assert(root.FirstBlockOrdinal == 0);
            Debug.Assert(root.LastBlockOrdinal == blocks.Length - 1);
            Debug.Assert(!methods.IsDefault);
            Debug.Assert(methodsMap != null);
            Debug.Assert(methodsMap.Count == methods.Length);
            Debug.Assert(methods.Distinct().Count() == methods.Length);
#if DEBUG
            foreach (IMethodSymbol method in methods)
            {
                Debug.Assert(method.MethodKind == MethodKind.AnonymousFunction || method.MethodKind == MethodKind.LocalFunction);
                Debug.Assert(methodsMap.ContainsKey(method));
            }
#endif 

            Blocks = blocks;
            Root = root;
            Methods = methods;
            _methodsMap = methodsMap;
        }

        /// <summary>
        /// PROTOTYPE(dataflow): Add documentation
        /// </summary>
        public ImmutableArray<BasicBlock> Blocks { get; }

        /// <summary>
        /// Root (<see cref="RegionKind.Root"/>) region for the graph.
        /// </summary>
        public Region Root { get; }

        /// <summary>
        /// PROTOTYPE(dataflow): Add documentation
        /// </summary>
        public ImmutableArray<IMethodSymbol> Methods { get; }

        /// <summary>
        /// PROTOTYPE(dataflow): Add documentation
        /// </summary>
        public ControlFlowGraph this[IMethodSymbol method]
        {
            get
            {
                // PROTOTYPE(dataflow): It looks like the indexing below will throw right exceptions on all invalid inputs.
                //                      However, we should consider if we want to perform our own validation for the input.
                //                      In any case, we need to add unit tests to cover behavior of this API with respect to
                //                      an invalid input (including null).

                (Region enclosing, IOperation operation, int ordinal) = _methodsMap[method];
                Debug.Assert(method == Methods[ordinal]);

                if (_lazySubGraphs == null)
                {
                    Interlocked.CompareExchange(ref _lazySubGraphs, new ControlFlowGraph[Methods.Length], null);
                }

                if (_lazySubGraphs[ordinal] == null)
                {
                    ControlFlowGraph graph;

                    switch (operation.Kind)
                    {
                        case OperationKind.LocalFunction:
                            var localFunction = (ILocalFunctionOperation)operation;
                            Debug.Assert(method == localFunction.Symbol);
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
}
