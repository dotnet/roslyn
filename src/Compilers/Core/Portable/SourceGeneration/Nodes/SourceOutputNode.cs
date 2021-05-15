// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        public SourceOutputNode(IIncrementalGeneratorNode<TInput> source, Action<SourceProductionContext, TInput> action)
        {
            _source = source;
            _action = action;
        }

        public NodeStateTable<TOutput> UpdateStateTable(DriverStateTable.Builder graphState, NodeStateTable<TOutput> previousTable, CancellationToken cancellationToken)
        {
            var sourceTable = graphState.GetLatestStateTableForNode(_source);
            if (sourceTable.IsCompacted)
            {
                return previousTable;
            }
            if (sourceTable.IsFaulted)
            {
                return NodeStateTable<TOutput>.FromFaultedTable(sourceTable);
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

                    var sourcesBuilder = ArrayBuilder<GeneratedSourceText>.GetInstance();
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

        public void AppendOutputs(IncrementalExecutionContext context)
        {
            // get our own state table
            var table = context.TableBuilder.GetLatestStateTableForNode(this);

            if (table.IsFaulted)
            {
                // PROTOTYPE (source-generators): we're essentially using exceptions as control flow here.
                //                                instead we should append the exceptions to the context and allow the driver to handle it there
                throw table.GetException();
            }

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
                            //PROTOTYPE(source-generators): we should update the error messages to be specific about *which* file errored as it now won't happen
                            //                              at the same time the file is added.
                            throw new UserFunctionException(e);
                        }
                    }
                    context.Diagnostics.AddRange(diagnostics);
                }
            }
        }
    }
}
