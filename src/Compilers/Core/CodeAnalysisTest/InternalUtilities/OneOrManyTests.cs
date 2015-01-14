// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.InternalUtilities
{
    public class OneOrManyTests : TestBase
    {
        [Fact]
        public void Positive()
        {
            var single = OneOrMany.Create(123);
            var quad = OneOrMany.Create(ImmutableArray.Create<int>(10, 20, 30, 40));

            Assert.Equal(1, single.Count);
            Assert.Equal(123, single[0]);

            Assert.Equal(10, quad[0]);
            Assert.Equal(20, quad[1]);
            Assert.Equal(30, quad[2]);
            Assert.Equal(40, quad[3]);
        }

        [Fact]
        public void Errors()
        {
            var single = OneOrMany.Create(123);
            var quad = OneOrMany.Create(ImmutableArray.Create<int>(10, 20, 30, 40));

            Assert.Throws<IndexOutOfRangeException>(() => single[1]);
            Assert.Throws<IndexOutOfRangeException>(() => single[-1]);
            Assert.Throws<IndexOutOfRangeException>(() => quad[5]);
            Assert.Throws<IndexOutOfRangeException>(() => quad[-1]);
            Assert.Throws<ArgumentNullException>(() => OneOrMany.Create(default(ImmutableArray<int>)));
        }
    }
}
