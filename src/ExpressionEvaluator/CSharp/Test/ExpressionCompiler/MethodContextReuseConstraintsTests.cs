// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class MethodContextReuseConstraintsTests : ExpressionCompilerTestBase
    {
        [Fact]
        public void AreSatisfied()
        {
            var moduleVersionId = Guid.NewGuid();
            const int methodToken = 0x06000001;
            const int methodVersion = 1;
            const uint startOffset = 1;
            const uint endOffsetExclusive = 3;

            var constraints = new MethodContextReuseConstraints(
                moduleVersionId,
                methodToken,
                methodVersion,
                startOffset,
                endOffsetExclusive);

            Assert.True(constraints.AreSatisfied(moduleVersionId, methodToken, methodVersion, (int)startOffset));
            Assert.True(constraints.AreSatisfied(moduleVersionId, methodToken, methodVersion, (int)endOffsetExclusive - 1));

            Assert.False(constraints.AreSatisfied(Guid.NewGuid(), methodToken, methodVersion, (int)startOffset));
            Assert.False(constraints.AreSatisfied(moduleVersionId, methodToken + 1, methodVersion, (int)startOffset));
            Assert.False(constraints.AreSatisfied(moduleVersionId, methodToken, methodVersion + 1, (int)startOffset));
            Assert.False(constraints.AreSatisfied(moduleVersionId, methodToken, methodVersion, (int)startOffset - 1));
            Assert.False(constraints.AreSatisfied(moduleVersionId, methodToken, methodVersion, (int)endOffsetExclusive));
        }

        [Fact]
        public void EndInclusive()
        {
            var moduleVersionId = Guid.NewGuid();
            const int methodToken = 0x06000001;
            const int methodVersion = 1;

            var builder = new MethodContextReuseConstraints.Builder(moduleVersionId, methodToken, methodVersion, ilOffset: 5, areRangesEndInclusive: true);
            Assert.True(builder.Build().HasExpectedSpan(0u, uint.MaxValue));

            builder.AddRange(1, 9);
            Assert.True(builder.Build().HasExpectedSpan(1, 10));

            builder.AddRange(2, 8);
            Assert.True(builder.Build().HasExpectedSpan(2, 9));

            builder.AddRange(1, 3);
            Assert.True(builder.Build().HasExpectedSpan(4, 9));

            builder.AddRange(7, 9);
            Assert.True(builder.Build().HasExpectedSpan(4, 7));
        }

        [Fact]
        public void EndExclusive()
        {
            var moduleVersionId = Guid.NewGuid();
            const int methodToken = 0x06000001;
            const int methodVersion = 1;

            var builder = new MethodContextReuseConstraints.Builder(moduleVersionId, methodToken, methodVersion, ilOffset: 5, areRangesEndInclusive: false);
            Assert.True(builder.Build().HasExpectedSpan(0u, uint.MaxValue));

            builder.AddRange(1, 9);
            Assert.True(builder.Build().HasExpectedSpan(1, 9));

            builder.AddRange(2, 8);
            Assert.True(builder.Build().HasExpectedSpan(2, 8));

            builder.AddRange(1, 3);
            Assert.True(builder.Build().HasExpectedSpan(3, 8));

            builder.AddRange(7, 9);
            Assert.True(builder.Build().HasExpectedSpan(3, 7));
        }

        [Fact]
        public void Cumulative()
        {
            var moduleVersionId = Guid.NewGuid();
            const int methodToken = 0x06000001;
            const int methodVersion = 1;

            var builder = new MethodContextReuseConstraints.Builder(moduleVersionId, methodToken, methodVersion, ilOffset: 5, areRangesEndInclusive: false);
            Assert.True(builder.Build().HasExpectedSpan(0u, uint.MaxValue));

            builder.AddRange(1, 10);
            Assert.True(builder.Build().HasExpectedSpan(1, 10));
            builder = new MethodContextReuseConstraints.Builder(builder.Build(), ilOffset: 5, areRangesEndInclusive: true);

            builder.AddRange(2, 8);
            Assert.True(builder.Build().HasExpectedSpan(2, 9));
            builder = new MethodContextReuseConstraints.Builder(builder.Build(), ilOffset: 5, areRangesEndInclusive: false);

            builder.AddRange(1, 3);
            Assert.True(builder.Build().HasExpectedSpan(3, 9));
            builder = new MethodContextReuseConstraints.Builder(builder.Build(), ilOffset: 5, areRangesEndInclusive: true);

            builder.AddRange(7, 9);
            Assert.True(builder.Build().HasExpectedSpan(3, 7));
        }
    }
}