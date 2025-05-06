// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.InternalUtilities
{
    public class SpecializedCollectionsTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)] // use same tests against a BCL implementation to demonstrate test correctness.
        public void EmptySetRespectsInterfaceContract(bool useReferenceImplementation)
        {
            var emptySet = useReferenceImplementation
                ? ImmutableHashSet<int>.Empty
                : SpecializedCollections.EmptySet<int>();

            // IEnumerable
            Assert.Empty(emptySet);
            Assert.False(emptySet.GetEnumerator().MoveNext());
            Assert.False(((IEnumerable)emptySet).GetEnumerator().MoveNext());

            // ICollection (read-only safe)
            Assert.Equal(0, emptySet.Count);
            Assert.True(emptySet.IsReadOnly);
            Assert.False(emptySet.Contains(0));
            emptySet.CopyTo(new int[0], 0); // should not throw

            // ICollection (not supported when read-only)
            Assert.Throws<NotSupportedException>(() => ((ICollection<int>)(emptySet)).Add(0));
            Assert.Throws<NotSupportedException>(() => emptySet.Remove(0));
            Assert.Throws<NotSupportedException>(() => emptySet.Clear());

            // ISet (read-only safe)
            Assert.False(emptySet.IsProperSubsetOf(new int[0]));
            Assert.True(emptySet.IsProperSubsetOf(new int[1]));
            Assert.False(emptySet.IsProperSupersetOf(new int[0]));
            Assert.False(emptySet.IsProperSupersetOf(new int[1]));
            Assert.True(emptySet.IsSubsetOf(new int[0]));
            Assert.True(emptySet.IsSubsetOf(new int[1]));
            Assert.True(emptySet.IsSupersetOf(new int[0]));
            Assert.False(emptySet.IsSupersetOf(new int[1]));
            Assert.False(emptySet.Overlaps(new int[0]));
            Assert.False(emptySet.Overlaps(new int[1]));
            Assert.True(emptySet.SetEquals(new int[0]));
            Assert.False(emptySet.SetEquals(new int[1]));

            // ISet (not supported when read-only)
            Assert.Throws<NotSupportedException>(() => emptySet.Add(0));
            Assert.Throws<NotSupportedException>(() => emptySet.ExceptWith(new int[0]));
            Assert.Throws<NotSupportedException>(() => emptySet.IntersectWith(new int[0]));
            Assert.Throws<NotSupportedException>(() => emptySet.SymmetricExceptWith(new int[0]));
            Assert.Throws<NotSupportedException>(() => emptySet.UnionWith(new int[0]));
        }
    }
}
