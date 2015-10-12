// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.EditorUtilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
{
    public class ITextSnapshotLineExtensionsTests
    {
        [WpfFact]
        public void GetFirstNonWhitespacePosition_EmptyLineReturnsNull()
        {
            var position = GetFirstNonWhitespacePosition(string.Empty);
            Assert.Null(position);
        }

        [WpfFact]
        public void GetFirstNonWhitespacePosition_WhitespaceLineReturnsNull1()
        {
            var position = GetFirstNonWhitespacePosition("    ");
            Assert.Null(position);
        }

        [WpfFact]
        public void GetFirstNonWhitespacePosition_WhitespaceLineReturnsNull2()
        {
            var position = GetFirstNonWhitespacePosition(" \t ");
            Assert.Null(position);
        }

        [WpfFact]
        public void GetFirstNonWhitespacePosition_WhitespaceLineReturnsNull3()
        {
            var position = GetFirstNonWhitespacePosition("\t\t");
            Assert.Null(position);
        }

        [WpfFact]
        public void GetFirstNonWhitespacePosition_TextLine()
        {
            var position = GetFirstNonWhitespacePosition("Foo");
            Assert.Equal(0, position.Value);
        }

        [WpfFact]
        public void GetFirstNonWhitespacePosition_TextLineStartingWithWhitespace1()
        {
            var position = GetFirstNonWhitespacePosition("    Foo");
            Assert.Equal(4, position.Value);
        }

        [WpfFact]
        public void GetFirstNonWhitespacePosition_TextLineStartingWithWhitespace2()
        {
            var position = GetFirstNonWhitespacePosition(" \t Foo");
            Assert.Equal(3, position.Value);
        }

        [WpfFact]
        public void GetFirstNonWhitespacePosition_TextLineStartingWithWhitespace3()
        {
            var position = GetFirstNonWhitespacePosition("\t\tFoo");
            Assert.Equal(2, position.Value);
        }

        [WpfFact]
        public void GetLastNonWhitespacePosition_EmptyLineReturnsNull()
        {
            var position = GetLastNonWhitespacePosition(string.Empty);
            Assert.Null(position);
        }

        [WpfFact]
        public void GetLastNonWhitespacePosition_WhitespaceLineReturnsNull1()
        {
            var position = GetLastNonWhitespacePosition("    ");
            Assert.Null(position);
        }

        [WpfFact]
        public void GetLastNonWhitespacePosition_WhitespaceLineReturnsNull2()
        {
            var position = GetLastNonWhitespacePosition(" \t ");
            Assert.Null(position);
        }

        [WpfFact]
        public void GetLastNonWhitespacePosition_WhitespaceLineReturnsNull3()
        {
            var position = GetLastNonWhitespacePosition("\t\t");
            Assert.Null(position);
        }

        [WpfFact]
        public void GetLastNonWhitespacePosition_TextLine()
        {
            var position = GetLastNonWhitespacePosition("Foo");
            Assert.Equal(2, position.Value);
        }

        [WpfFact]
        public void GetLastNonWhitespacePosition_TextLineEndingWithWhitespace1()
        {
            var position = GetLastNonWhitespacePosition("Foo    ");
            Assert.Equal(2, position.Value);
        }

        [WpfFact]
        public void GetLastNonWhitespacePosition_TextLineEndingWithWhitespace2()
        {
            var position = GetLastNonWhitespacePosition("Foo \t ");
            Assert.Equal(2, position.Value);
        }

        [WpfFact]
        public void GetLastNonWhitespacePosition_TextLineEndingWithWhitespace3()
        {
            var position = GetLastNonWhitespacePosition("Foo\t\t");
            Assert.Equal(2, position.Value);
        }

        [WpfFact]
        public void IsEmptyOrWhitespace_EmptyLineReturnsTrue()
        {
            var value = IsEmptyOrWhitespace(string.Empty);
            Assert.True(value);
        }

        [WpfFact]
        public void IsEmptyOrWhitespace_WhitespaceLineReturnsTrue1()
        {
            var value = IsEmptyOrWhitespace("    ");
            Assert.True(value);
        }

        [WpfFact]
        public void IsEmptyOrWhitespace_WhitespaceLineReturnsTrue2()
        {
            var value = IsEmptyOrWhitespace("\t\t");
            Assert.True(value);
        }

        [WpfFact]
        public void IsEmptyOrWhitespace_WhitespaceLineReturnsTrue3()
        {
            var value = IsEmptyOrWhitespace(" \t ");
            Assert.True(value);
        }

        [WpfFact]
        public void IsEmptyOrWhitespace_TextLineReturnsFalse()
        {
            var value = IsEmptyOrWhitespace("Foo");
            Assert.False(value);
        }

        [WpfFact]
        public void IsEmptyOrWhitespace_TextLineStartingWithWhitespaceReturnsFalse1()
        {
            var value = IsEmptyOrWhitespace("    Foo");
            Assert.False(value);
        }

        [WpfFact]
        public void IsEmptyOrWhitespace_TextLineStartingWithWhitespaceReturnsFalse2()
        {
            var value = IsEmptyOrWhitespace(" \t Foo");
            Assert.False(value);
        }

        [WpfFact]
        public void IsEmptyOrWhitespace_TextLineStartingWithWhitespaceReturnsFalse3()
        {
            var value = IsEmptyOrWhitespace("\t\tFoo");
            Assert.False(value);
        }

        private ITextSnapshotLine GetLine(string codeLine)
        {
            var snapshot = EditorFactory.CreateBuffer(TestExportProvider.ExportProviderWithCSharpAndVisualBasic, codeLine).CurrentSnapshot;
            return snapshot.GetLineFromLineNumber(0);
        }

        private bool IsEmptyOrWhitespace(string codeLine)
        {
            var line = GetLine(codeLine);
            return line.IsEmptyOrWhitespace();
        }

        private int? GetFirstNonWhitespacePosition(string codeLine)
        {
            var line = GetLine(codeLine);
            return line.GetFirstNonWhitespacePosition();
        }

        private int? GetLastNonWhitespacePosition(string codeLine)
        {
            var line = GetLine(codeLine);
            return line.GetLastNonWhitespacePosition();
        }
    }
}
