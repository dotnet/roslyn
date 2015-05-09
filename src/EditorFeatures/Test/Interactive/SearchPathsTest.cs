// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Interactive;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Interactive
{
    public class SearchPathsTest
    {
        [Fact, Trait(Traits.Feature, Traits.Features.InteractiveHost)]
        public void ListOperations()
        {
            var sp = new SearchPaths();
            AssertEx.Equal(Array.Empty<string>(), sp.List.GetNewContent());
            Assert.Equal(0, sp.List.Version);

            sp.Add("foo");
            AssertEx.Equal(new[] { "foo" }, sp.List.GetNewContent());
            Assert.Equal(1, sp.List.Version);

            sp.AddRange(new[] { "bar" });
            AssertEx.Equal(new[] { "foo", "bar" }, sp.List.GetNewContent());
            Assert.Equal(2, sp.List.Version);

            sp.AddRange(new[] { "baz" });
            AssertEx.Equal(new[] { "foo", "bar", "baz" }, sp.List.GetNewContent());
            Assert.Equal(3, sp.List.Version);

            Assert.True(sp.Contains("bar"));
            AssertEx.Equal(null, sp.List.GetNewContent());
            Assert.Equal(3, sp.List.Version);

            var a = new string[sp.Count + 2];
            Assert.Equal(3, sp.List.Version);
            AssertEx.Equal(null, sp.List.GetNewContent());

            sp.CopyTo(a, 1);
            AssertEx.Equal(new[] { null, "foo", "bar", "baz", null }, a);
            AssertEx.Equal(null, sp.List.GetNewContent());
            Assert.Equal(3, sp.List.Version);

            AssertEx.Equal(new[] { "foo", "bar", "baz" }, sp);
            AssertEx.Equal(null, sp.List.GetNewContent());
            Assert.Equal(3, sp.List.Version);

            Assert.Equal(2, sp.IndexOf("baz"));
            AssertEx.Equal(null, sp.List.GetNewContent());
            Assert.Equal(3, sp.List.Version);

            sp.Insert(1, "goo");
            AssertEx.Equal(new[] { "foo", "goo", "bar", "baz" }, sp);
            AssertEx.Equal(new[] { "foo", "goo", "bar", "baz" }, sp.List.GetNewContent());
            Assert.Equal(4, sp.List.Version);

            Assert.False(sp.IsReadOnly);
            AssertEx.Equal(null, sp.List.GetNewContent());
            Assert.Equal(4, sp.List.Version);

            Assert.True(sp.Remove("bar"));
            Assert.Equal(5, sp.List.Version);
            AssertEx.Equal(new[] { "foo", "goo", "baz" }, sp);
            AssertEx.Equal(new[] { "foo", "goo", "baz" }, sp.List.GetNewContent());

            Assert.False(sp.Remove("___"));
            AssertEx.Equal(null, sp.List.GetNewContent());
            Assert.Equal(5, sp.List.Version);

            sp.RemoveAt(1);
            AssertEx.Equal(new[] { "foo", "baz" }, sp);
            AssertEx.Equal(new[] { "foo", "baz" }, sp.List.GetNewContent());
            Assert.Equal(6, sp.List.Version);

            sp.Clear();
            AssertEx.Equal(Array.Empty<string>(), sp.List.GetNewContent());
            Assert.Equal(7, sp.List.Version);

            sp.Clear();
            AssertEx.Equal(null, sp.List.GetNewContent());
            Assert.Equal(7, sp.List.Version);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.InteractiveHost)]
        public void Exceptions()
        {
            var sp = new SearchPaths();
            AssertEx.Equal(Array.Empty<string>(), sp.List.GetNewContent());
            Assert.Equal(0, sp.List.Version);

            Assert.Throws<ArgumentNullException>(() => sp.AddRange(null));
            AssertEx.Equal(null, sp.List.GetNewContent());
            Assert.Equal(0, sp.List.Version);

            Assert.Throws<ArgumentNullException>(() => sp.CopyTo(null, 0));
            AssertEx.Equal(null, sp.List.GetNewContent());
            Assert.Equal(0, sp.List.Version);

            Assert.Throws<ArgumentOutOfRangeException>(() => sp.Insert(10, ""));
            AssertEx.Equal(null, sp.List.GetNewContent());
            Assert.Equal(0, sp.List.Version);

            Assert.Throws<ArgumentOutOfRangeException>(() => sp.RemoveAt(-1));
            AssertEx.Equal(null, sp.List.GetNewContent());
            Assert.Equal(0, sp.List.Version);
        }
    }
}
