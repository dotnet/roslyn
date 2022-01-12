// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class SyntaxReceiverStrategy<T> : ISyntaxSelectionStrategy<T>
    {
        private readonly SyntaxContextReceiverCreator _receiverCreator;
        private readonly Action<IIncrementalGeneratorOutputNode> _registerOutput;

        public SyntaxReceiverStrategy(SyntaxContextReceiverCreator receiverCreator, Action<IIncrementalGeneratorOutputNode> registerOutput)
        {
            _receiverCreator = receiverCreator;
            _registerOutput = registerOutput;
        }

        public ISyntaxInputBuilder GetBuilder(StateTableStore table, object key, bool trackIncrementalSteps, string? name, IEqualityComparer<T>? comparer) => new Builder(this, key, table, trackIncrementalSteps);

        private sealed class Builder : ISyntaxInputBuilder
        {
            private readonly SyntaxReceiverStrategy<T> _owner;
            private readonly object _key;
            private readonly NodeStateTable<ISyntaxContextReceiver?>.Builder _nodeStateTable;
            private readonly ISyntaxContextReceiver? _receiver;
            private readonly GeneratorSyntaxWalker? _walker;
            private TimeSpan lastElapsedTime;

            public Builder(SyntaxReceiverStrategy<T> owner, object key, StateTableStore driverStateTable, bool trackIncrementalSteps)
            {
                _owner = owner;
                _key = key;
                _nodeStateTable = driverStateTable.GetStateTableOrEmpty<ISyntaxContextReceiver?>(_key).ToBuilder(stepName: null, trackIncrementalSteps);
                try
                {
                    _receiver = owner._receiverCreator();
                }
                catch (Exception e)
                {
                    throw new UserFunctionException(e);
                }

                if (_receiver is object)
                {
                    _walker = new GeneratorSyntaxWalker(_receiver);
                }
            }

            private bool TrackIncrementalSteps => _nodeStateTable.TrackIncrementalSteps;

            public void SaveStateAndFree(StateTableStore.Builder tables)
            {
                _nodeStateTable.AddEntry(_receiver, EntryState.Modified, lastElapsedTime, TrackIncrementalSteps ? System.Collections.Immutable.ImmutableArray<(IncrementalGeneratorRunStep, int)>.Empty : default, EntryState.Modified);
                tables.SetTable(_key, _nodeStateTable.ToImmutableAndFree());
            }

            public void VisitTree(Lazy<SyntaxNode> root, EntryState state, SemanticModel? model, CancellationToken cancellationToken)
            {
                if (_walker is not null && state != EntryState.Removed)
                {
                    Debug.Assert(model is not null);
                    try
                    {
                        var stopwatch = SharedStopwatch.StartNew();
                        _walker.VisitWithModel(model, root.Value);
                        if (TrackIncrementalSteps)
                        {
                            lastElapsedTime = stopwatch.Elapsed;
                        }
                    }
                    catch (Exception e) when (!ExceptionUtilities.IsCurrentOperationBeingCancelled(e, cancellationToken))
                    {
                        throw new UserFunctionException(e);
                    }
                }
            }
        }
    }
}
