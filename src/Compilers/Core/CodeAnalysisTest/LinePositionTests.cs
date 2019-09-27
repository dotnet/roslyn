// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class LinePositionTests
    {
        [Fact]
        public void Equality1()
        {
            EqualityUtil.RunAll(
                (left, right) => left == right,
                (left, right) => left != right,
                EqualityUnit.Create(new LinePosition(1, 2)).WithEqualValues(new LinePosition(1, 2)),
                EqualityUnit.Create(new LinePosition()).WithEqualValues(new LinePosition()),
                EqualityUnit.Create(new LinePosition(1, 2)).WithNotEqualValues(new LinePosition(1, 3)),
                EqualityUnit.Create(new LinePosition(1, 2)).WithNotEqualValues(new LinePosition(2, 2)));
        }

        [Fact]
        public void Ctor1()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => { var notUsed = new LinePosition(-1, 42); });
        }

        [Fact]
        public void Ctor2()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => { var notUsed = new LinePosition(42, -1); });
        }

        [Fact]
        public void Ctor3()
        {
            var lp = new LinePosition(42, 13);
            Assert.Equal(42, lp.Line);
            Assert.Equal(13, lp.Character);
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
            var hash1 = new LinePosition(1, 1).GetHashCode();
            var hash2 = new LinePosition(2, 2).GetHashCode();
            var hash3 = new LinePosition(1, 2).GetHashCode();
            var hash4 = new LinePosition(2, 1).GetHashCode();

            Assert.NotEqual(hash1, hash2);
            Assert.NotEqual(hash1, hash3);
            Assert.NotEqual(hash1, hash4);
            Assert.NotEqual(hash2, hash3);
            Assert.NotEqual(hash2, hash4);
            Assert.NotEqual(hash3, hash4);
        }

#endif

        [Fact]
        public void CompareTo()
        {
            Assert.Equal(0, new LinePosition(1, 1).CompareTo(new LinePosition(1, 1)));

            Assert.Equal(-1, Math.Sign(new LinePosition(1, 1).CompareTo(new LinePosition(1, 2))));
            Assert.True(new LinePosition(1, 1) < new LinePosition(1, 2));

            Assert.Equal(-1, Math.Sign(new LinePosition(1, 2).CompareTo(new LinePosition(2, 1))));
            Assert.True(new LinePosition(1, 2) < new LinePosition(2, 1));
            Assert.True(new LinePosition(1, 2) <= new LinePosition(1, 2));
            Assert.True(new LinePosition(1, 2) <= new LinePosition(2, 1));

            Assert.Equal(+1, Math.Sign(new LinePosition(1, 2).CompareTo(new LinePosition(1, 1))));
            Assert.True(new LinePosition(1, 2) > new LinePosition(1, 1));

            Assert.Equal(+1, Math.Sign(new LinePosition(2, 1).CompareTo(new LinePosition(1, 2))));
            Assert.True(new LinePosition(2, 1) > new LinePosition(1, 2));
            Assert.True(new LinePosition(2, 1) >= new LinePosition(2, 1));
            Assert.True(new LinePosition(2, 1) >= new LinePosition(1, 2));
        }
    }
}
