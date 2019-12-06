// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class FileLinePositionSpanTests
    {
        [Fact]
        public void Equality1()
        {
            EqualityUtil.RunAll(
                (left, right) => object.Equals(left, right),
                (left, right) => !object.Equals(left, right),
                EqualityUnit.Create(new FileLinePositionSpan("C:\\goo.cs", new LinePosition(1, 2), new LinePosition(3, 5)))
                .WithNotEqualValues(new FileLinePositionSpan("C:\\Goo.cs", new LinePosition(1, 2), new LinePosition(3, 5))),
                EqualityUnit.Create(new FileLinePositionSpan("C:\\goo.cs", new LinePosition(1, 2), new LinePosition(3, 5)))
                .WithNotEqualValues(new FileLinePositionSpan("C:\\bar.cs", new LinePosition(1, 2), new LinePosition(3, 5))),
                EqualityUnit.Create(new FileLinePositionSpan("C:\\goo.cs", new LinePosition(1, 2), new LinePosition(3, 5)))
                .WithNotEqualValues(new FileLinePositionSpan("C:\\goo.cs", new LinePosition(1, 4), new LinePosition(3, 5))),
                EqualityUnit.Create(new FileLinePositionSpan("C:\\goo.cs", new LinePosition(1, 2), new LinePosition(3, 5)))
                .WithNotEqualValues(new FileLinePositionSpan("C:\\goo.cs", new LinePosition(1, 2), new LinePosition(4, 5))));
        }

        [Fact]
        public void Ctor1()
        {
            Assert.Throws<ArgumentNullException>(
                () =>
            {
                var notUsed = new FileLinePositionSpan(null, new LinePosition(1, 2), new LinePosition(3, 5));
            });

            Assert.Throws<ArgumentException>(
                () =>
            {
                var notUsed = new FileLinePositionSpan("C:\\goo.cs", new LinePosition(3, 2), new LinePosition(2, 4));
            });

            Assert.Throws<ArgumentException>(
                () =>
            {
                var notUsed = new FileLinePositionSpan("C:\\goo.cs", new LinePosition(1, 2), new LinePosition(1, 1));
            });
        }

        // In general, different values are not required to have different hash codes.
        // But for perf reasons we want hash functions with a good distribution, 
        // so we expect hash codes to differ if a single component is incremented.
        // But program correctness should be preserved even with a null hash function,
        // so we need a way to disable these tests during such correctness validation.
#if !DISABLE_GOOD_HASH_TESTS

        [Fact]
        public void SaneHashCode()
        {
            var hash1 = new FileLinePositionSpan("C:\\goo.cs", new LinePosition(1, 2), new LinePosition(3, 5)).GetHashCode();
            var hash2 = new FileLinePositionSpan("C:\\goo1.cs", new LinePosition(1, 2), new LinePosition(3, 5)).GetHashCode();
            var hash3 = new FileLinePositionSpan("C:\\goo.cs", new LinePosition(1, 3), new LinePosition(3, 5)).GetHashCode();
            var hash4 = new FileLinePositionSpan("C:\\goo.cs", new LinePosition(1, 2), new LinePosition(6, 5)).GetHashCode();
            var hash5 = new FileLinePositionSpan("C:\\goo.cs", new LinePosition(2, 2), new LinePosition(6, 5)).GetHashCode();
            var hash6 = new FileLinePositionSpan("C:\\goo.cs", new LinePosition(2, 2), new LinePosition(6, 8)).GetHashCode();

            Assert.NotEqual(hash1, hash2);
            Assert.NotEqual(hash1, hash3);
            Assert.NotEqual(hash3, hash4);
            Assert.NotEqual(hash4, hash5);
            Assert.NotEqual(hash5, hash6);
        }

#endif

        [Fact]
        public void TestToString()
        {
            Assert.Equal("C:\\goo.cs: (1,2)-(3,5)", new FileLinePositionSpan("C:\\goo.cs", new LinePosition(1, 2), new LinePosition(3, 5)).ToString());
            Assert.Equal("\\server\\goo.vb: (1,2)-(3,5)", new FileLinePositionSpan("\\server\\goo.vb", new LinePosition(1, 2), new LinePosition(3, 5)).ToString());
            Assert.Equal("~\\goo.huh: (1,2)-(3,5)", new FileLinePositionSpan("~\\goo.huh", new LinePosition(1, 2), new LinePosition(3, 5)).ToString());
        }
    }
}
