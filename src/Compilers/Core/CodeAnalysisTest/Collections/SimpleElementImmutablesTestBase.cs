// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://raw.githubusercontent.com/dotnet/runtime/v6.0.0-preview.5.21301.5/src/libraries/System.Collections.Immutable/tests/SimpleElementImmutablesTestBase.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
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
