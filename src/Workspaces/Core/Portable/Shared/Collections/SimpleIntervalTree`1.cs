// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

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

        protected IIntervalIntrospector<T> Introspector
        {
            get { return _introspector; }
        }

        public IEnumerable<T> GetOverlappingIntervals(int start, int length)
        {
            return GetOverlappingIntervals(start, length, _introspector);
        }

        public IEnumerable<T> GetIntersectingIntervals(int start, int length)
        {
            return GetIntersectingIntervals(start, length, _introspector);
        }

        public IEnumerable<T> GetContainingIntervals(int start, int length)
        {
            return GetContainingIntervals(start, length, _introspector);
        }

        public bool IntersectsWith(int position)
        {
            return GetIntersectingIntervals(position, 0).Any();
        }

        protected int MaxEndValue(Node node)
        {
            return GetEnd(node.MaxEndNode.Value, _introspector);
        }
    }
}
