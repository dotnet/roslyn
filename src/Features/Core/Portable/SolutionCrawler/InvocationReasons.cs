// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal partial struct InvocationReasons : IEnumerable<string>
    {
        public static readonly InvocationReasons Empty = new InvocationReasons(ImmutableHashSet<string>.Empty);

        private readonly ImmutableHashSet<string> _reasons;

        public InvocationReasons(string reason)
            : this(ImmutableHashSet.Create<string>(reason))
        {
        }

        private InvocationReasons(ImmutableHashSet<string> reasons)
        {
            _reasons = reasons;
        }

        public bool Contains(string reason)
        {
            return _reasons.Contains(reason);
        }

        public InvocationReasons With(InvocationReasons invocationReasons)
        {
            return new InvocationReasons((_reasons ?? ImmutableHashSet<string>.Empty).Union(invocationReasons._reasons));
        }

        public InvocationReasons With(string reason)
        {
            return new InvocationReasons((_reasons ?? ImmutableHashSet<string>.Empty).Add(reason));
        }

        public bool IsEmpty
        {
            get
            {
                return _reasons == null || _reasons.Count == 0;
            }
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _reasons.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
