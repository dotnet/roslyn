// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

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

        public ImmutableArray<T> GetOverlappingIntervals(int start, int length)
            => GetOverlappingIntervals(start, length, _introspector);

        public ImmutableArray<T> GetIntersectingIntervals(int start, int length)
            => GetIntersectingIntervals(start, length, _introspector);

        public ImmutableArray<T> GetContainingIntervals(int start, int length)
            => GetContainingIntervals(start, length, _introspector);

        public void FillOverlappingIntervals(int start, int length, ArrayBuilder<T> builder)
            => FillOverlappingIntervals(start, length, _introspector, builder);

        public void FillIntersectingIntervals(int start, int length, ArrayBuilder<T> builder)
            => FillIntersectingIntervals(start, length, _introspector, builder);

        public void FillContainingIntervals(int start, int length, ArrayBuilder<T> builder)
            => FillContainingIntervals(start, length, _introspector, builder);

        public bool HasIntervalThatIntersectsWith(int position)
            => HasIntervalThatIntersectsWith(position, 0);

        public bool HasIntervalThatOverlapsWith(int start, int length)
            => Any(start, length, s_overlapsWithTest, _introspector);

        public bool HasIntervalThatIntersectsWith(int start, int length)
            => Any(start, length, s_intersectsWithTest, _introspector);

        public bool HasIntervalThatContains(int start, int length)
            => Any(start, length, s_containsTest, _introspector);

        protected int MaxEndValue(Node node)
            => GetEnd(node.MaxEndNode.Value, _introspector);
    }
}