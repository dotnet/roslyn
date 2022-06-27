// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    internal sealed class SyntaxStore
    {
        private readonly StateTableStore _tables;
        private readonly Compilation? _compilation;
        internal static readonly SyntaxStore Empty = new SyntaxStore(StateTableStore.Empty, compilation: null);

        private SyntaxStore(StateTableStore tables, Compilation? compilation)
        {
            _tables = tables;
            _compilation = compilation;
        }

        public Builder ToBuilder(Compilation compilation, ImmutableArray<SyntaxInputNode> syntaxInputNodes, bool enableTracking, CancellationToken cancellationToken) => new Builder(compilation, syntaxInputNodes, enableTracking, this, cancellationToken);

        public sealed class Builder
        {
            private readonly ImmutableDictionary<SyntaxInputNode, Exception>.Builder _syntaxExceptions = ImmutableDictionary.CreateBuilder<SyntaxInputNode, Exception>();
            private readonly StateTableStore.Builder _tableBuilder = new StateTableStore.Builder();
            private readonly Compilation _compilation;
            private readonly ImmutableArray<SyntaxInputNode> _syntaxInputNodes;
            private readonly bool _enableTracking;
            private readonly SyntaxStore _previous;
            private readonly CancellationToken _cancellationToken;

            internal Builder(Compilation compilation, ImmutableArray<SyntaxInputNode> syntaxInputNodes, bool enableTracking, SyntaxStore previousStore, CancellationToken cancellationToken)
            {
                _compilation = compilation;
                _syntaxInputNodes = syntaxInputNodes;
                _enableTracking = enableTracking;
                _previous = previousStore;
                _cancellationToken = cancellationToken;
            }

            public IStateTable GetSyntaxInputTable(SyntaxInputNode syntaxInputNode, NodeStateTable<SyntaxTree> syntaxTreeTable)
            {
                Debug.Assert(_syntaxInputNodes.Contains(syntaxInputNode));

                // when we don't have a value for this node, we update all the syntax inputs at once
                if (!_tableBuilder.Contains(syntaxInputNode))
                {
                    // CONSIDER: when the compilation is the same as previous, the syntax trees must also be the same.
                    // if we have a previous state table for a node, we can just short circuit knowing that it is up to date
                    // This step isn't part of the tree, so we can skip recording.
                    var compilationIsCached = _compilation == _previous._compilation;

                    // get a builder for each input node
                    var syntaxInputBuilders = ArrayBuilder<(SyntaxInputNode node, ISyntaxInputBuilder builder)>.GetInstance(_syntaxInputNodes.Length);
                    foreach (var node in _syntaxInputNodes)
                    {
                        // We don't cache the tracked incremental steps in a manner that we can easily rehydrate between runs,
                        // so we disable the cached compilation perf optimization when incremental step tracking is enabled.
                        if (compilationIsCached && !_enableTracking && _previous._tables.TryGetValue(node, out var previousStateTable))
                        {
                            _tableBuilder.SetTable(node, previousStateTable);
                        }
                        else
                        {
                            syntaxInputBuilders.Add((node, node.GetBuilder(_previous._tables, _enableTracking)));
                        }
                    }

                    if (syntaxInputBuilders.Count > 0)
                    {
                        GeneratorRunStateTable.Builder temporaryRunStateBuilder = new GeneratorRunStateTable.Builder(_enableTracking);

                        // at this point we need to grab the syntax trees from the new compilation, and optionally diff them against the old ones
                        NodeStateTable<SyntaxTree> syntaxTreeState = syntaxTreeTable;

                        // update each tree for the builders, sharing the semantic model
                        foreach (var (tree, state, syntaxTreeIndex, stepInfo) in syntaxTreeState)
                        {
                            var root = new Lazy<SyntaxNode>(() => tree.GetRoot(_cancellationToken));
                            var model = state != EntryState.Removed ? _compilation.GetSemanticModel(tree) : null;
                            for (int i = 0; i < syntaxInputBuilders.Count; i++)
                            {
                                try
                                {
                                    _cancellationToken.ThrowIfCancellationRequested();
                                    syntaxInputBuilders[i].builder.VisitTree(root, state, model, _cancellationToken);
                                }
                                catch (UserFunctionException ufe)
                                {
                                    // we're evaluating this node ahead of time, so we can't just throw the exception
                                    // instead we'll hold onto it, and throw the exception when a downstream node actually
                                    // attempts to read the value
                                    _syntaxExceptions[syntaxInputBuilders[i].node] = ufe;
                                    syntaxInputBuilders.RemoveAt(i);
                                    i--;
                                }
                            }
                        }

                        // save the updated inputs
                        foreach ((var node, ISyntaxInputBuilder builder) in syntaxInputBuilders)
                        {
                            builder.SaveStateAndFree(_tableBuilder);
                            Debug.Assert(_tableBuilder.Contains(node));
                        }
                    }
                    syntaxInputBuilders.Free();
                }

                // if we don't have an entry for this node, it must have thrown an exception
                if (!_tableBuilder.TryGetTable(syntaxInputNode, out var result))
                {
                    throw _syntaxExceptions[syntaxInputNode];
                }
                return result;
            }

            public SyntaxStore ToImmutable()
            {
                return new SyntaxStore(_tableBuilder.ToImmutable(), _compilation);
            }
        }
    }
}
