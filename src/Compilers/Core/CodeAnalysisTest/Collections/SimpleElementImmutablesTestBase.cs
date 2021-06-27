// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace System.Collections.Immutable.Tests
{
    public abstract class SimpleElementImmutablesTestBase : ImmutablesTestBase
    {
        protected abstract IEnumerable<T> GetEnumerableOf<T>(params T[] contents);

        protected IEnumerable<T> GetEnumerableOf<T>(IEnumerable<T> contents)
        {
            return GetEnumerableOf(contents.ToArray());
        }
    }
}
