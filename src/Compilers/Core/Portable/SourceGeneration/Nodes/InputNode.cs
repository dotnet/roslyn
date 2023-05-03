// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Input nodes are the 'root' nodes in the graph, and get their values from the inputs of the driver state table
    /// </summary>
    /// <typeparam name="T">The type of the input</typeparam>
    internal sealed class InputNode<T> : IIncrementalGeneratorNode<T>
    {
        private readonly Func<DriverStateTable.Builder, ImmutableArray<T>> _getInput;
        private readonly Action<ArrayBuilder<IIncrementalGeneratorOutputNode>, IIncrementalGeneratorOutputNode> _registerOutput;
        private readonly IEqualityComparer<T> _inputComparer;
        private readonly TransformFactory? _transformFactory;
        private readonly IEqualityComparer<T> _comparer;
        private readonly string? _name;

        public InputNode(Func<DriverStateTable.Builder, ImmutableArray<T>> getInput, IEqualityComparer<T>? inputComparer = null)
            : this(getInput, transformFactory: null, registerOutput: null, inputComparer: inputComparer, comparer: null)
        {
        }

        private InputNode(Func<DriverStateTable.Builder, ImmutableArray<T>> getInput, TransformFactory? transformFactory, Action<ArrayBuilder<IIncrementalGeneratorOutputNode>, IIncrementalGeneratorOutputNode>? registerOutput, IEqualityComparer<T>? inputComparer = null, IEqualityComparer<T>? comparer = null, string? name = null)
        {
            _getInput = getInput;
            _comparer = comparer ?? EqualityComparer<T>.Default;
            _inputComparer = inputComparer ?? EqualityComparer<T>.Default;
            _transformFactory = transformFactory;
            _registerOutput = registerOutput ?? ((_, _) => throw ExceptionUtilities.Unreachable());
            _name = name;
        }

        public TransformFactory TransformFactory => _transformFactory ?? throw ExceptionUtilities.Unreachable();

        public NodeStateTable<T> UpdateStateTable(DriverStateTable.Builder graphState, NodeStateTable<T>? previousTable, CancellationToken cancellationToken)
        {
            var stopwatch = SharedStopwatch.StartNew();
            var inputItems = _getInput(graphState);
            TimeSpan elapsedTime = stopwatch.Elapsed;

            // create a mutable hashset of the new items we can check against
            HashSet<T> itemsSet = new HashSet<T>(_inputComparer);
            foreach (var item in inputItems)
            {
                var added = itemsSet.Add(item);
                Debug.Assert(added);
            }

            var builder = graphState.CreateTableBuilder(previousTable, _name, _comparer);

            // We always have no inputs steps into an InputNode, but we track the difference between "no inputs" (empty collection) and "no step information" (default value)
            var noInputStepsStepInfo = builder.TrackIncrementalSteps ? ImmutableArray<(IncrementalGeneratorRunStep, int)>.Empty : default;

            if (previousTable is not null)
            {
                // for each item in the previous table, check if its still in the new items
                int itemIndex = 0;
                foreach (var (oldItem, _, _, _) in previousTable)
                {
                    if (itemsSet.Remove(oldItem))
                    {
                        // we're iterating the table, so know that it has entries
                        var usedCache = builder.TryUseCachedEntries(elapsedTime, noInputStepsStepInfo);
                        Debug.Assert(usedCache);
                    }
                    else if (inputItems.Length == previousTable.Count)
                    {
                        // When the number of items matches the previous iteration, we use a heuristic to mark the input as modified
                        // This allows us to correctly 'replace' items even when they aren't actually the same. In the case that the
                        // item really isn't modified, but a new item, we still function correctly as we mostly treat them the same,
                        // but will perform an extra comparison that is omitted in the pure 'added' case.
                        var modified = builder.TryModifyEntry(inputItems[itemIndex], _comparer, elapsedTime, noInputStepsStepInfo, EntryState.Modified);
                        Debug.Assert(modified);
                        itemsSet.Remove(inputItems[itemIndex]);
                    }
                    else
                    {
                        var removed = builder.TryRemoveEntries(elapsedTime, noInputStepsStepInfo);
                        Debug.Assert(removed);
                    }
                    itemIndex++;
                }
            }

            // any remaining new items are added
            foreach (var newItem in itemsSet)
            {
                builder.AddEntry(newItem, EntryState.Added, elapsedTime, noInputStepsStepInfo, EntryState.Added);
            }

            return builder.ToImmutableAndFree();
        }

        public IIncrementalGeneratorNode<T> WithComparer(IEqualityComparer<T> comparer)
        {
            return TransformFactory.WithComparerAndTrackingName(this, ApplyComparer, ApplyTrackingName, comparer, _name);
        }

        public IIncrementalGeneratorNode<T> WithTrackingName(string name)
        {
            return TransformFactory.WithComparerAndTrackingName(this, ApplyComparer, ApplyTrackingName, _comparer, name);
        }

        public InputNode<T> WithContext(TransformFactory transformFactory, Action<ArrayBuilder<IIncrementalGeneratorOutputNode>, IIncrementalGeneratorOutputNode> registerOutput)
        {
            return transformFactory.WithContext(this, ApplyContext, registerOutput);
        }

        public void RegisterOutput(ArrayBuilder<IIncrementalGeneratorOutputNode> outputNodes, IIncrementalGeneratorOutputNode output) => _registerOutput(outputNodes, output);

        private static IIncrementalGeneratorNode<T> ApplyComparer(IIncrementalGeneratorNode<T> node, IEqualityComparer<T>? comparer)
        {
            var inputNode = (InputNode<T>)node;
            if (inputNode._comparer == (comparer ?? EqualityComparer<T>.Default))
                return inputNode;

            return new InputNode<T>(inputNode._getInput, inputNode.TransformFactory, inputNode._registerOutput, inputNode._inputComparer, comparer, inputNode._name);
        }

        private static IIncrementalGeneratorNode<T> ApplyTrackingName(IIncrementalGeneratorNode<T> node, string? name)
        {
            var inputNode = (InputNode<T>)node;
            if (inputNode._name == name)
                return inputNode;

            return new InputNode<T>(inputNode._getInput, inputNode.TransformFactory, inputNode._registerOutput, inputNode._inputComparer, inputNode._comparer, name);
        }

        private static InputNode<T> ApplyContext(InputNode<T> node, TransformFactory transformFactory, Action<ArrayBuilder<IIncrementalGeneratorOutputNode>, IIncrementalGeneratorOutputNode> registerOutput)
        {
            return new InputNode<T>(node._getInput, transformFactory, registerOutput, node._inputComparer, node._comparer, node._name);
        }
    }
}
