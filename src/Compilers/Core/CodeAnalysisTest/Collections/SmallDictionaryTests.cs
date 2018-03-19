﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            for (int i = 0; i < elements; i += 10)
            {
                ht.Add(i, i);
                ht.AssertBalanced();
                ht.Add(i - 2, i - 2);
                ht.AssertBalanced();
                ht.Add(i - 3, i - 3);
                ht.AssertBalanced();
                ht.Add(i - 4, i - 4);
                ht.AssertBalanced();
                ht.Add(i - 6, i - 6);
                ht.AssertBalanced();
                ht.Add(i - 5, i - 5);
                ht.AssertBalanced();
                ht.Add(i - 1, i - 1);
                ht.AssertBalanced();
                ht.Add(i - 7, i - 7);
                ht.AssertBalanced();
                ht.Add(i - 8, i - 8);
                ht.AssertBalanced();
                ht.Add(i - 9, i - 9);
                ht.AssertBalanced();
            }

            Assert.Equal(150, ht.Count());

            for (int i = 0; i < elements; i += 10)
            {
                Assert.Equal(ht[i], i);
                Assert.Equal(ht[i - 2], i - 2);
                Assert.Equal(ht[i - 3], i - 3);
                Assert.Equal(ht[i - 4], i - 4);
                Assert.Equal(ht[i - 6], i - 6);
                Assert.Equal(ht[i - 5], i - 5);
                Assert.Equal(ht[i - 1], i - 1);
                Assert.Equal(ht[i - 7], i - 7);
                Assert.Equal(ht[i - 8], i - 8);
                Assert.Equal(ht[i - 9], i - 9);
            }

            foreach (KeyValuePair<int, int> p in ht)
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
