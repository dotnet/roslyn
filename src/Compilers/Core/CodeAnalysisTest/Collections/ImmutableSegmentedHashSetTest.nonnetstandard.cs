// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.8/src/libraries/System.Collections.Immutable/tests/ImmutableHashSetTest.nonnetstandard.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Collections.Immutable.Tests
{
    public partial class ImmutableHashSetTest : ImmutableSetTest
    {
        [Fact]
        public void EmptyTest()
        {
            this.EmptyTestHelper(Empty<int>(), 5, null);
            this.EmptyTestHelper(EmptyTyped<string>().WithComparer(StringComparer.OrdinalIgnoreCase), "a", StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void TryGetValueTest()
        {
            this.TryGetValueTestHelper(ImmutableHashSet<string>.Empty.WithComparer(StringComparer.OrdinalIgnoreCase));
        }

        internal override IBinaryTree GetRootNode<T>(IImmutableSet<T> set)
        {
            return ((ImmutableHashSet<T>)set).Root;
        }

        /// <summary>
        /// Tests various aspects of an unordered set.
        /// </summary>
        /// <typeparam name="T">The type of element stored in the set.</typeparam>
        /// <param name="emptySet">The empty set.</param>
        /// <param name="value">A value that could be placed in the set.</param>
        /// <param name="comparer">The comparer used to obtain the empty set, if any.</param>
        private void EmptyTestHelper<T>(IImmutableSet<T> emptySet, T value, IEqualityComparer<T> comparer)
        {
            Assert.NotNull(emptySet);

            this.EmptyTestHelper(emptySet);
            Assert.Same(emptySet, emptySet.ToImmutableHashSet(comparer));
            Assert.Same(comparer ?? EqualityComparer<T>.Default, ((IHashKeyCollection<T>)emptySet).KeyComparer);

            if (comparer == null)
            {
                Assert.Same(emptySet, ImmutableHashSet<T>.Empty);
            }

            var reemptied = emptySet.Add(value).Clear();
            Assert.Same(reemptied, reemptied.ToImmutableHashSet(comparer)); //, "Getting the empty set from a non-empty instance did not preserve the comparer.");
        }
    }
}
