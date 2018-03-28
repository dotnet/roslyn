// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Shared.Collections
{
    internal class SimpleIntervalTree<T> : IntervalTree<T>
    {
        private readonly IIntervalIntrospector<T> _introspector;

        public SimpleIntervalTree(IIntervalIntrospector<T> introspector, IEnumerable<T> values)
        {
            _introspector = introspector;

            if (values != null)
            {
                foreach (var value in values)
                {
                    root = Insert(root, new Node(value), introspector);
                }
            }
        }

        protected IIntervalIntrospector<T> Introspector => _introspector;

        /// <summary>
        /// Warning.  Mutates the tree in place.
        /// </summary>
        /// <param name="value"></param>
        public void AddIntervalInPlace(T value)
        {
            var newNode = new Node(value);
            this.root = Insert(root, newNode, Introspector);
        }

        public ImmutableArray<T> GetIntervalsThatOverlapWith(int start, int length)
            => GetIntervalsThatOverlapWith(start, length, _introspector);

        public ImmutableArray<T> GetIntervalsThatIntersectWith(int start, int length)
            => GetIntervalsThatIntersectWith(start, length, _introspector);

        public ImmutableArray<T> GetIntervalsThatContain(int start, int length)
            => GetIntervalsThatContain(start, length, _introspector);

        public void FillWithIntervalsThatOverlapWith(int start, int length, ArrayBuilder<T> builder)
            => FillWithIntervalsThatOverlapWith(start, length, builder, _introspector);

        public void FillWithIntervalsThatIntersectWith(int start, int length, ArrayBuilder<T> builder)
            => FillWithIntervalsThatIntersectWith(start, length, builder, _introspector);

        public void FillWithIntervalsThatContain(int start, int length, ArrayBuilder<T> builder)
            => FillWithIntervalsThatContain(start, length, builder, _introspector);

        public bool HasIntervalThatIntersectsWith(int position)
            => HasIntervalThatIntersectsWith(position, _introspector);

        public bool HasIntervalThatOverlapsWith(int start, int length)
            => HasIntervalThatOverlapsWith(start, length, _introspector);

        public bool HasIntervalThatIntersectsWith(int start, int length)
            => HasIntervalThatIntersectsWith(start, length, _introspector);

        public bool HasIntervalThatContains(int start, int length)
            => HasIntervalThatContains(start, length, _introspector);

        protected int MaxEndValue(Node node)
            => GetEnd(node.MaxEndNode.Value, _introspector);
    }
}
