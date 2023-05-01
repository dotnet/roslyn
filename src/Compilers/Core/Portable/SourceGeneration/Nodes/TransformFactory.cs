// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
    }
}
