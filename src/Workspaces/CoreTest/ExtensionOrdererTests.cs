// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public sealed class ExtensionOrdererTests
{
    private sealed class Extension { }

    [Fact]
    public void TestNoCycle1()
    {
        var a = CreateExtension(name: "a", before: ["b"]);
        var b = CreateExtension(name: "b", before: ["c"]);
        var c = CreateExtension(name: "c", before: ["d"]);
        var d = CreateExtension(name: "d", before: ["e"]);
        var e = CreateExtension(name: "e");

        var extensions = new List<Lazy<Extension, OrderableMetadata>>() { d, b, a, c, e };

        // ExtensionOrderer.TestAccessor.CheckForCycles() will throw ArgumentException if cycle is detected.
        ExtensionOrderer.TestAccessor.CheckForCycles(extensions);
        var order = ExtensionOrderer.Order(extensions);
        VerifyOrder("abcde", order);
    }

    [Fact]
    public void TestNoCycle2()
    {
        var a = CreateExtension(name: "a", after: ["b"]);
        var b = CreateExtension(name: "b", after: ["c"]);
        var c = CreateExtension(name: "c", after: ["d"]);
        var d = CreateExtension(name: "d", after: ["e"]);
        var e = CreateExtension(name: "e");

        var extensions = new List<Lazy<Extension, OrderableMetadata>>() { d, b, a, c, e };

        // ExtensionOrderer.TestAccessor.CheckForCycles() will throw ArgumentException if cycle is detected.
        ExtensionOrderer.TestAccessor.CheckForCycles(extensions);
        var order = ExtensionOrderer.Order(extensions);
        VerifyOrder("edcba", order);
    }

    [Fact]
    public void TestNoCycle3()
    {
        var a = CreateExtension(name: "a", before: ["b", "c", "d", "e"]);
        var b = CreateExtension(name: "b", before: ["c", "d", "e"], after: ["a"]);
        var c = CreateExtension(name: "c", before: ["d", "e"], after: ["b", "a"]);
        var d = CreateExtension(name: "d", before: ["e"], after: ["c", "b", "a"]);
        var e = CreateExtension(name: "e", after: ["d", "c", "b", "a"]);

        var extensions = new List<Lazy<Extension, OrderableMetadata>>() { d, b, a, c, e };

        // ExtensionOrderer.TestAccessor.CheckForCycles() will throw ArgumentException if cycle is detected.
        ExtensionOrderer.TestAccessor.CheckForCycles(extensions);
        var order = ExtensionOrderer.Order(extensions);
        VerifyOrder("abcde", order);
    }

    [Fact]
    public void TestCycle1()
    {
        var a = CreateExtension(name: "a", before: ["b"]);
        var b = CreateExtension(name: "b", before: ["c"]);
        var c = CreateExtension(name: "c", before: ["d"]);
        var d = CreateExtension(name: "d", before: ["e"]);
        var e = CreateExtension(name: "e", before: ["a"]);

        var extensions = new List<Lazy<Extension, OrderableMetadata>>() { a, b, c, d, e };

        // ExtensionOrderer.TestAccessor.CheckForCycles() will throw ArgumentException when cycle is detected.
        Assert.Throws<ArgumentException>(() => ExtensionOrderer.TestAccessor.CheckForCycles(extensions));
        var order = ExtensionOrderer.Order(extensions);
        VerifyOrder("bcdea", order);
    }

    [Fact]
    public void TestCycle2()
    {
        var a = CreateExtension(name: "a", after: ["b"]);
        var b = CreateExtension(name: "b", after: ["c"]);
        var c = CreateExtension(name: "c", after: ["d"]);
        var d = CreateExtension(name: "d", after: ["e"]);
        var e = CreateExtension(name: "e", after: ["a"]);

        var extensions = new List<Lazy<Extension, OrderableMetadata>>() { a, b, c, d, e };

        // ExtensionOrderer.TestAccessor.CheckForCycles() will throw ArgumentException when cycle is detected.
        Assert.Throws<ArgumentException>(() => ExtensionOrderer.TestAccessor.CheckForCycles(extensions));
        var order = ExtensionOrderer.Order(extensions);
        VerifyOrder("edcba", order);
    }

    [Fact]
    public void TestCycle3()
    {
        var a = CreateExtension(name: "a");
        var b = CreateExtension(name: "b", before: ["a"], after: ["a"]);
        var c = CreateExtension(name: "c");

        var extensions = new List<Lazy<Extension, OrderableMetadata>>() { a, b, c };

        // ExtensionOrderer.TestAccessor.CheckForCycles() will throw ArgumentException when cycle is detected.
        Assert.Throws<ArgumentException>(() => ExtensionOrderer.TestAccessor.CheckForCycles(extensions));
        var order = ExtensionOrderer.Order(extensions);
        VerifyOrder("bac", order);
    }

    [Fact]
    public void TestCycle4()
    {
        var a = CreateExtension(name: "a");
        var b = CreateExtension(name: "b", before: ["b"], after: ["b"]);
        var c = CreateExtension(name: "c");

        var extensions = new List<Lazy<Extension, OrderableMetadata>>() { a, b, c };

        // ExtensionOrderer.TestAccessor.CheckForCycles() will throw ArgumentException when cycle is detected.
        Assert.Throws<ArgumentException>(() => ExtensionOrderer.TestAccessor.CheckForCycles(extensions));
        var order = ExtensionOrderer.Order(extensions);
        VerifyOrder("abc", order);
    }

    [Fact]
    public void TestCycle5()
    {
        var a = CreateExtension(name: "a", before: ["b"]);
        var b = CreateExtension(name: "b", before: ["c"]);
        var c = CreateExtension(name: "c", before: ["d"]);
        var d = CreateExtension(name: "d", before: ["e"]);
        var e = CreateExtension(name: "e", before: ["c"]);
        var f = CreateExtension(name: "f", before: ["g"]);
        var g = CreateExtension(name: "g");

        var extensions = new List<Lazy<Extension, OrderableMetadata>>() { a, b, c, d, e, f, g };

        // ExtensionOrderer.TestAccessor.CheckForCycles() will throw ArgumentException when cycle is detected.
        Assert.Throws<ArgumentException>(() => ExtensionOrderer.TestAccessor.CheckForCycles(extensions));
        var order = ExtensionOrderer.Order(extensions);
        VerifyOrder("abdecfg", order);
    }

    [Fact]
    public void TestCycle6()
    {
        var a = CreateExtension(name: "a", before: ["b"]);
        var b = CreateExtension(name: "b", before: ["c"]);
        var c = CreateExtension(name: "c", before: ["d"]);
        var d = CreateExtension(name: "d", before: ["e"]);
        var e = CreateExtension(name: "e", before: ["a"]);
        var f = CreateExtension(name: "f", before: ["g"]);
        var g = CreateExtension(name: "g");

        var extensions = new List<Lazy<Extension, OrderableMetadata>>() { a, b, c, d, e, f, g };

        // ExtensionOrderer.TestAccessor.CheckForCycles() will throw ArgumentException when cycle is detected.
        Assert.Throws<ArgumentException>(() => ExtensionOrderer.TestAccessor.CheckForCycles(extensions));
        var order = ExtensionOrderer.Order(extensions);
        VerifyOrder("bcdeafg", order);
    }

    [Fact]
    public void TestCycle7()
    {
        var a = CreateExtension(name: "a", before: ["b"]);
        var b = CreateExtension(name: "b", before: ["c"]);
        var c = CreateExtension(name: "c", before: ["a"]);
        var d = CreateExtension(name: "d", before: ["e"]);
        var e = CreateExtension(name: "e", before: ["f"]);
        var f = CreateExtension(name: "f", before: ["d"]);

        var extensions = new List<Lazy<Extension, OrderableMetadata>>() { a, b, c, d, e, f };

        // ExtensionOrderer.TestAccessor.CheckForCycles() will throw ArgumentException when cycle is detected.
        Assert.Throws<ArgumentException>(() => ExtensionOrderer.TestAccessor.CheckForCycles(extensions));
        var order = ExtensionOrderer.Order(extensions);
        VerifyOrder("bcaefd", order);
    }

    [Fact]
    public void TestCycle8()
    {
        var a = CreateExtension(name: "a", before: ["b"]);
        var b = CreateExtension(name: "b", before: ["c"]);
        var c = CreateExtension(name: "c", before: ["d"]);
        var d = CreateExtension(name: "d", before: ["e", "c"]);
        var e = CreateExtension(name: "e", before: ["f"]);
        var f = CreateExtension(name: "f", before: ["a"]);

        var extensions = new List<Lazy<Extension, OrderableMetadata>>() { a, b, c, d, e, f };

        // ExtensionOrderer.TestAccessor.CheckForCycles() will throw ArgumentException when cycle is detected.
        Assert.Throws<ArgumentException>(() => ExtensionOrderer.TestAccessor.CheckForCycles(extensions));
        var order = ExtensionOrderer.Order(extensions);
        VerifyOrder("bcdefa", order);
    }

    #region Helpers

    private static Lazy<Extension, OrderableMetadata> CreateExtension(string? name = null, IEnumerable<string>? before = null, IEnumerable<string>? after = null)
        => new(new OrderableMetadata(name, before: before, after: after));

    private static IEnumerable<string?> GetNames(IEnumerable<Lazy<Extension, OrderableMetadata>> actual)
        => actual.Select(i => i.Metadata.Name);

    private static void VerifyOrder(string expected, IEnumerable<Lazy<Extension, OrderableMetadata>> actual)
    {
        var actualOrder = string.Join(string.Empty, GetNames(actual));
        Assert.Equal(expected, actualOrder);
    }

    #endregion
}
