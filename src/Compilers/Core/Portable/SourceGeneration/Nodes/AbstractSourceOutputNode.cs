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
    internal abstract class AbstractSourceOutputNode<TInput> : IIncrementalGeneratorOutputNode, IIncrementalGeneratorNode<TOutput>
    {
        private static readonly string? s_tableType = typeof(TOutput).FullName;

        private readonly IIncrementalGeneratorNode<TInput> _source;
        private readonly string _sourceExtension;

        protected AbstractSourceOutputNode(IIncrementalGeneratorNode<TInput> source, string sourceExtension)
        {
            _source = source;
            _sourceExtension = sourceExtension;
        }

        public abstract IncrementalGeneratorOutputKind Kind { get; }

        protected abstract string StepName { get; }

        protected abstract void InvokeUserAction(AdditionalSourcesCollection sources, DiagnosticBag diagnostics, DriverStateTable.Builder graphState, TInput item, CancellationToken cancellationToken);

        public NodeStateTable<TOutput> UpdateStateTable(DriverStateTable.Builder graphState, NodeStateTable<TOutput>? previousTable, CancellationToken cancellationToken)
        {
            var stepName = StepName;
            var sourceTable = graphState.GetLatestStateTableForNode(_source);
            if (sourceTable.IsCached && previousTable is not null)
            {
                this.LogTables(stepName, s_tableType, previousTable, previousTable, sourceTable);
                if (graphState.DriverState.TrackIncrementalSteps)
                {
                    return previousTable.CreateCachedTableWithUpdatedSteps(sourceTable, stepName, equalityComparer: null);
                }
                return previousTable;
            }

            var tableBuilder = graphState.CreateTableBuilder(previousTable, stepName, equalityComparer: null);
            foreach (var entry in sourceTable)
            {
                var inputs = tableBuilder.TrackIncrementalSteps ? ImmutableArray.Create((entry.Step!, entry.OutputIndex)) : default;
                if (entry.State == EntryState.Removed)
                {
                    tableBuilder.TryRemoveEntries(TimeSpan.Zero, inputs);
                }
                else if (entry.State != EntryState.Cached || !tableBuilder.TryUseCachedEntries(TimeSpan.Zero, inputs))
                {
                    var sourcesBuilder = new AdditionalSourcesCollection(_sourceExtension);
                    var diagnostics = DiagnosticBag.GetInstance();

                    try
                    {
                        var stopwatch = SharedStopwatch.StartNew();
                        InvokeUserAction(sourcesBuilder, diagnostics, graphState, entry.Item, cancellationToken);
                        var sourcesAndDiagnostics = (sourcesBuilder.ToImmutable(), diagnostics.ToReadOnly());

                        if (entry.State != EntryState.Modified || !tableBuilder.TryModifyEntry(sourcesAndDiagnostics, stopwatch.Elapsed, inputs, entry.State))
                        {
                            tableBuilder.AddEntry(sourcesAndDiagnostics, EntryState.Added, stopwatch.Elapsed, inputs, EntryState.Added);
                        }
                    }
                    finally
                    {
                        sourcesBuilder.Free();
                        diagnostics.Free();
                    }
                }
            }

            var newTable = tableBuilder.ToImmutableAndFree();
            this.LogTables(stepName, s_tableType, previousTable, newTable, sourceTable);
            return newTable;
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
