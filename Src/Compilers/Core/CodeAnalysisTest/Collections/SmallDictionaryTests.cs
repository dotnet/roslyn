// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    public class SmallDictionaryTests
    {

        [Fact]
        public void SmallDictionaryAlwaysBalanced()
        {
            var sd = new SmallDictionary<int, string>();

            sd.Add(1, "1");
            sd.AssertBalanced();

            sd.Add(10, "1");
            sd.AssertBalanced();

            sd.Add(2, "1");
            sd.AssertBalanced();

            sd.Add(1000, "1");
            sd.AssertBalanced();

            sd.Add(-123, "1");
            sd.AssertBalanced();

            sd.Add(0, "1");
            sd.AssertBalanced();

            sd.Add(4, "1");
            sd.AssertBalanced();

            sd.Add(5, "1");
            sd.AssertBalanced();

            sd.Add(6, "1");
            sd.AssertBalanced();
        }

        [Fact]
        public void TestSmallDict()
        {
            var ht = new SmallDictionary<int, int>();
            const int elements = 150; 

            for (int i = 0; i < elements; i += 4)
            {
                ht.Add(i, i);
                ht.Add(i - 1, i - 1);
                ht.Add(i - 2, i - 2);
                ht.Add(i - 3, i - 3);
            }

            Assert.Equal(152, ht.Count());

            for (int j = 0; j < 100; j++)
            {
                for (int i = 0; i < elements; i += 4)
                {
                    int v;
                    ht.TryGetValue(i, out v);
                    Assert.Equal(i, v);

                    ht.TryGetValue(i - 1, out v);
                    Assert.Equal(i - 1, v);

                    ht.TryGetValue(i - 2, out v);
                    Assert.Equal(i - 2, v);

                    ht.TryGetValue(i - 3, out v);
                    Assert.Equal(i - 3, v);
                }
            }

            foreach(var p in ht)
            {
                Assert.Equal(p.Key, p.Value);
            }

            var keys = ht.Keys.ToArray();
            var values = ht.Values.ToArray();

            for (int i = 0, l = ht.Count(); i < l; i++)
            {
                Assert.Equal(keys[i], values[i]);
            }
        }

    }
}
