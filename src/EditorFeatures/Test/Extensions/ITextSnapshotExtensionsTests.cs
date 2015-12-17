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
    public class ITextSnapshotExtensionsTests
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
            var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition(" \t ", 0);
            Assert.Equal(" \t ", leadingWhitespace);
        }

        [Fact]
        public void GetLeadingWhitespaceOfLineAtPosition_WhitespaceLineReturnsWhitespace3()
        {
            var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition("\t\t", 0);
            Assert.Equal("\t\t", leadingWhitespace);
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
            var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition(" \t Foo", 0);
            Assert.Equal(" \t ", leadingWhitespace);
        }

        [Fact]
        public void GetLeadingWhitespaceOfLineAtPosition_TextLineStartingWithWhitespace3()
        {
            var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition("\t\tFoo", 0);
            Assert.Equal("\t\t", leadingWhitespace);
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
            var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition("Foo\r\n \t ", 5);
            Assert.Equal(" \t ", leadingWhitespace);
        }

        [Fact]
        public void GetLeadingWhitespaceOfLineAtPosition_WhitespaceSecondLineReturnsWhitespace3()
        {
            var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition("Foo\r\n\t\t", 5);
            Assert.Equal("\t\t", leadingWhitespace);
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
            var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition("Foo\r\n \t Foo", 5);
            Assert.Equal(" \t ", leadingWhitespace);
        }

        [Fact]
        public void GetLeadingWhitespaceOfLineAtPosition_TextSecondLineStartingWithWhitespace3()
        {
            var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition("Foo\r\n\t\tFoo", 5);
            Assert.Equal("\t\t", leadingWhitespace);
        }

        [Fact]
        public void GetSpanTest()
        {
            // each line of sample code contains 4 characters followed by a newline
            var snapshot = GetSampleCodeSnapshot();
            var span = snapshot.GetSpan(0, 2, 1, 1);

            // column 0, index 2 = (0 * 5) + 2 = 2
            Assert.Equal(span.Start, 2);

            // column 1, index 1 = (1 * 5) + 1 = 6
            Assert.Equal(span.End, 6);
        }

        [Fact]
        public void TryGetPositionTest()
        {
            // each line of sample code contains 4 characters followed by a newline
            var snapshot = GetSampleCodeSnapshot();
            var point = new SnapshotPoint();

            // valid line, valid column
            Assert.True(snapshot.TryGetPosition(3, 2, out point));
            Assert.Equal(17, point.Position);

            // valid line, invalid column
            Assert.False(snapshot.TryGetPosition(1, 8, out point));
            Assert.False(snapshot.TryGetPosition(3, -2, out point));

            // invalid line, valid column
            Assert.False(snapshot.TryGetPosition(18, 1, out point));
            Assert.False(snapshot.TryGetPosition(-1, 1, out point));
        }

        [Fact]
        public void GetPointTest()
        {
            var snapshot = GetSampleCodeSnapshot();
            Assert.Equal(new SnapshotPoint(snapshot, 15), snapshot.GetPoint(3, 0));
        }

        [Fact]
        public void GetLineAndColumnTest()
        {
            var snapshot = GetSampleCodeSnapshot();
            int line;
            int col;
            snapshot.GetLineAndColumn(16, out line, out col);
            Assert.Equal(3, line);
            Assert.Equal(1, col);
        }

        private string GetLeadingWhitespaceOfLineAtPosition(string code, int position)
        {
            var snapshot = EditorFactory.CreateBuffer(TestExportProvider.ExportProviderWithCSharpAndVisualBasic, code).CurrentSnapshot;
            return snapshot.GetLeadingWhitespaceOfLineAtPosition(position);
        }

        private ITextSnapshot GetSampleCodeSnapshot()
        {
            // to make verification simpler, each line of code is 4 characters and will be joined to other lines
            // with a single newline character making the formula to calculate the offset from a given line and
            // column thus:
            //   position = row * 5 + column
            var lines = new string[]
            {
                "foo1",
                "bar1",
                "foo2",
                "bar2",
                "foo3",
                "bar3",
            };
            var code = string.Join("\n", lines);
            var snapshot = EditorFactory.CreateBuffer(TestExportProvider.ExportProviderWithCSharpAndVisualBasic, code).CurrentSnapshot;
            return snapshot;
        }
    }
}
