// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Collections;

internal static partial class IntervalTreeHelpers<T, TIntervalTree, TNode, TIntervalTreeWitness>
    where TIntervalTree : IIntervalTree<T>
    where TIntervalTreeWitness : struct, IIntervalTreeWitness<T, TIntervalTree, TNode>
{
    public struct Enumerator(TIntervalTree tree) : IEnumerator<T>
    {
        /// <summary>
        /// An introspector that always throws.  Used when we need to call an api that takes this, but we know will never
        /// call into it due to other arguments we pass along.
        /// </summary>
        private readonly struct AlwaysThrowIntrospector : IIntervalIntrospector<T>
        {
            public TextSpan GetSpan(T value) => throw new System.NotImplementedException();
        }

        private readonly TIntervalTree _tree = tree;

        /// <summary>
        /// Because we're passing the full span of all ints, we know that we'll never call into the introspector.  Since
        /// all intervals will always be in that span.
        /// </summary>
        private NodeEnumerator<AlwaysThrowIntrospector> _nodeEnumerator = new(tree, start: int.MinValue, end: int.MaxValue, default);

        public readonly T Current => default(TIntervalTreeWitness).GetValue(_tree, _nodeEnumerator.Current);

        readonly object IEnumerator.Current => this.Current!;

        public bool MoveNext() => _nodeEnumerator.MoveNext();
        public readonly void Reset() => _nodeEnumerator.Reset();
        public readonly void Dispose() => _nodeEnumerator.Dispose();
    }
}
