// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Collections.Immutable.Maps
{
    public class BidirectionalMapTests
    {
        [Fact]
        public void TestEmpty()
        {
            var map = BidirectionalMap<string, int>.Empty;

            Assert.Equal(0, map.Keys.Count());
            Assert.Equal(0, map.Values.Count());
            Assert.False(map.TryGetKey(0, out _));
            Assert.False(map.TryGetValue("0", out _));

            Assert.False(map.ContainsKey("0"));
            Assert.False(map.ContainsValue(0));
        }

        [Fact]
        public void TestMap()
        {
            var map = BidirectionalMap<string, int>.Empty
                .Add("0", 0)
                .Add("1", 1)
                .Add("2", 2);

            Assert.Equal(3, map.Keys.Count());
            Assert.Equal(3, map.Values.Count());
            Assert.True(map.TryGetKey(0, out var key));
            Assert.Equal("0", key);
            Assert.True(map.TryGetKey(1, out key));
            Assert.Equal("1", key);
            Assert.True(map.TryGetKey(2, out key));
            Assert.Equal("2", key);
            Assert.True(map.TryGetValue("0", out var value));
            Assert.Equal(0, value);
            Assert.True(map.TryGetValue("1", out value));
            Assert.Equal(1, value);
            Assert.True(map.TryGetValue("2", out value));
            Assert.Equal(2, value);

            Assert.True(map.ContainsKey("0"));
            Assert.True(map.ContainsKey("1"));
            Assert.True(map.ContainsKey("2"));

            Assert.True(map.ContainsValue(0));
            Assert.True(map.ContainsValue(1));
            Assert.True(map.ContainsValue(2));
        }

        [Fact]
        public void TestRemoveKey()
        {
            var map = BidirectionalMap<string, int>.Empty
                .Add("0", 0)
                .Add("1", 1)
                .Add("2", 2);

            var map2 = map.RemoveKey("1");

            Assert.True(map2.ContainsKey("0"));
            Assert.False(map2.ContainsKey("1"));
            Assert.True(map2.ContainsKey("2"));

            Assert.True(map2.ContainsValue(0));
            Assert.False(map2.ContainsValue(1));
            Assert.True(map2.ContainsValue(2));

            Assert.True(map.ContainsKey("0"));
            Assert.True(map.ContainsKey("1"));
            Assert.True(map.ContainsKey("2"));

            Assert.True(map.ContainsValue(0));
            Assert.True(map.ContainsValue(1));
            Assert.True(map.ContainsValue(2));
        }

        [Fact]
        public void TestRemoveValue()
        {
            var map = BidirectionalMap<string, int>.Empty
                .Add("0", 0)
                .Add("1", 1)
                .Add("2", 2);

            var map2 = map.RemoveValue(1);

            Assert.True(map2.ContainsKey("0"));
            Assert.False(map2.ContainsKey("1"));
            Assert.True(map2.ContainsKey("2"));

            Assert.True(map2.ContainsValue(0));
            Assert.False(map2.ContainsValue(1));
            Assert.True(map2.ContainsValue(2));

            Assert.True(map.ContainsKey("0"));
            Assert.True(map.ContainsKey("1"));
            Assert.True(map.ContainsKey("2"));

            Assert.True(map.ContainsValue(0));
            Assert.True(map.ContainsValue(1));
            Assert.True(map.ContainsValue(2));
        }
    }
}
