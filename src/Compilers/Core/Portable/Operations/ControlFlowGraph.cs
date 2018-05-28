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
        private ImmutableDictionary<IMethodSymbol, (Region, IOperation, int)> _methodsMap;
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
