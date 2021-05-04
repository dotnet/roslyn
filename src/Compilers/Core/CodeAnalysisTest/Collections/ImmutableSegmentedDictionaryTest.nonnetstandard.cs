// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.2/src/libraries/System.Collections.Immutable/tests/ImmutableDictionaryTest.nonnetstandard.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    public partial class ImmutableSegmentedDictionaryTest : ImmutableDictionaryTestBase
    {
        [Fact]
        public override void EmptyTest()
        {
            base.EmptyTest();
            EmptyTestHelperHash(Empty<int, bool>(), 5);
        }

        [Fact]
        public void EnumeratorWithHashCollisionsTest()
        {
            var emptyMap = Empty<int, GenericParameterHelper>(new BadHasher<int>());
            EnumeratorTestHelper(emptyMap);
        }

        private static void EmptyTestHelperHash<TKey, TValue>(IImmutableDictionary<TKey, TValue> empty, TKey someKey)
            where TKey : notnull
        {
            // Intentionally not used
            _ = someKey;

            Assert.Same(EqualityComparer<TKey>.Default, empty.GetKeyComparer());
        }
    }
}
