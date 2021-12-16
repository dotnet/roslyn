// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.InternalUtilities
{
    public class ConcurrentDictionaryExtensionsTests
    {
        [Fact]
        public void TestAdd()
        {
            var dictionary = new ConcurrentDictionary<int, int>();
            dictionary.Add(0, 0);
            Assert.Equal(0, dictionary[0]);

            Assert.Throws<ArgumentException>(() => dictionary.Add(0, 0));
        }

        [Fact]
        public void TestGetOrAdd()
        {
            var first = new object();
            var second = new object();

            var dictionary = new ConcurrentDictionary<int, object>();
            Assert.Same(first, dictionary.GetOrAdd(0, static (key, arg) => arg, first));
            Assert.Same(first, dictionary[0]);
            Assert.Single(dictionary);
            Assert.Same(first, dictionary.GetOrAdd(0, static (key, arg) => arg, second));
            Assert.Same(first, dictionary[0]);
            Assert.Single(dictionary);
        }
    }
}
