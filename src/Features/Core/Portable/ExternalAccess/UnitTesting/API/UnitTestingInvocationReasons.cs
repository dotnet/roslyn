// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    [DataContract]
    internal partial struct UnitTestingInvocationReasons(ImmutableHashSet<string> reasons) : IEnumerable<string>
    {
        public static readonly UnitTestingInvocationReasons Empty = new(ImmutableHashSet<string>.Empty);

        [DataMember(Order = 0)]
        private readonly ImmutableHashSet<string> _reasons = reasons ?? ImmutableHashSet<string>.Empty;

        public UnitTestingInvocationReasons(string reason)
            : this(ImmutableHashSet.Create(reason))
        {
        }

        public bool IsEmpty => _reasons.IsEmpty;

        public bool Contains(string reason)
            => _reasons.Contains(reason);

        public UnitTestingInvocationReasons With(UnitTestingInvocationReasons invocationReasons)
            => new(_reasons.Union(invocationReasons._reasons));

        public UnitTestingInvocationReasons With(string reason)
            => new(_reasons.Add(reason));

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
