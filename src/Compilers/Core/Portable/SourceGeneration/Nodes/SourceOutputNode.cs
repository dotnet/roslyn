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

        private readonly Action<SourceProductionContext, TInput, CancellationToken> _action;

        private readonly IncrementalGeneratorOutputKind _outputKind;

        private readonly string _sourceExtension;

        public SourceOutputNode(IIncrementalGeneratorNode<TInput> source, Action<SourceProductionContext, TInput, CancellationToken> action, IncrementalGeneratorOutputKind outputKind, string sourceExtension)
        {
            _source = source;
            _action = action;

            Debug.Assert(outputKind == IncrementalGeneratorOutputKind.Source || outputKind == IncrementalGeneratorOutputKind.Implementation);
            _outputKind = outputKind;
            _sourceExtension = sourceExtension;
        }

        public IncrementalGeneratorOutputKind Kind => _outputKind;

        public NodeStateTable<TOutput> UpdateStateTable(DriverStateTable.Builder graphState, NodeStateTable<TOutput>? previousTable, CancellationToken cancellationToken)
        {
            string stepName = Kind == IncrementalGeneratorOutputKind.Source ? WellKnownGeneratorOutputs.SourceOutput : WellKnownGeneratorOutputs.ImplementationSourceOutput;
            var sourceTable = graphState.GetLatestStateTableForNode(_source);
            if (sourceTable.IsCached && previousTable is not null)
            {
                if (graphState.DriverState.TrackIncrementalSteps)
                {
                    return previousTable.CreateCachedTableWithUpdatedSteps(sourceTable, stepName, EqualityComparer<TOutput>.Default);
                }
                return previousTable;
            }

            var nodeTable = graphState.CreateTableBuilder(previousTable, stepName, EqualityComparer<TOutput>.Default);
            foreach (var entry in sourceTable)
            {
                var inputs = nodeTable.TrackIncrementalSteps ? ImmutableArray.Create((entry.Step!, entry.OutputIndex)) : default;
                if (entry.State == EntryState.Removed)
                {
                    nodeTable.TryRemoveEntries(TimeSpan.Zero, inputs);
                }
                else if (entry.State != EntryState.Cached || !nodeTable.TryUseCachedEntries(TimeSpan.Zero, inputs))
                {
                    var sourcesBuilder = new AdditionalSourcesCollection(_sourceExtension);
                    var diagnostics = DiagnosticBag.GetInstance();

                    SourceProductionContext context = new SourceProductionContext(sourcesBuilder, diagnostics, graphState.Compilation, cancellationToken);
                    try
                    {
                        var stopwatch = SharedStopwatch.StartNew();
                        _action(context, entry.Item, cancellationToken);
                        var sourcesAndDiagnostics = (sourcesBuilder.ToImmutable(), diagnostics.ToReadOnly());

                        if (entry.State != EntryState.Modified || !nodeTable.TryModifyEntry(sourcesAndDiagnostics, EqualityComparer<TOutput>.Default, stopwatch.Elapsed, inputs, entry.State))
                        {
                            nodeTable.AddEntry(sourcesAndDiagnostics, EntryState.Added, stopwatch.Elapsed, inputs, EntryState.Added);
                        }
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

        IIncrementalGeneratorNode<TOutput> IIncrementalGeneratorNode<TOutput>.WithComparer(IEqualityComparer<TOutput> comparer) => throw ExceptionUtilities.Unreachable();

        public IIncrementalGeneratorNode<(IEnumerable<GeneratedSourceText>, IEnumerable<Diagnostic>)> WithTrackingName(string name) => throw ExceptionUtilities.Unreachable();

        void IIncrementalGeneratorNode<TOutput>.RegisterOutput(IIncrementalGeneratorOutputNode output) => throw ExceptionUtilities.Unreachable();

        public void AppendOutputs(IncrementalExecutionContext context, CancellationToken cancellationToken)
        {
            // get our own state table
            Debug.Assert(context.TableBuilder is not null);
            var table = context.TableBuilder.GetLatestStateTableForNode(this);

            // add each non-removed entry to the context
            foreach (var ((sources, diagnostics), state, _, _) in table)
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

            if (context.GeneratorRunStateBuilder.RecordingExecutedSteps)
            {
                context.GeneratorRunStateBuilder.RecordStepsFromOutputNodeUpdate(table);
            }
        }
    }
}
