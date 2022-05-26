// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Collections.Immutable.Maps
{
    public class MapTests
    {
        [Fact]
        public void TestEnumerator()
        {
            var map = ImmutableDictionary.Create<string, int>().Add("1", 1);
            foreach (var v in map)
            {
                Assert.Equal("1", v.Key);
                Assert.Equal(1, v.Value);
            }

            foreach (var v in (IEnumerable)map)
            {
                Assert.Equal("1", ((KeyValuePair<string, int>)v).Key);
                Assert.Equal(1, ((KeyValuePair<string, int>)v).Value);
            }
        }

        [Fact]
        public void TestCounts()
        {
            var map = ImmutableDictionary.Create<string, int>()
                .Add("1", 1)
                .Add("2", 2)
                .Add("3", 3)
                .Add("4", 4)
                .Add("5", 5);

            Assert.Equal(5, map.Count);
            Assert.Equal(5, map.Keys.Count());
            Assert.Equal(5, map.Values.Count());
        }

        [Fact]
        public void TestRemove()
        {
            var map = ImmutableDictionary.Create<string, int>()
                .Add("1", 1)
                .Add("2", 2)
                .Add("3", 3)
                .Add("4", 4)
                .Add("5", 5);

            map = map.Remove("0");
            Assert.Equal(5, map.Count);

            map = map.Remove("1");
            Assert.Equal(4, map.Count);

            map = map.Remove("1");
            Assert.Equal(4, map.Count);

            map = map.Remove("2");
            Assert.Equal(3, map.Count);

            map = map.Remove("3");
            Assert.Equal(2, map.Count);

            map = map.Remove("4");
            Assert.Equal(1, map.Count);

            map = map.Remove("5");
            Assert.Equal(0, map.Count);
        }

        [Fact]
        public void TestPathology()
        {
            var map = ImmutableDictionary.Create<string, int>(new PathologicalComparer<string>())
                .Add("1", 1)
                .Add("2", 2)
                .Add("3", 3)
                .Add("4", 4)
                .Add("5", 5);

            Assert.Equal(5, map.Count);
            Assert.Equal(1, map["1"]);
            Assert.Equal(2, map["2"]);
            Assert.Equal(3, map["3"]);

            map = map.Remove("0");
            Assert.Equal(5, map.Count);

            map = map.Remove("3");
            Assert.Equal(4, map.Count);

            map = map.Remove("3");
            Assert.Equal(4, map.Count);

            map = map.Remove("2");
            Assert.Equal(3, map.Count);

            map = map.Remove("1");
            Assert.Equal(2, map.Count);

            map = map.Remove("4");
            Assert.Equal(1, map.Count);

            map = map.Remove("6");
            Assert.Equal(1, map.Count);

            map = map.Remove("5");
            Assert.Equal(0, map.Count);
        }

        private class PathologicalComparer<T> : IEqualityComparer<T>
        {
            public bool Equals(T x, T y)
                => EqualityComparer<T>.Default.Equals(x, y);

            public int GetHashCode(T obj)
                => 0;
        }
    }
}
