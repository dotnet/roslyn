// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Microsoft.CodeAnalysis.Testing
{
    public class VerifierExtensionsTests
    {
        [Fact]
        [WorkItem(876, "https://github.com/dotnet/roslyn-sdk/issues/876")]
        public void VerifyContentWithMixedLineEndings1()
        {
            var baseline =
                "Line 1\r\n"
                + "Line 2\r\n"
                + "Line 3\r\n"
                + "Line 4\r\n"
                + "Line 5\r\n"
                + "Line 6\r\n";
            var modified =
                "Line 1\r"
                + "Line 2\r\n"
                + "Line 3\n"
                + "Line 4\r"
                + "Line 5\r\n"
                + "Line 6\n";

            var exception = Assert.Throws<InvalidOperationException>(() => new DefaultVerifier().EqualOrDiff(baseline, modified));
            Assert.Equal(
                $"Actual and expected values differ. Expected shown in baseline of diff:{Environment.NewLine}"
                + $"-Line 1<CR><LF>{Environment.NewLine}"
                + $"+Line 1<CR>{Environment.NewLine}"
                + $" Line 2<CR><LF>{Environment.NewLine}"
                + $"-Line 3<CR><LF>{Environment.NewLine}"
                + $"-Line 4<CR><LF>{Environment.NewLine}"
                + $"+Line 3<LF>{Environment.NewLine}"
                + $"+Line 4<CR>{Environment.NewLine}"
                + $" Line 5<CR><LF>{Environment.NewLine}"
                + $"-Line 6<CR><LF>{Environment.NewLine}"
                + $"+Line 6<LF>{Environment.NewLine}",
                exception.Message);
        }

        [Fact]
        [WorkItem(876, "https://github.com/dotnet/roslyn-sdk/issues/876")]
        public void VerifyContentWithMixedLineEnding2()
        {
            var baseline =
                "Line 1\n"
                + "Line 2\n"
                + "Line 3\n"
                + "Line 4\n"
                + "Line 5\n"
                + "Line 6\n";
            var modified =
                "Line 1\r"
                + "Line 2\r\n"
                + "Line 3\n"
                + "Line 4\r"
                + "Line 5\r\n"
                + "Line 6\n";

            var exception = Assert.Throws<InvalidOperationException>(() => new DefaultVerifier().EqualOrDiff(baseline, modified));
            Assert.Equal(
                $"Actual and expected values differ. Expected shown in baseline of diff:{Environment.NewLine}"
                + $"-Line 1<LF>{Environment.NewLine}"
                + $"-Line 2<LF>{Environment.NewLine}"
                + $"+Line 1<CR>{Environment.NewLine}"
                + $"+Line 2<CR><LF>{Environment.NewLine}"
                + $" Line 3<LF>{Environment.NewLine}"
                + $"-Line 4<LF>{Environment.NewLine}"
                + $"-Line 5<LF>{Environment.NewLine}"
                + $"+Line 4<CR>{Environment.NewLine}"
                + $"+Line 5<CR><LF>{Environment.NewLine}"
                + $" Line 6<LF>{Environment.NewLine}",
                exception.Message);
        }
    }
}
