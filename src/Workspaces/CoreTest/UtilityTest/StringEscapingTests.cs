// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class StringEscapingTests
    {
        [Fact]
        public void TestEscaping()
        {
            Assert.Equal("abc", "abc".Escape('$', '?'));
            Assert.Equal($"abc${(int)'?':X2}", "abc?".Escape('$', '?'));
            Assert.Equal($"abc${(int)'$':X2}", "abc$".Escape('$', '?'));
            Assert.Equal($"abc${(int)'?':X2}def${(int)'!':X2}", "abc?def!".Escape('$', '?', '!'));
        }

        [Fact]
        public void TestUnescaping()
        {
            Assert.Equal("abc", "abc".Unescape('$'));
            Assert.Equal("abc?", $"abc${(int)'?':X2}".Unescape('$'));
            Assert.Equal("abc$", $"abc${(int)'$':X2}".Unescape('$'));
            Assert.Equal("abc?def!", $"abc${(int)'?':X2}def${(int)'!':X2}".Unescape('$'));
        }
    }
}