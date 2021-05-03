// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    public class ActiveStatementsMapTests
    {
        [Fact]
        public void GetOverlappingSpans()
        {
            var span = new LinePositionSpan(new(3, 0), new(5, 2));

            var array = ImmutableArray.Create(
                new LinePositionSpan(new(4, 4), new(4, 18)),
                new LinePositionSpan(new(19, 0), new(19, 42)));

            Assert.Equal(new Range(0, 1), ActiveStatementsMap.GetOverlappingSpans(span, array, overlapsWith: (x, y) => x.OverlapsWith(y)));
        }
    }
}
