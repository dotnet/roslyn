// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.EditAndContinue
{
    public class LinePositionSpanExtensionsTests
    {
        [Fact]
        public void OverlapsWith()
        {
            var span1 = new LinePositionSpan(new(3, 0), new(5, 2));
            var span2 = new LinePositionSpan(new(4, 4), new(4, 18));

            Assert.True(span1.OverlapsWith(span2));
        }
    }
}
