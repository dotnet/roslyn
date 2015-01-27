// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal partial struct InvocationReasons : IEnumerable<string>
    {
        public static readonly InvocationReasons Empty = new InvocationReasons(ImmutableHashSet.Create<string>());

        private ImmutableHashSet<string> reasons;

        public InvocationReasons(string reason)
            : this(ImmutableHashSet.Create<string>(reason))
        {
        }

        private InvocationReasons(ImmutableHashSet<string> reasons)
        {
            this.reasons = reasons;
        }

        public bool Contains(string reason)
        {
            return this.reasons.Contains(reason);
        }

        public InvocationReasons With(InvocationReasons invocationReasons)
        {
            return new InvocationReasons((reasons ?? ImmutableHashSet.Create<string>()).Union(invocationReasons.reasons));
        }

        public bool IsEmpty
        {
            get
            {
                return this.reasons == null || this.reasons.Count == 0;
            }
        }

        public IEnumerator<string> GetEnumerator()
        {
            return this.reasons.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
