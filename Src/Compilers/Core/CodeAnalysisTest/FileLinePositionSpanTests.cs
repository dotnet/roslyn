// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                EqualityUnit.Create(new FileLinePositionSpan("C:\\foo.cs", new LinePosition(1, 2), new LinePosition(3, 5)))
                .WithNotEqualValues(new FileLinePositionSpan("C:\\Foo.cs", new LinePosition(1, 2), new LinePosition(3, 5))),
                EqualityUnit.Create(new FileLinePositionSpan("C:\\foo.cs", new LinePosition(1, 2), new LinePosition(3, 5)))
                .WithNotEqualValues(new FileLinePositionSpan("C:\\bar.cs", new LinePosition(1, 2), new LinePosition(3, 5))),
                EqualityUnit.Create(new FileLinePositionSpan("C:\\foo.cs", new LinePosition(1, 2), new LinePosition(3, 5)))
                .WithNotEqualValues(new FileLinePositionSpan("C:\\foo.cs", new LinePosition(1, 4), new LinePosition(3, 5))),
                EqualityUnit.Create(new FileLinePositionSpan("C:\\foo.cs", new LinePosition(1, 2), new LinePosition(3, 5)))
                .WithNotEqualValues(new FileLinePositionSpan("C:\\foo.cs", new LinePosition(1, 2), new LinePosition(4, 5))));
        }

        [Fact]
        public void Ctor1()
        {
            Assert.Throws(
                typeof(ArgumentNullException),
                () =>
            {
                var notUsed = new FileLinePositionSpan(null, new LinePosition(1, 2), new LinePosition(3, 5));
            });

            Assert.Throws(
                typeof(ArgumentException),
                () =>
            {
                var notUsed = new FileLinePositionSpan("C:\\foo.cs", new LinePosition(3, 2), new LinePosition(2, 4));
            });

            Assert.Throws(
                typeof(ArgumentException),
                () =>
            {
                var notUsed = new FileLinePositionSpan("C:\\foo.cs", new LinePosition(1, 2), new LinePosition(1, 1));
            });
        }

        [Fact]
        public void SaneHashCode()
        {
            var hash1 = new FileLinePositionSpan("C:\\foo.cs", new LinePosition(1, 2), new LinePosition(3, 5)).GetHashCode();
            var hash2 = new FileLinePositionSpan("C:\\foo1.cs", new LinePosition(1, 2), new LinePosition(3, 5)).GetHashCode();
            var hash3 = new FileLinePositionSpan("C:\\foo.cs", new LinePosition(1, 3), new LinePosition(3, 5)).GetHashCode();
            var hash4 = new FileLinePositionSpan("C:\\foo.cs", new LinePosition(1, 1), new LinePosition(6, 5)).GetHashCode();

            Assert.NotEqual(hash1, hash2);
            Assert.NotEqual(hash1, hash3);
            Assert.NotEqual(hash1, hash4);
            Assert.NotEqual(hash2, hash3);
            Assert.NotEqual(hash2, hash4);
            Assert.NotEqual(hash3, hash4);
        }

        [Fact]
        public void TestToString()
        {
            Assert.Equal("C:\\foo.cs: (1,2)-(3,5)", new FileLinePositionSpan("C:\\foo.cs", new LinePosition(1, 2), new LinePosition(3, 5)).ToString());
            Assert.Equal("\\server\foo.vb: (1,2)-(3,5)", new FileLinePositionSpan("\\server\foo.vb", new LinePosition(1, 2), new LinePosition(3, 5)).ToString());
            Assert.Equal("~\foo.huh: (1,2)-(3,5)", new FileLinePositionSpan("~\foo.huh", new LinePosition(1, 2), new LinePosition(3, 5)).ToString());
        }
    }
}
