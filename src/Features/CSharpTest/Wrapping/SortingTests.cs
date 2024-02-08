// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Wrapping;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Wrapping
{
    public class SortingTests
    {
        [Fact]
        public void FirstNotInMruSecondNotInMru()
        {
            var items = ImmutableArray.Create("Action1", "Action2");

            var sorted = WrapItemsAction.SortByMostRecentlyUsed(
                items, [], a => a);

            // Shouldn't change order
            Assert.Equal((IEnumerable<string>)items, sorted);
        }

        [Fact]
        public void FirstInMruSecondNotInMru()
        {
            var items = ImmutableArray.Create("Action1", "Action2");

            var sorted = WrapItemsAction.SortByMostRecentlyUsed(
                items, ImmutableArray.Create("Action1"), a => a);

            // Shouldn't change order
            Assert.Equal((IEnumerable<string>)items, sorted);
        }

        [Fact]
        public void FirstNotInMruSecondInMru()
        {
            var items = ImmutableArray.Create("Action1", "Action2");

            var sorted = WrapItemsAction.SortByMostRecentlyUsed(
                items, ImmutableArray.Create("Action2"), a => a);

            // Should swap order.
            Assert.Equal((IEnumerable<string>)ImmutableArray.Create("Action2", "Action1"), sorted);
        }

        [Fact]
        public void FirstInMruSecondInMru1()
        {
            var items = ImmutableArray.Create("Action1", "Action2");

            var sorted = WrapItemsAction.SortByMostRecentlyUsed(
                items, ImmutableArray.Create("Action1", "Action2"), a => a);

            // Shouldn't change order
            Assert.Equal((IEnumerable<string>)items, sorted);
        }

        [Fact]
        public void FirstInMruSecondInMru2()
        {
            var items = ImmutableArray.Create("Action1", "Action2");

            var sorted = WrapItemsAction.SortByMostRecentlyUsed(
                items, ImmutableArray.Create("Action2", "Action1"), a => a);

            // Should swap order.
            Assert.Equal((IEnumerable<string>)ImmutableArray.Create("Action2", "Action1"), sorted);
        }
    }
}
