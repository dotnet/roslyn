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
    internal class SyntaxStore
    {
        public class Builder
        {
            private readonly ImmutableDictionary<ISyntaxInputNode, Exception>.Builder _syntaxExceptions = ImmutableDictionary.CreateBuilder<ISyntaxInputNode, Exception>();
            private readonly StateTableStore.Builder _tableBuilder;
            private readonly Compilation _compilation;
            private readonly ImmutableArray<ISyntaxInputNode> _syntaxInputNodes;
            private readonly GeneratorDriverState _driverState;
            private readonly DriverStateTable _previousTable;
            private readonly StateTableStore _previousTableStates;
            private readonly CancellationToken _cancellationToken;

            public Builder(Compilation compilation, StateTableStore.Builder tableBuilder, ImmutableArray<ISyntaxInputNode> syntaxInputNodes, GeneratorDriverState driverState, DriverStateTable previousTable, StateTableStore previousTableStates, CancellationToken cancellationToken = default)
            {
                _compilation = compilation;
                _tableBuilder = tableBuilder;
                _syntaxInputNodes = syntaxInputNodes;
                _driverState = driverState;
                _previousTable = previousTable;
                _previousTableStates = previousTableStates;
                _cancellationToken = cancellationToken;
            }

            public IStateTable GetSyntaxInputTable(ISyntaxInputNode syntaxInputNode, bool compilationIsCached, NodeStateTable<SyntaxTree> syntaxTreeTable)
            {
                Debug.Assert(_syntaxInputNodes.Contains(syntaxInputNode));

                // when we don't have a value for this node, we update all the syntax inputs at once
                if (!_tableBuilder.Contains(syntaxInputNode))
                {
                    // CONSIDER: when the compilation is the same as previous, the syntax trees must also be the same.
                    // if we have a previous state table for a node, we can just short circuit knowing that it is up to date
                    // This step isn't part of the tree, so we can skip recording.

                    // get a builder for each input node
                    var syntaxInputBuilders = ArrayBuilder<ISyntaxInputBuilder>.GetInstance(_syntaxInputNodes.Length);
                    foreach (var node in _syntaxInputNodes)
                    {
                        // TODO: We don't cache the tracked incremental steps in a manner that we can easily rehydrate between runs,
                        // so we'll disable the cached compilation perf optimization when incremental step tracking is enabled.
                        if (compilationIsCached && !_driverState.TrackIncrementalSteps && _previousTableStates.TryGetValue(node, out var previousStateTable))
                        {
                            _tableBuilder.SetTable(node, previousStateTable);
                        }
                        else
                        {
                            syntaxInputBuilders.Add(node.GetBuilder(_previousTableStates, _driverState.TrackIncrementalSteps));
                        }
                    }

                    if (syntaxInputBuilders.Count == 0)
                    {
                        // bring over the previously cached syntax tree inputs
                        _tableBuilder.SetTable(SharedInputNodes.SyntaxTrees, _previousTableStates.GetStateTableOrEmpty<SyntaxTree>(SharedInputNodes.SyntaxTrees));
                    }
                    else
                    {
                        GeneratorRunStateTable.Builder temporaryRunStateBuilder = new GeneratorRunStateTable.Builder(_driverState.TrackIncrementalSteps);
                        NodeStateTable<SyntaxTree> syntaxTreeState = syntaxTreeTable;

                        // update each tree for the builders, sharing the semantic model
                        foreach ((var tree, var state, var syntaxTreeIndex, var stepInfo) in syntaxTreeState)
                        {
                            var root = new Lazy<SyntaxNode>(() => tree.GetRoot(_cancellationToken));
                            var model = state != EntryState.Removed ? _compilation.GetSemanticModel(tree) : null;
                            for (int i = 0; i < syntaxInputBuilders.Count; i++)
                            {
                                try
                                {
                                    _cancellationToken.ThrowIfCancellationRequested();
                                    syntaxInputBuilders[i].VisitTree(root, state, model, _cancellationToken);
                                }
                                catch (UserFunctionException ufe)
                                {
                                    // we're evaluating this node ahead of time, so we can't just throw the exception
                                    // instead we'll hold onto it, and throw the exception when a downstream node actually
                                    // attempts to read the value
                                    _syntaxExceptions[syntaxInputBuilders[i].SyntaxInputNode] = ufe;
                                    syntaxInputBuilders.RemoveAt(i);
                                    i--;
                                }
                            }
                        }

                        // save the updated inputs
                        foreach (ISyntaxInputBuilder builder in syntaxInputBuilders)
                        {
                            builder.SaveStateAndFree(_tableBuilder);
                            Debug.Assert(_tableBuilder.Contains(builder.SyntaxInputNode));
                        }
                    }
                    syntaxInputBuilders.Free();
                }

                // if we don't have an entry for this node, it must have thrown an exception
                if (!_tableBuilder.Contains(syntaxInputNode))
                {
                    throw _syntaxExceptions[syntaxInputNode];
                }

                _tableBuilder.TryGetTable(syntaxInputNode, out var result);
                return result!;
            }
        }
    }
}
