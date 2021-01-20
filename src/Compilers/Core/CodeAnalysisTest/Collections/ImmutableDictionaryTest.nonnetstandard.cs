// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.2/src/libraries/System.Collections.Immutable/tests/ImmutableDictionaryTest.nonnetstandard.cs
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
    public partial class ImmutableDictionaryTest : ImmutableDictionaryTestBase
    {
        [Fact]
        public override void EmptyTest()
        {
            base.EmptyTest();
            this.EmptyTestHelperHash(Empty<int, bool>(), 5);
        }

        [Fact]
        public void EnumeratorWithHashCollisionsTest()
        {
            var emptyMap = Empty<int, GenericParameterHelper>(new BadHasher<int>());
            this.EnumeratorTestHelper(emptyMap);
        }

        internal override IBinaryTree GetRootNode<TKey, TValue>(IImmutableDictionary<TKey, TValue> dictionary)
        {
            return ((ImmutableDictionary<TKey, TValue>)dictionary).Root;
        }

        private void EmptyTestHelperHash<TKey, TValue>(IImmutableDictionary<TKey, TValue> empty, TKey someKey)
        {
            Assert.Same(EqualityComparer<TKey>.Default, ((IHashKeyCollection<TKey>)empty).KeyComparer);
        }
    }
}
