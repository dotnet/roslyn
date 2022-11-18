// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    public class RopeTests
    {
        private static readonly string[] longStrings = new[]
        {
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.  ",
            "Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.  ",
            "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.  ",
            "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.  ",
            // So true
        };
        private static readonly Rope[] longRopes = longStrings.Select(s => Rope.ForString(s)).ToArray();

        private static readonly string[] shortStrings = new[]
        {
            "abcd", "efgh", "ijkl", "mnop", "qrst", "uvwx", "yz01", "2345", "6789"
        };
        private static readonly Rope[] shortRopes = shortStrings.Select(s => Rope.ForString(s)).ToArray();

        private static readonly Rope[] someRopes = shortRopes.Concat(longRopes).ToArray();
        private static readonly string[] someStrings = shortStrings.Concat(longStrings).ToArray();

        [Fact]
        public void Empty()
        {
            Rope r = Rope.Empty;
            Assert.Equal(0, r.Length);
            Assert.Equal(string.Empty, r.ToString());
            Assert.Equal(r, Rope.ForString(string.Empty));
            Assert.Equal(Rope.ForString(string.Empty), r);
        }

        [Fact]
        public void EmptyConcatenation()
        {
            foreach (var rope in someRopes)
            {
                Assert.Same(rope, Rope.Concat(rope, Rope.Empty));
                Assert.Same(rope, Rope.Concat(Rope.Empty, rope));
            }
        }

        [Fact]
        public void ForString()
        {
            foreach (var s in someStrings)
            {
                Assert.Equal(s, Rope.ForString(s).ToString());
            }
        }

        [Fact]
        public void Concatenation()
        {
            foreach (var r1 in someRopes)
            {
                foreach (var r2 in someRopes)
                {
                    foreach (var r3 in someRopes)
                    {
                        Rope c1 = Rope.Concat(r1, Rope.Concat(r2, r3));
                        Rope c2 = Rope.Concat(Rope.Concat(r1, r2), r3);
                        Assert.Equal(c1, c2); // associative
                        Assert.Equal(c1.GetHashCode(), c2.GetHashCode());
                        string s = r1.ToString() + r2.ToString() + r3.ToString();
                        Assert.Equal(c1.ToString(), s);
                        Assert.Equal(c2.ToString(), s);
                        Rope c3 = Rope.ForString(s);
                        Assert.Equal(c1.GetHashCode(), c3.GetHashCode());
                        Assert.Equal(c1, c3);
                        Assert.Equal(c2, c3);
                        Assert.Equal(c3.ToString(), s);
                    }
                }
            }
        }

        [Fact]
        public void ForNullString()
        {
            Assert.Throws<ArgumentNullException>(() => { Rope.ForString(null); });
        }

        [Fact]
        public void ConcatNull()
        {
            foreach (var r in someRopes)
            {
                Assert.Throws<ArgumentNullException>(() => { Rope.Concat(r, null); });
                Assert.Throws<ArgumentNullException>(() => { Rope.Concat(null, r); });
            }
        }

        [Fact]
        public void MaxLength()
        {
            var r = shortRopes.Aggregate(Rope.Concat);
            var concatted = shortStrings.Aggregate((a, b) => a + b);
            Assert.Equal(r.Length, concatted.Length);

            for (int i = 0; i < concatted.Length; i++)
            {
                Assert.Equal(concatted[..i], r.ToString(i));
            }
        }

        [Fact]
        public void MaxLength_Invalid()
        {
            var r = Rope.ForString("x");
            Assert.Throws<InvalidOperationException>(() => r.ToString(-1));
        }

        [Fact]
        public void Overflow()
        {
            Rope r = Rope.ForString("x");
            Rope all = r;
            Assert.Equal(1, all.Length);
            for (int i = 1; i < 31; i++)
            {
                r = Rope.Concat(r, r);
                all = Rope.Concat(all, r);
                Assert.Equal(1 << i, r.Length);
                Assert.Equal((1 << (i + 1)) - 1, all.Length);
            }

            Assert.Equal(int.MaxValue, all.Length);
            var waferThinMint = Rope.ForString("y");
            Assert.Throws<OverflowException>(() => Rope.Concat(all, waferThinMint));
            Assert.Throws<OverflowException>(() => Rope.Concat(waferThinMint, all));
            Assert.Throws<OverflowException>(() => Rope.Concat(all, all));
        }
    }
}
