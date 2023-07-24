// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    internal sealed class TransformFactory
    {
        private readonly Dictionary<IIncrementalGeneratorNode, IIncrementalGeneratorNode> _baseNodeForWithContext = new();
        private readonly Dictionary<(IIncrementalGeneratorNode, Action<ArrayBuilder<IIncrementalGeneratorOutputNode>, IIncrementalGeneratorOutputNode>), IIncrementalGeneratorNode> _withContext = new();
        private readonly Dictionary<IIncrementalGeneratorNode, IIncrementalGeneratorNode> _baseNodeForComparerAndTrackingName = new();
        private readonly Dictionary<(IIncrementalGeneratorNode node, object? comparer), IIncrementalGeneratorNode> _withComparer = new();
        private readonly Dictionary<(IIncrementalGeneratorNode, string?), IIncrementalGeneratorNode> _withTrackingName = new();
        private readonly Dictionary<object, object> _wrappedUserObjects = new();
        private readonly Dictionary<Delegate, Delegate> _wrappedUserFunctionsAsImmutableArray = new();
        private readonly Dictionary<Delegate, Delegate> _wrappedPredicateForSelectMany = new();

        /// <summary>
        /// Map from a generator node to the result of calling <see cref="IncrementalValueProviderExtensions.Collect{TSource}"/> on that generator node.
        /// </summary>
        private readonly Dictionary<IIncrementalGeneratorNode, IIncrementalGeneratorNode> _collectedNodes = new();

        /// <summary>
        /// Map from a pair of generator nodes to the result of calling <see cref="Combine{TLeft, TRight}"/> on those nodes.
        /// </summary>
        private readonly Dictionary<(IIncrementalGeneratorNode left, IIncrementalGeneratorNode right), IIncrementalGeneratorNode> _combinedNodes = new();

        /// <summary>
        /// Map from a generator node and selector to the result of calling <see cref="Select{TSource, TResult}"/>.
        /// </summary>
        private readonly Dictionary<(IIncrementalGeneratorNode node, Delegate selector), IIncrementalGeneratorNode> _selectNodes = new();

        /// <summary>
        /// Map from a generator node and selector to the result of calling <see cref="SelectMany{TSource, TResult}"/>.
        /// </summary>
        private readonly Dictionary<(IIncrementalGeneratorNode node, Delegate selector), IIncrementalGeneratorNode> _selectManyNodes = new();

        public T WithContext<T>(
            T node,
            Func<T, TransformFactory, Action<ArrayBuilder<IIncrementalGeneratorOutputNode>, IIncrementalGeneratorOutputNode>, T> applyContext,
            Action<ArrayBuilder<IIncrementalGeneratorOutputNode>, IIncrementalGeneratorOutputNode> registerOutput)
            where T : IIncrementalGeneratorNode
        {
            // Always start with the root node without context to maximize cache hits
            var nodeWithoutContext = GetBaseNodeForContext(node);
            if (_withContext.TryGetValue((nodeWithoutContext, registerOutput), out var nodeWithContext))
                return (T)nodeWithContext;

            var nodeTWithContext = applyContext(nodeWithoutContext, this, registerOutput);
            _baseNodeForWithContext.Add(nodeTWithContext, nodeWithoutContext);
            _withContext.Add((nodeWithoutContext, registerOutput), nodeTWithContext);
            return nodeTWithContext;
        }

        public IIncrementalGeneratorNode<T> WithComparerAndTrackingName<T>(
            IIncrementalGeneratorNode<T> node,
            Func<IIncrementalGeneratorNode<T>, IEqualityComparer<T>?, IIncrementalGeneratorNode<T>> applyComparer,
            Func<IIncrementalGeneratorNode<T>, string?, IIncrementalGeneratorNode<T>> applyTrackingName,
            IEqualityComparer<T>? comparer,
            string? trackingName)
        {
            // Always start with a common root node, and then apply the comparer and tracking name in order to maximize
            // cache hits
            var baseNode = GetBaseNodeForComparerAndTrackingName(node);
            if (!_withComparer.TryGetValue((baseNode, comparer), out var nodeWithComparer))
            {
                nodeWithComparer = applyComparer(baseNode, comparer);
                _withComparer.Add((baseNode, comparer), nodeWithComparer);
            }

            if (_withTrackingName.TryGetValue((nodeWithComparer, trackingName), out var nodeWithName))
                return (IIncrementalGeneratorNode<T>)nodeWithName;

            var nodeTWithName = applyTrackingName((IIncrementalGeneratorNode<T>)nodeWithComparer, trackingName);
            _baseNodeForComparerAndTrackingName.Add(nodeTWithName, baseNode);
            _withTrackingName.Add((nodeWithComparer, trackingName), nodeTWithName);
            return nodeTWithName;
        }

        private T GetBaseNodeForContext<T>(T node)
            where T : IIncrementalGeneratorNode
        {
            return (T)_baseNodeForWithContext.GetOrAdd(node, node);
        }

        private IIncrementalGeneratorNode<T> GetBaseNodeForComparerAndTrackingName<T>(IIncrementalGeneratorNode<T> node)
        {
            return (IIncrementalGeneratorNode<T>)_baseNodeForComparerAndTrackingName.GetOrAdd(node, node);
        }

        internal IEqualityComparer<T> WrapUserComparer<T>(IEqualityComparer<T> comparer)
        {
            if (_wrappedUserObjects.TryGetValue(comparer, out var wrappedComparer))
                return (IEqualityComparer<T>)wrappedComparer;

            var wrappedComparerT = comparer.WrapUserComparer();
            _wrappedUserObjects.Add(comparer, wrappedComparerT);
            return wrappedComparerT;
        }

        internal Func<TInput, CancellationToken, TOutput> WrapUserFunction<TInput, TOutput>(Func<TInput, CancellationToken, TOutput> userFunction)
        {
            if (_wrappedUserObjects.TryGetValue(userFunction, out var wrappedUserFunction))
                return (Func<TInput, CancellationToken, TOutput>)wrappedUserFunction;

            var wrappedUserFunctionT = userFunction.WrapUserFunction();
            _wrappedUserObjects.Add(userFunction, wrappedUserFunctionT);
            return wrappedUserFunctionT;
        }

        internal Func<TInput, CancellationToken, ImmutableArray<TOutput>> WrapUserFunctionAsImmutableArray<TInput, TOutput>(Func<TInput, CancellationToken, IEnumerable<TOutput>> userFunction)
        {
            if (_wrappedUserFunctionsAsImmutableArray.TryGetValue(userFunction, out var wrappedUserFunctionAsImmutableArray))
                return (Func<TInput, CancellationToken, ImmutableArray<TOutput>>)wrappedUserFunctionAsImmutableArray;

            var wrappedUserFunctionAsImmutableArrayT = userFunction.WrapUserFunctionAsImmutableArray();
            _wrappedUserFunctionsAsImmutableArray.Add(userFunction, wrappedUserFunctionAsImmutableArrayT);
            return wrappedUserFunctionAsImmutableArrayT;
        }

        internal Func<TSource, CancellationToken, ImmutableArray<TSource>> WrapPredicateForSelectMany<TSource>(Func<TSource, bool> predicate)
        {
            if (_wrappedPredicateForSelectMany.TryGetValue(predicate, out var wrappedPredicateForSelectMany))
                return (Func<TSource, CancellationToken, ImmutableArray<TSource>>)wrappedPredicateForSelectMany;

            var wrappedPredicateForSelectManyT = predicate.WrapPredicateForSelectMany();
            _wrappedPredicateForSelectMany.Add(predicate, wrappedPredicateForSelectManyT);
            return wrappedPredicateForSelectManyT;
        }

        internal Func<TSource, CancellationToken, ImmutableArray<TSource>> WrapPredicateForSelectMany<TSource>(Func<TSource, CancellationToken, bool> predicate)
        {
            if (_wrappedPredicateForSelectMany.TryGetValue(predicate, out var wrappedPredicateForSelectMany))
                return (Func<TSource, CancellationToken, ImmutableArray<TSource>>)wrappedPredicateForSelectMany;

            var wrappedPredicateForSelectManyT = predicate.WrapPredicateForSelectMany();
            _wrappedPredicateForSelectMany.Add(predicate, wrappedPredicateForSelectManyT);
            return wrappedPredicateForSelectManyT;
        }

        internal BatchNode<TSource> Collect<TSource>(IIncrementalGeneratorNode<TSource> node)
        {
            if (_collectedNodes.TryGetValue(node, out var collectedNode))
                return (BatchNode<TSource>)collectedNode;

            var batchNode = new BatchNode<TSource>(node);
            _collectedNodes.Add(node, batchNode);
            return batchNode;
        }

        internal CombineNode<TLeft, TRight> Combine<TLeft, TRight>(IIncrementalGeneratorNode<TLeft> node1, IIncrementalGeneratorNode<TRight> node2)
        {
            if (_combinedNodes.TryGetValue((node1, node2), out var combinedNode))
                return (CombineNode<TLeft, TRight>)combinedNode;

            var combineNode = new CombineNode<TLeft, TRight>(node1, node2);
            _combinedNodes.Add((node1, node2), combineNode);
            return combineNode;
        }

        internal TransformNode<TSource, TResult> Select<TSource, TResult>(IIncrementalGeneratorNode<TSource> node, Func<TSource, CancellationToken, TResult> selector)
        {
            if (_selectNodes.TryGetValue((node, selector), out var transformedNode))
                return (TransformNode<TSource, TResult>)transformedNode;

            var transformNodeT = new TransformNode<TSource, TResult>(node, selector);
            _selectNodes.Add((node, selector), transformNodeT);
            return transformNodeT;
        }

        internal TransformNode<TSource, TResult> SelectMany<TSource, TResult>(IIncrementalGeneratorNode<TSource> node, Func<TSource, CancellationToken, ImmutableArray<TResult>> selector)
        {
            if (_selectManyNodes.TryGetValue((node, selector), out var transformedNode))
                return (TransformNode<TSource, TResult>)transformedNode;

            var transformNodeT = new TransformNode<TSource, TResult>(node, selector);
            _selectManyNodes.Add((node, selector), transformNodeT);
            return transformNodeT;
        }
    }
}
