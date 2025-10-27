// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Text
{
    public class SubTextTests
    {
        [Fact]
        public void SubTextCarriageReturnLineFeedSplit()
        {
            var fullStringText = SourceText.From("\r\n");
            var subText = new SubText(fullStringText, new TextSpan(start: 1, length: 1));

            Assert.Equal(1, subText.Lines.IndexOf(1));
        }
    }
}
