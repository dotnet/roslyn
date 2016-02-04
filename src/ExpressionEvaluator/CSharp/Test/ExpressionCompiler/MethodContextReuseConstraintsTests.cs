// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
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
                new ILSpan(startOffset, endOffsetExclusive));

            Assert.True(constraints.AreSatisfied(moduleVersionId, methodToken, methodVersion, (int)startOffset));
            Assert.True(constraints.AreSatisfied(moduleVersionId, methodToken, methodVersion, (int)endOffsetExclusive - 1));

            Assert.False(constraints.AreSatisfied(Guid.NewGuid(), methodToken, methodVersion, (int)startOffset));
            Assert.False(constraints.AreSatisfied(moduleVersionId, methodToken + 1, methodVersion, (int)startOffset));
            Assert.False(constraints.AreSatisfied(moduleVersionId, methodToken, methodVersion + 1, (int)startOffset));
            Assert.False(constraints.AreSatisfied(moduleVersionId, methodToken, methodVersion, (int)startOffset - 1));
            Assert.False(constraints.AreSatisfied(moduleVersionId, methodToken, methodVersion, (int)endOffsetExclusive));
        }

        [Fact]
        public void EndExclusive()
        {
            var spans = new[] 
            {
                new ILSpan(0u, uint.MaxValue),
                new ILSpan(1, 9),
                new ILSpan(2, 8),
                new ILSpan(1, 3),
                new ILSpan(7, 9),
            };

            Assert.Equal(new ILSpan(0u, uint.MaxValue), MethodContextReuseConstraints.CalculateReuseSpan(5, ILSpan.MaxValue, spans.Take(1)));
            Assert.Equal(new ILSpan(1, 9), MethodContextReuseConstraints.CalculateReuseSpan(5, ILSpan.MaxValue, spans.Take(2)));
            Assert.Equal(new ILSpan(2, 8), MethodContextReuseConstraints.CalculateReuseSpan(5, ILSpan.MaxValue, spans.Take(3)));
            Assert.Equal(new ILSpan(3, 8), MethodContextReuseConstraints.CalculateReuseSpan(5, ILSpan.MaxValue, spans.Take(4)));
            Assert.Equal(new ILSpan(3, 7), MethodContextReuseConstraints.CalculateReuseSpan(5, ILSpan.MaxValue, spans.Take(5)));
        }

        [Fact]
        public void Cumulative()
        {
            var span = ILSpan.MaxValue;

            span = MethodContextReuseConstraints.CalculateReuseSpan(5, span, new ILSpan[0]);
            Assert.Equal(new ILSpan(0u, uint.MaxValue), span);

            span = MethodContextReuseConstraints.CalculateReuseSpan(5, span, new[] { new ILSpan(1, 10) });
            Assert.Equal(new ILSpan(1, 10), span);

            span = MethodContextReuseConstraints.CalculateReuseSpan(5, span, new[] { new ILSpan(2, 9) });
            Assert.Equal(new ILSpan(2, 9), span);

            span = MethodContextReuseConstraints.CalculateReuseSpan(5, span, new[] { new ILSpan(1, 3) });
            Assert.Equal(new ILSpan(3, 9), span);

            span = MethodContextReuseConstraints.CalculateReuseSpan(5, span, new[] { new ILSpan(7, 9) });
            Assert.Equal(new ILSpan(3, 7), span);
        }
    }
}