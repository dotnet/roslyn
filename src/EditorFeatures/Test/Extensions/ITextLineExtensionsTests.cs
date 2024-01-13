// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
{
    public class ITextLineExtensionsTests
    {
        [Fact]
        public void GetFirstNonWhitespacePosition_EmptyLineReturnsNull()
        {
            var position = GetFirstNonWhitespacePosition(string.Empty);
            Assert.Null(position);
        }

        [Fact]
        public void GetFirstNonWhitespacePosition_WhitespaceLineReturnsNull1()
        {
            var position = GetFirstNonWhitespacePosition("    ");
            Assert.Null(position);
        }

        [Fact]
        public void GetFirstNonWhitespacePosition_WhitespaceLineReturnsNull2()
        {
            var position = GetFirstNonWhitespacePosition("\t\t");
            Assert.Null(position);
        }

        [Fact]
        public void GetFirstNonWhitespacePosition_WhitespaceLineReturnsNull3()
        {
            var position = GetFirstNonWhitespacePosition(" \t ");
            Assert.Null(position);
        }

        [Fact]
        public void GetFirstNonWhitespacePosition_TextLine()
        {
            var position = GetFirstNonWhitespacePosition("Goo");
            Assert.Equal(0, position.Value);
        }

        [Fact]
        public void GetFirstNonWhitespacePosition_TextLineStartingWithWhitespace1()
        {
            var position = GetFirstNonWhitespacePosition("    Goo");
            Assert.Equal(4, position.Value);
        }

        [Fact]
        public void GetFirstNonWhitespacePosition_TextLineStartingWithWhitespace2()
        {
            var position = GetFirstNonWhitespacePosition(" \t Goo");
            Assert.Equal(3, position.Value);
        }

        [Fact]
        public void GetFirstNonWhitespacePosition_TextLineStartingWithWhitespace3()
        {
            var position = GetFirstNonWhitespacePosition("\t\tGoo");
            Assert.Equal(2, position.Value);
        }

        [Fact]
        public void IsEmptyOrWhitespace_EmptyLineReturnsTrue()
        {
            var value = IsEmptyOrWhitespace(string.Empty);
            Assert.True(value);
        }

        [Fact]
        public void IsEmptyOrWhitespace_WhitespaceLineReturnsTrue1()
        {
            var value = IsEmptyOrWhitespace("    ");
            Assert.True(value);
        }

        [Fact]
        public void IsEmptyOrWhitespace_WhitespaceLineReturnsTrue2()
        {
            var value = IsEmptyOrWhitespace("\t\t");
            Assert.True(value);
        }

        [Fact]
        public void IsEmptyOrWhitespace_WhitespaceLineReturnsTrue3()
        {
            var value = IsEmptyOrWhitespace(" \t ");
            Assert.True(value);
        }

        [Fact]
        public void IsEmptyOrWhitespace_TextLineReturnsFalse()
        {
            var value = IsEmptyOrWhitespace("Goo");
            Assert.False(value);
        }

        [Fact]
        public void IsEmptyOrWhitespace_TextLineStartingWithWhitespaceReturnsFalse1()
        {
            var value = IsEmptyOrWhitespace("    Goo");
            Assert.False(value);
        }

        [Fact]
        public void IsEmptyOrWhitespace_TextLineStartingWithWhitespaceReturnsFalse2()
        {
            var value = IsEmptyOrWhitespace(" \t Goo");
            Assert.False(value);
        }

        [Fact]
        public void IsEmptyOrWhitespace_TextLineStartingWithWhitespaceReturnsFalse3()
        {
            var value = IsEmptyOrWhitespace("\t\tGoo");
            Assert.False(value);
        }

        private static TextLine GetLine(string codeLine)
        {
            var text = SourceText.From(codeLine);
            return text.Lines[0];
        }

        private static bool IsEmptyOrWhitespace(string codeLine)
        {
            var line = GetLine(codeLine);
            return line.IsEmptyOrWhitespace();
        }

        private static int? GetFirstNonWhitespacePosition(string codeLine)
        {
            var line = GetLine(codeLine);
            return line.GetFirstNonWhitespacePosition();
        }
    }
}
