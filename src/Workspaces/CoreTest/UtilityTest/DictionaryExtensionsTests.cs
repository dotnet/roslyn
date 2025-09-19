// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public class DictionaryExtensionsTests
{
    [Fact]
    public void RemoveAll_Empty()
    {
        var dictionary = new Dictionary<int, string>();
        dictionary.RemoveAll((_, _, _) => true, 0);
        Assert.Empty(dictionary);
    }

    [Fact]
    public void RemoveAll_RemovesMatchingEntries()
    {
        var dictionary = new Dictionary<int, string>
        {
            { 1, "one" },
            { 2, "two" },
            { 3, "three" }
        };

        dictionary.RemoveAll((key, value, arg) => value.StartsWith(arg), "t");

        Assert.Equal(1, dictionary.Count);
        Assert.True(dictionary.ContainsKey(1));
        Assert.False(dictionary.ContainsKey(2));
        Assert.False(dictionary.ContainsKey(3));
    }

    [Fact]
    public void RemoveAll_NoMatching()
    {
        var dictionary = new Dictionary<int, string>
        {
            { 1, "one" },
            { 2, "two" },
            { 3, "three" }
        };

        dictionary.RemoveAll((key, value, arg) => value.StartsWith(arg), "z");

        Assert.Equal(3, dictionary.Count);
        Assert.True(dictionary.ContainsKey(1));
        Assert.True(dictionary.ContainsKey(2));
        Assert.True(dictionary.ContainsKey(3));
    }

    [Fact]
    public void RemoveAll_AllMatching()
    {
        var dictionary = new Dictionary<int, string>
        {
            { 1, "test1" },
            { 2, "test2" },
            { 3, "test3" }
        };

        dictionary.RemoveAll((key, value, arg) => value.StartsWith(arg), "t");

        Assert.Empty(dictionary);
    }
}

