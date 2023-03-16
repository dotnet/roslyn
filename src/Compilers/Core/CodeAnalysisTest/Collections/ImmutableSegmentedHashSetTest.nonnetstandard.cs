// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.8/src/libraries/System.Collections.Immutable/tests/ImmutableHashSetTest.nonnetstandard.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Collections;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    public partial class ImmutableSegmentedHashSetTest : ImmutableSetTest
    {
        [Fact]
        public void EmptyTest()
        {
            EmptyTestHelper(Empty<int>(), 5, null);
            EmptyTestHelper(EmptyTyped<string>().WithComparer(StringComparer.OrdinalIgnoreCase), "a", StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void TryGetValueTest()
        {
            TryGetValueTestHelper(ImmutableSegmentedHashSet<string>.Empty.WithComparer(StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Tests various aspects of an unordered set.
        /// </summary>
        /// <typeparam name="T">The type of element stored in the set.</typeparam>
        /// <param name="emptySet">The empty set.</param>
        /// <param name="value">A value that could be placed in the set.</param>
        /// <param name="comparer">The comparer used to obtain the empty set, if any.</param>
        private static void EmptyTestHelper<T>(System.Collections.Immutable.IImmutableSet<T> emptySet, T value, IEqualityComparer<T>? comparer)
        {
            Assert.NotNull(emptySet);

            EmptyTestHelper(emptySet);
            Assert.True(IsSame(emptySet, emptySet.ToImmutableSegmentedHashSet(comparer)));
            Assert.Same(comparer ?? EqualityComparer<T>.Default, GetEqualityComparer(emptySet));

            if (comparer == null)
            {
                Assert.True(IsSame(emptySet, ImmutableSegmentedHashSet<T>.Empty));
            }

            var reemptied = emptySet.Add(value).Clear();
            Assert.True(IsSame(reemptied, reemptied.ToImmutableSegmentedHashSet(comparer))); //, "Getting the empty set from a non-empty instance did not preserve the comparer.");
        }
    }
}
