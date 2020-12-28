﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal partial struct InvocationReasons : IEnumerable<string>
    {
        public static readonly InvocationReasons Empty = new(ImmutableHashSet<string>.Empty);

        private readonly ImmutableHashSet<string> _reasons;

        public InvocationReasons(string reason)
            : this(ImmutableHashSet.Create<string>(reason))
        {
        }

        private InvocationReasons(ImmutableHashSet<string> reasons)
            => _reasons = reasons;

        public bool Contains(string reason)
            => _reasons.Contains(reason);

        public InvocationReasons With(InvocationReasons invocationReasons)
            => new((_reasons ?? ImmutableHashSet<string>.Empty).Union(invocationReasons._reasons));

        public InvocationReasons With(string reason)
            => new((_reasons ?? ImmutableHashSet<string>.Empty).Add(reason));

        public bool IsEmpty
        {
            get
            {
                return _reasons == null || _reasons.Count == 0;
            }
        }

        public ImmutableHashSet<string>.Enumerator GetEnumerator()
            => _reasons.GetEnumerator();

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
            => _reasons.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => _reasons.GetEnumerator();

        public override string ToString()
            => string.Join("|", _reasons ?? ImmutableHashSet<string>.Empty);
    }
}
