// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public sealed class TextUtilitiesTests : TestBase
    {
        [Fact]
        public void IsAnyLineBreakCharacter1()
        {
            Assert.True(TextUtilities.IsAnyLineBreakCharacter('\n'));
            Assert.True(TextUtilities.IsAnyLineBreakCharacter('\r'));
            Assert.True(TextUtilities.IsAnyLineBreakCharacter('\u0085'));
            Assert.True(TextUtilities.IsAnyLineBreakCharacter('\u2028'));
            Assert.True(TextUtilities.IsAnyLineBreakCharacter('\u2029'));
        }

        [Fact]
        public void IsAnyLineBreakCharacter2()
        {
            Assert.False(TextUtilities.IsAnyLineBreakCharacter('a'));
            Assert.False(TextUtilities.IsAnyLineBreakCharacter('b'));
        }

        [Fact]
        public void GetLengthOfLineBreak1()
        {
            Assert.Equal(0, TextUtilities.GetLengthOfLineBreak(SourceText.From("aoeu"), 0));
            Assert.Equal(0, TextUtilities.GetLengthOfLineBreak(SourceText.From("aoeu"), 2));
        }

        /// <summary>
        /// Normal line break within the string
        /// </summary>
        [Fact]
        public void GetLengthOfLineBreak2()
        {
            Assert.Equal(1, TextUtilities.GetLengthOfLineBreak(SourceText.From("\naoeu"), 0));
            Assert.Equal(1, TextUtilities.GetLengthOfLineBreak(SourceText.From("a\nbaou"), 1));
            Assert.Equal(0, TextUtilities.GetLengthOfLineBreak(SourceText.From("a\n"), 0));
        }

        /// <summary>
        /// Ensure \n \r combinations are handled correctly
        /// </summary>
        [Fact]
        public void GetLengthOfLineBreak3()
        {
            Assert.Equal(2, TextUtilities.GetLengthOfLineBreak(SourceText.From("\r\n"), 0));
            Assert.Equal(1, TextUtilities.GetLengthOfLineBreak(SourceText.From("\n\r"), 0));
        }

        /// <summary>
        /// Don't go past the end of the buffer
        /// </summary>
        [Fact]
        public void GetLengthOfLineBreak4()
        {
            Assert.Equal(1, TextUtilities.GetLengthOfLineBreak(SourceText.From("\r"), 0));
        }
    }
}
