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
                                  ControlFlowGraph parent,
                                  ControlFlowGraphBuilder.CaptureIdDispenser captureIdDispenser,
                                  ImmutableArray<BasicBlock> blocks, ControlFlowRegion root,
                                  ImmutableArray<IMethodSymbol> localFunctions,
                                  ImmutableDictionary<IMethodSymbol, (ControlFlowRegion region, ILocalFunctionOperation operation, int ordinal)> localFunctionsMap,
                                  ImmutableDictionary<IFlowAnonymousFunctionOperation, (ControlFlowRegion region, int ordinal)> anonymousFunctionsMap)
        {
            Debug.Assert(parent != null == (originalOperation.Kind == OperationKind.LocalFunction || originalOperation.Kind == OperationKind.AnonymousFunction));
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
            Parent = parent;
            Blocks = blocks;
            Root = root;
            LocalFunctions = localFunctions;
            _localFunctionsMap = localFunctionsMap;
            _anonymousFunctionsMap = anonymousFunctionsMap;
            _captureIdDispenser = captureIdDispenser;
        }

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        /// <summary>
        /// Creates a <see cref="ControlFlowGraph"/> for the given executable code block root <paramref name="node"/>.
        /// </summary>
        /// <param name="node">Root syntax node for an executable code block.</param>
        /// <param name="semanticModel">Semantic model for the syntax tree containing the <paramref name="node"/>.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>
        /// Returns null if <see cref="SemanticModel.GetOperation(SyntaxNode, CancellationToken)"/> returns null for the given <paramref name="node"/> and <paramref name="semanticModel"/>.
        /// Otherwise, returns a <see cref="ControlFlowGraph"/> for the executable code block.
        /// </returns>
        public static ControlFlowGraph Create(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken = default)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (semanticModel == null)
            {
                throw new ArgumentNullException(nameof(semanticModel));
            }

            IOperation operation = semanticModel.GetOperation(node, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return operation == null ? null : CreateCore(operation, nameof(operation), cancellationToken);
        }

        /// <summary>
        /// Creates a <see cref="ControlFlowGraph"/> for the given executable code block <paramref name="body"/>.
        /// </summary>
        /// <param name="body">Root operation block, which must have a null parent.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        public static ControlFlowGraph Create(Operations.IBlockOperation body, CancellationToken cancellationToken = default)
        {
            return CreateCore(body, nameof(body), cancellationToken);
        }

        /// <summary>
        /// Creates a <see cref="ControlFlowGraph"/> for the given executable code block <paramref name="initializer"/>.
        /// </summary>
        /// <param name="initializer">Root field initializer operation, which must have a null parent.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        public static ControlFlowGraph Create(Operations.IFieldInitializerOperation initializer, CancellationToken cancellationToken = default)
        {
            return CreateCore(initializer, nameof(initializer), cancellationToken);
        }

        /// <summary>
        /// Creates a <see cref="ControlFlowGraph"/> for the given executable code block <paramref name="initializer"/>.
        /// </summary>
        /// <param name="initializer">Root property initializer operation, which must have a null parent.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        public static ControlFlowGraph Create(Operations.IPropertyInitializerOperation initializer, CancellationToken cancellationToken = default)
        {
            return CreateCore(initializer, nameof(initializer), cancellationToken);
        }

        /// <summary>
        /// Creates a <see cref="ControlFlowGraph"/> for the given executable code block <paramref name="initializer"/>.
        /// </summary>
        /// <param name="initializer">Root parameter initializer operation, which must have a null parent.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        public static ControlFlowGraph Create(Operations.IParameterInitializerOperation initializer, CancellationToken cancellationToken = default)
        {
            return CreateCore(initializer, nameof(initializer), cancellationToken);
        }


        /// <summary>
        /// Creates a <see cref="ControlFlowGraph"/> for the given executable code block <paramref name="constructorBody"/>.
        /// </summary>
        /// <param name="constructorBody">Root constructor body operation, which must have a null parent.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        public static ControlFlowGraph Create(Operations.IConstructorBodyOperation constructorBody, CancellationToken cancellationToken = default)
        {
            return CreateCore(constructorBody, nameof(constructorBody), cancellationToken);
        }

        /// <summary>
        /// Creates a <see cref="ControlFlowGraph"/> for the given executable code block <paramref name="methodBody"/>.
        /// </summary>
        /// <param name="methodBody">Root method body operation, which must have a null parent.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        public static ControlFlowGraph Create(Operations.IMethodBodyOperation methodBody, CancellationToken cancellationToken = default)
        {
            return CreateCore(methodBody, nameof(methodBody), cancellationToken);
        }
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters

        internal static ControlFlowGraph CreateCore(IOperation operation, string argumentNameForException, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (operation == null)
            {
                throw new ArgumentNullException(argumentNameForException);
            }

            if (operation.Parent != null)
            {
                throw new ArgumentException(CodeAnalysisResources.NotARootOperation, argumentNameForException);
            }

            if (((Operation)operation).OwningSemanticModel == null)
            {
                throw new ArgumentException(CodeAnalysisResources.OperationHasNullSemanticModel, argumentNameForException);
            }

            try
            {
                ControlFlowGraph controlFlowGraph = ControlFlowGraphBuilder.Create(operation);
                Debug.Assert(controlFlowGraph.OriginalOperation == operation);
                return controlFlowGraph;
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                // Log a Non-fatal-watson and then ignore the crash in the attempt of getting flow graph.
                Debug.Assert(false, "\n" + e.ToString());
            }

            return default;
        }

        /// <summary>
        /// Original operation, representing an executable code block, from which this control flow graph was generated.
        /// Note that <see cref="BasicBlock.Operations"/> in the control flow graph are not in the same operation tree as
        /// the original operation.
        /// </summary>
        public IOperation OriginalOperation { get; }

        /// <summary>
        /// Optional parent control flow graph for this graph.
        /// Non-null for a control flow graph generated for a local function or a lambda.
        /// Null otherwise.
        /// </summary>
        public ControlFlowGraph Parent { get; }

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
        public ControlFlowGraph GetLocalFunctionControlFlowGraph(IMethodSymbol localFunction, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (localFunction is null)
            {
                throw new ArgumentNullException(nameof(localFunction));
            }

            if (!TryGetLocalFunctionControlFlowGraph(localFunction, cancellationToken, out var controlFlowGraph))
            {
                throw new ArgumentOutOfRangeException(nameof(localFunction));
            }

            return controlFlowGraph;
        }

        internal bool TryGetLocalFunctionControlFlowGraph(IMethodSymbol localFunction, CancellationToken cancellationToken, out ControlFlowGraph controlFlowGraph)
        {
            if (!_localFunctionsMap.TryGetValue(localFunction, out (ControlFlowRegion enclosing, ILocalFunctionOperation operation, int ordinal) info))
            {
                controlFlowGraph = null;
                return false;
            }

            Debug.Assert(localFunction == LocalFunctions[info.ordinal]);

            if (_lazyLocalFunctionsGraphs == null)
            {
                Interlocked.CompareExchange(ref _lazyLocalFunctionsGraphs, new ControlFlowGraph[LocalFunctions.Length], null);
            }

            if (_lazyLocalFunctionsGraphs[info.ordinal] == null)
            {
                Debug.Assert(localFunction == info.operation.Symbol);
                ControlFlowGraph graph = ControlFlowGraphBuilder.Create(info.operation, this, info.enclosing, _captureIdDispenser);
                Debug.Assert(graph.OriginalOperation == info.operation);
                Interlocked.CompareExchange(ref _lazyLocalFunctionsGraphs[info.ordinal], graph, null);
            }

            controlFlowGraph = _lazyLocalFunctionsGraphs[info.ordinal];
            Debug.Assert(controlFlowGraph.Parent == this);
            return true;
        }

        /// <summary>
        /// Creates a control flow graph for the given <paramref name="anonymousFunction"/>.
        /// </summary>
        public ControlFlowGraph GetAnonymousFunctionControlFlowGraph(IFlowAnonymousFunctionOperation anonymousFunction, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (anonymousFunction is null)
            {
                throw new ArgumentNullException(nameof(anonymousFunction));
            }

            if (!TryGetAnonymousFunctionControlFlowGraph(anonymousFunction, cancellationToken, out ControlFlowGraph controlFlowGraph))
            {
                throw new ArgumentOutOfRangeException(nameof(anonymousFunction));
            }

            return controlFlowGraph;
        }

        internal bool TryGetAnonymousFunctionControlFlowGraph(IFlowAnonymousFunctionOperation anonymousFunction, CancellationToken cancellationToken, out ControlFlowGraph controlFlowGraph)
        {
            if (!_anonymousFunctionsMap.TryGetValue(anonymousFunction, out (ControlFlowRegion enclosing, int ordinal) info))
            {
                controlFlowGraph = null;
                return false;
            }

            if (_lazyAnonymousFunctionsGraphs == null)
            {
                Interlocked.CompareExchange(ref _lazyAnonymousFunctionsGraphs, new ControlFlowGraph[_anonymousFunctionsMap.Count], null);
            }

            if (_lazyAnonymousFunctionsGraphs[info.ordinal] == null)
            {
                var anonymous = (FlowAnonymousFunctionOperation)anonymousFunction;
                ControlFlowGraph graph = ControlFlowGraphBuilder.Create(anonymous.Original, this, info.enclosing, _captureIdDispenser, in anonymous.Context);
                Debug.Assert(graph.OriginalOperation == anonymous.Original);
                Interlocked.CompareExchange(ref _lazyAnonymousFunctionsGraphs[info.ordinal], graph, null);
            }

            controlFlowGraph = _lazyAnonymousFunctionsGraphs[info.ordinal];
            Debug.Assert(controlFlowGraph.Parent == this);
            return true;
        }
    }
}
