// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using TOutput = System.Collections.Immutable.ImmutableArray<(string, string)>;

namespace Microsoft.CodeAnalysis.ExternalAccess.RazorCompiler
{
    internal sealed class HostOutputNode<TInput> : IIncrementalGeneratorOutputNode, IIncrementalGeneratorNode<TOutput>
    {
        private readonly IIncrementalGeneratorNode<TInput> _source;

        private readonly Action<HostProductionContext, TInput, CancellationToken> _action;

        public HostOutputNode(IIncrementalGeneratorNode<TInput> source, Action<HostProductionContext, TInput, CancellationToken> action)
        {
            _source = source;
            _action = action;
        }

        public IncrementalGeneratorOutputKind Kind => GeneratorDriver.HostKind;

        public NodeStateTable<TOutput> UpdateStateTable(DriverStateTable.Builder graphState, NodeStateTable<TOutput>? previousTable, CancellationToken cancellationToken)
        {
            string stepName = "HostOutput";
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
                    ArrayBuilder<(string, string)> output = ArrayBuilder<(string, string)>.GetInstance();
                    HostProductionContext context = new HostProductionContext(output);
                    var stopwatch = SharedStopwatch.StartNew();
                    _action(context, entry.Item, cancellationToken);
                    nodeTable.AddEntry(output.ToImmutableAndFree(), EntryState.Added, stopwatch.Elapsed, inputs, EntryState.Added);
                }
            }

            return nodeTable.ToImmutableAndFree();
        }

        public void AppendOutputs(IncrementalExecutionContext context, CancellationToken cancellationToken)
        {
            // get our own state table
            Debug.Assert(context.TableBuilder is not null);
            var table = context.TableBuilder!.GetLatestStateTableForNode(this);

            // add each non-removed entry to the context
            foreach (var (list, state, _, _) in table)
            {
                if (state != EntryState.Removed)
                {
                    context.HostOutputBuilder.AddRange(list);
                }
            }

            if (context.GeneratorRunStateBuilder.RecordingExecutedSteps)
            {
                context.GeneratorRunStateBuilder.RecordStepsFromOutputNodeUpdate(table);
            }
        }

        IIncrementalGeneratorNode<TOutput> IIncrementalGeneratorNode<TOutput>.WithComparer(IEqualityComparer<TOutput> comparer) => throw ExceptionUtilities.Unreachable();

        public IIncrementalGeneratorNode<TOutput> WithTrackingName(string name) => throw ExceptionUtilities.Unreachable();

        void IIncrementalGeneratorNode<TOutput>.RegisterOutput(IIncrementalGeneratorOutputNode output) => throw ExceptionUtilities.Unreachable();
    }
}
