// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class ExtensionOrdererTests : TestBase
    {
        private class Extension { }

        private class ExtensionMetadata : IOrderableMetadata
        {
            public string Name { get; }
            public IEnumerable<string> Before { get; }
            public IEnumerable<string> After { get; }

            public ExtensionMetadata(string name = null, IEnumerable<string> before = null, IEnumerable<string> after = null)
            {
                this.Name = name;
                this.Before = before;
                this.After = after;
            }
        }

        [Fact]
        public void TestNoCycle1()
        {
            var a = CreateExtension(name: "a", before: new[] { "b" });
            var b = CreateExtension(name: "b", before: new[] { "c" });
            var c = CreateExtension(name: "c", before: new[] { "d" });
            var d = CreateExtension(name: "d", before: new[] { "e" });
            var e = CreateExtension(name: "e");

            var extensions = new List<Lazy<Extension, ExtensionMetadata>>() { d, b, a, c, e };

            // ExtensionOrderer.CheckForCycles() will throw ArgumentException if cycle is detected.
            ExtensionOrderer.CheckForCycles(extensions);
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

            var extensions = new List<Lazy<Extension, ExtensionMetadata>>() { d, b, a, c, e };

            // ExtensionOrderer.CheckForCycles() will throw ArgumentException if cycle is detected.
            ExtensionOrderer.CheckForCycles(extensions);
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

            var extensions = new List<Lazy<Extension, ExtensionMetadata>>() { d, b, a, c, e };

            // ExtensionOrderer.CheckForCycles() will throw ArgumentException if cycle is detected.
            ExtensionOrderer.CheckForCycles(extensions);
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

            var extensions = new List<Lazy<Extension, ExtensionMetadata>>() { a, b, c, d, e };

            // ExtensionOrderer.CheckForCycles() will throw ArgumentException when cycle is detected.
            Assert.Throws<ArgumentException>(() => ExtensionOrderer.CheckForCycles(extensions));
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

            var extensions = new List<Lazy<Extension, ExtensionMetadata>>() { a, b, c, d, e };

            // ExtensionOrderer.CheckForCycles() will throw ArgumentException when cycle is detected.
            Assert.Throws<ArgumentException>(() => ExtensionOrderer.CheckForCycles(extensions));
            var order = ExtensionOrderer.Order(extensions);
            VerifyOrder("edcba", order);
        }

        [Fact]
        public void TestCycle3()
        {
            var a = CreateExtension(name: "a");
            var b = CreateExtension(name: "b", before: new[] { "a" }, after: new[] { "a" });
            var c = CreateExtension(name: "c");

            var extensions = new List<Lazy<Extension, ExtensionMetadata>>() { a, b, c };

            // ExtensionOrderer.CheckForCycles() will throw ArgumentException when cycle is detected.
            Assert.Throws<ArgumentException>(() => ExtensionOrderer.CheckForCycles(extensions));
            var order = ExtensionOrderer.Order(extensions);
            VerifyOrder("bac", order);
        }

        [Fact]
        public void TestCycle4()
        {
            var a = CreateExtension(name: "a");
            var b = CreateExtension(name: "b", before: new[] { "b" }, after: new[] { "b" });
            var c = CreateExtension(name: "c");

            var extensions = new List<Lazy<Extension, ExtensionMetadata>>() { a, b, c };

            // ExtensionOrderer.CheckForCycles() will throw ArgumentException when cycle is detected.
            Assert.Throws<ArgumentException>(() => ExtensionOrderer.CheckForCycles(extensions));
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

            var extensions = new List<Lazy<Extension, ExtensionMetadata>>() { a, b, c, d, e, f, g };

            // ExtensionOrderer.CheckForCycles() will throw ArgumentException when cycle is detected.
            Assert.Throws<ArgumentException>(() => ExtensionOrderer.CheckForCycles(extensions));
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

            var extensions = new List<Lazy<Extension, ExtensionMetadata>>() { a, b, c, d, e, f, g };

            // ExtensionOrderer.CheckForCycles() will throw ArgumentException when cycle is detected.
            Assert.Throws<ArgumentException>(() => ExtensionOrderer.CheckForCycles(extensions));
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

            var extensions = new List<Lazy<Extension, ExtensionMetadata>>() { a, b, c, d, e, f };

            // ExtensionOrderer.CheckForCycles() will throw ArgumentException when cycle is detected.
            Assert.Throws<ArgumentException>(() => ExtensionOrderer.CheckForCycles(extensions));
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

            var extensions = new List<Lazy<Extension, ExtensionMetadata>>() { a, b, c, d, e, f };

            // ExtensionOrderer.CheckForCycles() will throw ArgumentException when cycle is detected.
            Assert.Throws<ArgumentException>(() => ExtensionOrderer.CheckForCycles(extensions));
            var order = ExtensionOrderer.Order(extensions);
            VerifyOrder("bcdefa", order);
        }

        #region Helpers
        private Lazy<Extension, ExtensionMetadata> CreateExtension(string name = null, IEnumerable<string> before = null, IEnumerable<string> after = null)
        {
            return new Lazy<Extension, ExtensionMetadata>(new ExtensionMetadata(name, before, after));
        }

        private IEnumerable<string> GetNames(IEnumerable<Lazy<Extension, ExtensionMetadata>> actual)
        {
            return actual.Select(i => i.Metadata.Name);
        }

        private void VerifyOrder(IEnumerable<string> expected, IEnumerable<Lazy<Extension, ExtensionMetadata>> actual)
        {
            var expectedOrder = string.Join(string.Empty, expected);
            var actualOrder = string.Join(string.Empty, GetNames(actual));
            Assert.Equal(expectedOrder, actualOrder);
        }

        private void VerifyOrder(string expected, IEnumerable<Lazy<Extension, ExtensionMetadata>> actual)
        {
            var actualOrder = string.Join(string.Empty, GetNames(actual));
            Assert.Equal(expected, actualOrder);
        }
        #endregion
    }
}
