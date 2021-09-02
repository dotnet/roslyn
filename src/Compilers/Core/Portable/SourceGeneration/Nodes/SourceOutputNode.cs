// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using TOutput = System.ValueTuple<System.Collections.Generic.IEnumerable<Microsoft.CodeAnalysis.GeneratedSourceText>, System.Collections.Generic.IEnumerable<Microsoft.CodeAnalysis.Diagnostic>>;

namespace Microsoft.CodeAnalysis
{
    internal sealed class SourceOutputNode<TInput> : IIncrementalGeneratorOutputNode, IIncrementalGeneratorNode<TOutput>
    {
        private readonly IIncrementalGeneratorNode<TInput> _source;

        private readonly Action<SourceProductionContext, TInput> _action;

        private readonly IncrementalGeneratorOutputKind _outputKind;

        private readonly string _sourceExtension;

        public SourceOutputNode(IIncrementalGeneratorNode<TInput> source, Action<SourceProductionContext, TInput> action, IncrementalGeneratorOutputKind outputKind, string sourceExtension)
        {
            _source = source;
            _action = action;

            Debug.Assert(outputKind == IncrementalGeneratorOutputKind.Source || outputKind == IncrementalGeneratorOutputKind.Implementation);
            _outputKind = outputKind;
            _sourceExtension = sourceExtension;
        }

        public IncrementalGeneratorOutputKind Kind => _outputKind;

        public NodeStateTable<TOutput> UpdateStateTable(DriverStateTable.Builder graphState, NodeStateTable<TOutput> previousTable, CancellationToken cancellationToken)
        {
            var sourceTable = graphState.GetLatestStateTableForNode(_source);
            if (sourceTable.IsCached)
            {
                return previousTable;
            }

            var nodeTable = previousTable.ToBuilder();
            foreach (var entry in sourceTable)
            {
                if (entry.state == EntryState.Removed)
                {
                    nodeTable.RemoveEntries();
                }
                else if (entry.state != EntryState.Cached || !nodeTable.TryUseCachedEntries())
                {
                    // we don't currently handle modified any differently than added at the output
                    // we just run the action and mark the new source as added. In theory we could compare
                    // the diagnostics and sources produced and compare them, to see if they are any different 
                    // than before.

                    var sourcesBuilder = new AdditionalSourcesCollection(_sourceExtension);
                    var diagnostics = DiagnosticBag.GetInstance();

                    SourceProductionContext context = new SourceProductionContext(sourcesBuilder, diagnostics, cancellationToken);
                    try
                    {
                        _action(context, entry.item);
                        nodeTable.AddEntry((sourcesBuilder.ToImmutable(), diagnostics.ToReadOnly()), EntryState.Added);
                    }
                    finally
                    {
                        sourcesBuilder.Free();
                        diagnostics.Free();
                    }

                }
            }

            return nodeTable.ToImmutableAndFree();
        }

        IIncrementalGeneratorNode<TOutput> IIncrementalGeneratorNode<TOutput>.WithComparer(IEqualityComparer<TOutput> comparer) => throw ExceptionUtilities.Unreachable;

        void IIncrementalGeneratorNode<TOutput>.RegisterOutput(IIncrementalGeneratorOutputNode output) => throw ExceptionUtilities.Unreachable;

        public void AppendOutputs(IncrementalExecutionContext context, CancellationToken cancellationToken)
        {
            // get our own state table
            Debug.Assert(context.TableBuilder is object);
            var table = context.TableBuilder.GetLatestStateTableForNode(this);

            // add each non-removed entry to the context
            foreach (var ((sources, diagnostics), state) in table)
            {
                if (state != EntryState.Removed)
                {
                    foreach (var text in sources)
                    {
                        try
                        {
                            context.Sources.Add(text.HintName, text.Text);
                        }
                        catch (ArgumentException e)
                        {
                            throw new UserFunctionException(e);
                        }
                    }
                    context.Diagnostics.AddRange(diagnostics);
                }
            }
        }
    }
}
