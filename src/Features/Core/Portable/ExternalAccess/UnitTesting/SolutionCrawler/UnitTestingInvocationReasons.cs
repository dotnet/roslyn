// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler
{
    [DataContract]
    internal partial struct UnitTestingInvocationReasons : IEnumerable<string>
    {
        public static readonly UnitTestingInvocationReasons Empty = new(ImmutableHashSet<string>.Empty);

        [DataMember(Order = 0)]
        public readonly ImmutableHashSet<string> Reasons;

        public UnitTestingInvocationReasons(string reason)
            : this(ImmutableHashSet.Create(reason))
        {
        }

        public UnitTestingInvocationReasons(ImmutableHashSet<string> reasons)
            => Reasons = reasons ?? ImmutableHashSet<string>.Empty;

        public bool IsEmpty => Reasons.IsEmpty;

        public bool Contains(string reason)
            => Reasons.Contains(reason);

        public UnitTestingInvocationReasons With(UnitTestingInvocationReasons invocationReasons)
            => new(Reasons.Union(invocationReasons.Reasons));

        public UnitTestingInvocationReasons With(string reason)
            => new(Reasons.Add(reason));

        public ImmutableHashSet<string>.Enumerator GetEnumerator()
            => Reasons.GetEnumerator();

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
            => Reasons.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => Reasons.GetEnumerator();

        public override string ToString()
            => string.Join("|", Reasons ?? ImmutableHashSet<string>.Empty);
    }
}
