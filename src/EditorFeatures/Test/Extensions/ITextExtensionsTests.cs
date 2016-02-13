// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
{
    public class ITextExtensionsTests
    {
        [Fact]
        public void GetLeadingWhitespaceOfLineAtPosition_EmptyLineReturnsEmptyString()
        {
            var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition(string.Empty, 0);
            Assert.Equal(string.Empty, leadingWhitespace);
        }

        [Fact]
        public void GetLeadingWhitespaceOfLineAtPosition_WhitespaceLineReturnsWhitespace1()
        {
            var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition("    ", 0);
            Assert.Equal("    ", leadingWhitespace);
        }

        [Fact]
        public void GetLeadingWhitespaceOfLineAtPosition_WhitespaceLineReturnsWhitespace2()
        {
            var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition("\t\t", 0);
            Assert.Equal("\t\t", leadingWhitespace);
        }

        [Fact]
        public void GetLeadingWhitespaceOfLineAtPosition_WhitespaceLineReturnsWhitespace3()
        {
            var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition(" \t ", 0);
            Assert.Equal(" \t ", leadingWhitespace);
        }

        [Fact]
        public void GetLeadingWhitespaceOfLineAtPosition_TextLine()
        {
            var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition("Foo", 0);
            Assert.Equal(string.Empty, leadingWhitespace);
        }

        [Fact]
        public void GetLeadingWhitespaceOfLineAtPosition_TextLineStartingWithWhitespace1()
        {
            var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition("    Foo", 0);
            Assert.Equal("    ", leadingWhitespace);
        }

        [Fact]
        public void GetLeadingWhitespaceOfLineAtPosition_TextLineStartingWithWhitespace2()
        {
            var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition("\t\tFoo", 0);
            Assert.Equal("\t\t", leadingWhitespace);
        }

        [Fact]
        public void GetLeadingWhitespaceOfLineAtPosition_TextLineStartingWithWhitespace3()
        {
            var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition(" \t Foo", 0);
            Assert.Equal(" \t ", leadingWhitespace);
        }

        [Fact]
        public void GetLeadingWhitespaceOfLineAtPosition_EmptySecondLineReturnsEmptyString()
        {
            var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition("Foo\r\n", 5);
            Assert.Equal(string.Empty, leadingWhitespace);
        }

        [Fact]
        public void GetLeadingWhitespaceOfLineAtPosition_WhitespaceSecondLineReturnsWhitespace1()
        {
            var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition("Foo\r\n    ", 5);
            Assert.Equal("    ", leadingWhitespace);
        }

        [Fact]
        public void GetLeadingWhitespaceOfLineAtPosition_WhitespaceSecondLineReturnsWhitespace2()
        {
            var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition("Foo\r\n\t\t", 5);
            Assert.Equal("\t\t", leadingWhitespace);
        }

        [Fact]
        public void GetLeadingWhitespaceOfLineAtPosition_WhitespaceSecondLineReturnsWhitespace3()
        {
            var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition("Foo\r\n \t ", 5);
            Assert.Equal(" \t ", leadingWhitespace);
        }

        [Fact]
        public void GetLeadingWhitespaceOfLineAtPosition_TextSecondLine()
        {
            var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition("Foo\r\nFoo", 5);
            Assert.Equal(string.Empty, leadingWhitespace);
        }

        [Fact]
        public void GetLeadingWhitespaceOfLineAtPosition_TextSecondLineStartingWithWhitespace1()
        {
            var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition("Foo\r\n    Foo", 5);
            Assert.Equal("    ", leadingWhitespace);
        }

        [Fact]
        public void GetLeadingWhitespaceOfLineAtPosition_TextSecondLineStartingWithWhitespace2()
        {
            var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition("Foo\r\n\t\tFoo", 5);
            Assert.Equal("\t\t", leadingWhitespace);
        }

        [Fact]
        public void GetLeadingWhitespaceOfLineAtPosition_TextSecondLineStartingWithWhitespace3()
        {
            var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition("Foo\r\n \t Foo", 5);
            Assert.Equal(" \t ", leadingWhitespace);
        }

        private string GetLeadingWhitespaceOfLineAtPosition(string code, int position)
        {
            var text = SourceText.From(code);
            return text.GetLeadingWhitespaceOfLineAtPosition(position);
        }
    }
}
