// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Collections.Immutable.Maps
{
    public class BidirectionalMapTests
    {
        [WpfFact]
        public void TestEmpty()
        {
            var map = BidirectionalMap<string, int>.Empty;

            Assert.Equal(map.Keys.Count(), 0);
            Assert.Equal(map.Values.Count(), 0);

            string key;
            int value;
            Assert.False(map.TryGetKey(0, out key));
            Assert.False(map.TryGetValue("0", out value));

            Assert.False(map.ContainsKey("0"));
            Assert.False(map.ContainsValue(0));
        }

        [WpfFact]
        public void TestMap()
        {
            var map = BidirectionalMap<string, int>.Empty
                .Add("0", 0)
                .Add("1", 1)
                .Add("2", 2);

            Assert.Equal(map.Keys.Count(), 3);
            Assert.Equal(map.Values.Count(), 3);

            string key;
            Assert.True(map.TryGetKey(0, out key));
            Assert.Equal(key, "0");
            Assert.True(map.TryGetKey(1, out key));
            Assert.Equal(key, "1");
            Assert.True(map.TryGetKey(2, out key));
            Assert.Equal(key, "2");

            int value;
            Assert.True(map.TryGetValue("0", out value));
            Assert.Equal(value, 0);
            Assert.True(map.TryGetValue("1", out value));
            Assert.Equal(value, 1);
            Assert.True(map.TryGetValue("2", out value));
            Assert.Equal(value, 2);

            Assert.True(map.ContainsKey("0"));
            Assert.True(map.ContainsKey("1"));
            Assert.True(map.ContainsKey("2"));

            Assert.True(map.ContainsValue(0));
            Assert.True(map.ContainsValue(1));
            Assert.True(map.ContainsValue(2));
        }

        [WpfFact]
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

        [WpfFact]
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
