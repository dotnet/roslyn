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
        private readonly ControlFlowGraphBuilder.CaptureIdDispenser _captureIdDispenser;
        private readonly ImmutableDictionary<IMethodSymbol, (ControlFlowRegion region, ILocalFunctionOperation operation, int ordinal)> _localFunctionsMap;
        private ControlFlowGraph[] _lazyLocalFunctionsGraphs;
        private readonly ImmutableDictionary<IFlowAnonymousFunctionOperation, (ControlFlowRegion region, int ordinal)> _anonymousFunctionsMap;
        private ControlFlowGraph[] _lazyAnonymousFunctionsGraphs;

        internal ControlFlowGraph(IOperation originalOperation,
                                  ControlFlowGraphBuilder.CaptureIdDispenser captureIdDispenser,
                                  ImmutableArray<BasicBlock> blocks, ControlFlowRegion root,
                                  ImmutableArray<IMethodSymbol> localFunctions,
                                  ImmutableDictionary<IMethodSymbol, (ControlFlowRegion region, ILocalFunctionOperation operation, int ordinal)> localFunctionsMap,
                                  ImmutableDictionary<IFlowAnonymousFunctionOperation, (ControlFlowRegion region, int ordinal)> anonymousFunctionsMap)
        {
            Debug.Assert(captureIdDispenser != null);
            Debug.Assert(!blocks.IsDefault);
            Debug.Assert(blocks.First().Kind == BasicBlockKind.Entry);
            Debug.Assert(blocks.Last().Kind == BasicBlockKind.Exit);
            Debug.Assert(root != null);
            Debug.Assert(root.Kind == ControlFlowRegionKind.Root);
            Debug.Assert(root.FirstBlockOrdinal == 0);
            Debug.Assert(root.LastBlockOrdinal == blocks.Length - 1);
            Debug.Assert(!localFunctions.IsDefault);
            Debug.Assert(localFunctionsMap != null);
            Debug.Assert(localFunctionsMap.Count == localFunctions.Length);
            Debug.Assert(localFunctions.Distinct().Count() == localFunctions.Length);
            Debug.Assert(anonymousFunctionsMap != null);
#if DEBUG
            foreach (IMethodSymbol method in localFunctions)
            {
                Debug.Assert(method.MethodKind == MethodKind.LocalFunction);
                Debug.Assert(localFunctionsMap.ContainsKey(method));
            }
#endif 

            OriginalOperation = originalOperation;
            Blocks = blocks;
            Root = root;
            LocalFunctions = localFunctions;
            _localFunctionsMap = localFunctionsMap;
            _anonymousFunctionsMap = anonymousFunctionsMap;
            _captureIdDispenser = captureIdDispenser;
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
        /// Local functions declared within <see cref="OriginalOperation"/>.
        /// </summary>
        public ImmutableArray<IMethodSymbol> LocalFunctions { get; }

        /// <summary>
        /// Creates a control flow graph for the given <paramref name="localFunction"/>.
        /// </summary>
        public ControlFlowGraph GetLocalFunctionControlFlowGraph(IMethodSymbol localFunction)
        {
            // PROTOTYPE(dataflow): It looks like the indexing below will throw right exceptions on all invalid inputs.
            //                      However, we should consider if we want to perform our own validation for the input.
            //                      In any case, we need to add unit tests to cover behavior of this API with respect to
            //                      an invalid input (including null).

            (ControlFlowRegion enclosing, ILocalFunctionOperation operation, int ordinal) = _localFunctionsMap[localFunction];
            Debug.Assert(localFunction == LocalFunctions[ordinal]);

            if (_lazyLocalFunctionsGraphs == null)
            {
                Interlocked.CompareExchange(ref _lazyLocalFunctionsGraphs, new ControlFlowGraph[LocalFunctions.Length], null);
            }

            if (_lazyLocalFunctionsGraphs[ordinal] == null)
            {
                Debug.Assert(localFunction == operation.Symbol);
                ControlFlowGraph graph = ControlFlowGraphBuilder.Create(operation, enclosing, _captureIdDispenser);
                Debug.Assert(graph.OriginalOperation == operation);
                Interlocked.CompareExchange(ref _lazyLocalFunctionsGraphs[ordinal], graph, null);
            }

            return _lazyLocalFunctionsGraphs[ordinal];
        }

        /// <summary>
        /// Creates a control flow graph for the given <paramref name="anonymousFunction"/>.
        /// </summary>
        public ControlFlowGraph GetAnonymousFunctionControlFlowGraph(IFlowAnonymousFunctionOperation anonymousFunction)
        {
            // PROTOTYPE(dataflow): It looks like the indexing below will throw right exceptions on all invalid inputs.
            //                      However, we should consider if we want to perform our own validation for the input.
            //                      In any case, we need to add unit tests to cover behavior of this API with respect to
            //                      an invalid input (including null).

            (ControlFlowRegion enclosing, int ordinal) = _anonymousFunctionsMap[anonymousFunction];

            if (_lazyAnonymousFunctionsGraphs == null)
            {
                Interlocked.CompareExchange(ref _lazyAnonymousFunctionsGraphs, new ControlFlowGraph[_anonymousFunctionsMap.Count], null);
            }

            if (_lazyAnonymousFunctionsGraphs[ordinal] == null)
            {
                var anonymous = (FlowAnonymousFunctionOperation)anonymousFunction;
                ControlFlowGraph graph = ControlFlowGraphBuilder.Create(anonymous.Original, enclosing, _captureIdDispenser, in anonymous.Context);
                Debug.Assert(graph.OriginalOperation == anonymous.Original);
                Interlocked.CompareExchange(ref _lazyAnonymousFunctionsGraphs[ordinal], graph, null);
            }

            return _lazyAnonymousFunctionsGraphs[ordinal];
        }
    }
}
