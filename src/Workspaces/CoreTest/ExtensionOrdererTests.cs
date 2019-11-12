// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class ExtensionOrdererTests
    {
        private class Extension { }

        [Fact]
        public void TestNoCycle1()
        {
            var a = CreateExtension(name: "a", before: new[] { "b" });
            var b = CreateExtension(name: "b", before: new[] { "c" });
            var c = CreateExtension(name: "c", before: new[] { "d" });
            var d = CreateExtension(name: "d", before: new[] { "e" });
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
            var a = CreateExtension(name: "a", after: new[] { "b" });
            var b = CreateExtension(name: "b", after: new[] { "c" });
            var c = CreateExtension(name: "c", after: new[] { "d" });
            var d = CreateExtension(name: "d", after: new[] { "e" });
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
            var a = CreateExtension(name: "a", before: new[] { "b", "c", "d", "e" });
            var b = CreateExtension(name: "b", before: new[] { "c", "d", "e" }, after: new[] { "a" });
            var c = CreateExtension(name: "c", before: new[] { "d", "e" }, after: new[] { "b", "a" });
            var d = CreateExtension(name: "d", before: new[] { "e" }, after: new[] { "c", "b", "a" });
            var e = CreateExtension(name: "e", after: new[] { "d", "c", "b", "a" });

            var extensions = new List<Lazy<Extension, OrderableMetadata>>() { d, b, a, c, e };

            // ExtensionOrderer.TestAccessor.CheckForCycles() will throw ArgumentException if cycle is detected.
            ExtensionOrderer.TestAccessor.CheckForCycles(extensions);
            var order = ExtensionOrderer.Order(extensions);
            VerifyOrder("abcde", order);
        }

        [Fact]
        public void TestCycle1()
        {
            var a = CreateExtension(name: "a", before: new[] { "b" });
            var b = CreateExtension(name: "b", before: new[] { "c" });
            var c = CreateExtension(name: "c", before: new[] { "d" });
            var d = CreateExtension(name: "d", before: new[] { "e" });
            var e = CreateExtension(name: "e", before: new[] { "a" });

            var extensions = new List<Lazy<Extension, OrderableMetadata>>() { a, b, c, d, e };

            // ExtensionOrderer.TestAccessor.CheckForCycles() will throw ArgumentException when cycle is detected.
            Assert.Throws<ArgumentException>(() => ExtensionOrderer.TestAccessor.CheckForCycles(extensions));
            var order = ExtensionOrderer.Order(extensions);
            VerifyOrder("bcdea", order);
        }

        [Fact]
        public void TestCycle2()
        {
            var a = CreateExtension(name: "a", after: new[] { "b" });
            var b = CreateExtension(name: "b", after: new[] { "c" });
            var c = CreateExtension(name: "c", after: new[] { "d" });
            var d = CreateExtension(name: "d", after: new[] { "e" });
            var e = CreateExtension(name: "e", after: new[] { "a" });

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
            var b = CreateExtension(name: "b", before: new[] { "a" }, after: new[] { "a" });
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
            var b = CreateExtension(name: "b", before: new[] { "b" }, after: new[] { "b" });
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
            var a = CreateExtension(name: "a", before: new[] { "b" });
            var b = CreateExtension(name: "b", before: new[] { "c" });
            var c = CreateExtension(name: "c", before: new[] { "d" });
            var d = CreateExtension(name: "d", before: new[] { "e" });
            var e = CreateExtension(name: "e", before: new[] { "c" });
            var f = CreateExtension(name: "f", before: new[] { "g" });
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
            var a = CreateExtension(name: "a", before: new[] { "b" });
            var b = CreateExtension(name: "b", before: new[] { "c" });
            var c = CreateExtension(name: "c", before: new[] { "d" });
            var d = CreateExtension(name: "d", before: new[] { "e" });
            var e = CreateExtension(name: "e", before: new[] { "a" });
            var f = CreateExtension(name: "f", before: new[] { "g" });
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
            var a = CreateExtension(name: "a", before: new[] { "b" });
            var b = CreateExtension(name: "b", before: new[] { "c" });
            var c = CreateExtension(name: "c", before: new[] { "a" });
            var d = CreateExtension(name: "d", before: new[] { "e" });
            var e = CreateExtension(name: "e", before: new[] { "f" });
            var f = CreateExtension(name: "f", before: new[] { "d" });

            var extensions = new List<Lazy<Extension, OrderableMetadata>>() { a, b, c, d, e, f };

            // ExtensionOrderer.TestAccessor.CheckForCycles() will throw ArgumentException when cycle is detected.
            Assert.Throws<ArgumentException>(() => ExtensionOrderer.TestAccessor.CheckForCycles(extensions));
            var order = ExtensionOrderer.Order(extensions);
            VerifyOrder("bcaefd", order);
        }

        [Fact]
        public void TestCycle8()
        {
            var a = CreateExtension(name: "a", before: new[] { "b" });
            var b = CreateExtension(name: "b", before: new[] { "c" });
            var c = CreateExtension(name: "c", before: new[] { "d" });
            var d = CreateExtension(name: "d", before: new[] { "e", "c" });
            var e = CreateExtension(name: "e", before: new[] { "f" });
            var f = CreateExtension(name: "f", before: new[] { "a" });

            var extensions = new List<Lazy<Extension, OrderableMetadata>>() { a, b, c, d, e, f };

            // ExtensionOrderer.TestAccessor.CheckForCycles() will throw ArgumentException when cycle is detected.
            Assert.Throws<ArgumentException>(() => ExtensionOrderer.TestAccessor.CheckForCycles(extensions));
            var order = ExtensionOrderer.Order(extensions);
            VerifyOrder("bcdefa", order);
        }

        #region Helpers

        private Lazy<Extension, OrderableMetadata> CreateExtension(string? name = null, IEnumerable<string>? before = null, IEnumerable<string>? after = null)
        {
            return new Lazy<Extension, OrderableMetadata>(new OrderableMetadata(name, before: before, after: after));
        }

        private IEnumerable<string?> GetNames(IEnumerable<Lazy<Extension, OrderableMetadata>> actual)
        {
            return actual.Select(i => i.Metadata.Name);
        }

        private void VerifyOrder(string expected, IEnumerable<Lazy<Extension, OrderableMetadata>> actual)
        {
            var actualOrder = string.Join(string.Empty, GetNames(actual));
            Assert.Equal(expected, actualOrder);
        }

        #endregion
    }
}
