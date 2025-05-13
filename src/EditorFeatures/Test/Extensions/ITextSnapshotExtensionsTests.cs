// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.EditorUtilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;

[UseExportProvider]
public sealed class ITextSnapshotExtensionsTests
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
        var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition("Goo", 0);
        Assert.Equal(string.Empty, leadingWhitespace);
    }

    [Fact]
    public void GetLeadingWhitespaceOfLineAtPosition_TextLineStartingWithWhitespace1()
    {
        var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition("    Goo", 0);
        Assert.Equal("    ", leadingWhitespace);
    }

    [Fact]
    public void GetLeadingWhitespaceOfLineAtPosition_TextLineStartingWithWhitespace2()
    {
        var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition(" \t Goo", 0);
        Assert.Equal(" \t ", leadingWhitespace);
    }

    [Fact]
    public void GetLeadingWhitespaceOfLineAtPosition_TextLineStartingWithWhitespace3()
    {
        var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition("\t\tGoo", 0);
        Assert.Equal("\t\t", leadingWhitespace);
    }

    [Fact]
    public void GetLeadingWhitespaceOfLineAtPosition_EmptySecondLineReturnsEmptyString()
    {
        var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition("Goo\r\n", 5);
        Assert.Equal(string.Empty, leadingWhitespace);
    }

    [Fact]
    public void GetLeadingWhitespaceOfLineAtPosition_WhitespaceSecondLineReturnsWhitespace1()
    {
        var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition("Goo\r\n    ", 5);
        Assert.Equal("    ", leadingWhitespace);
    }

    [Fact]
    public void GetLeadingWhitespaceOfLineAtPosition_WhitespaceSecondLineReturnsWhitespace2()
    {
        var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition("Goo\r\n \t ", 5);
        Assert.Equal(" \t ", leadingWhitespace);
    }

    [Fact]
    public void GetLeadingWhitespaceOfLineAtPosition_WhitespaceSecondLineReturnsWhitespace3()
    {
        var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition("Goo\r\n\t\t", 5);
        Assert.Equal("\t\t", leadingWhitespace);
    }

    [Fact]
    public void GetLeadingWhitespaceOfLineAtPosition_TextSecondLine()
    {
        var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition("Goo\r\nGoo", 5);
        Assert.Equal(string.Empty, leadingWhitespace);
    }

    [Fact]
    public void GetLeadingWhitespaceOfLineAtPosition_TextSecondLineStartingWithWhitespace1()
    {
        var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition("Goo\r\n    Goo", 5);
        Assert.Equal("    ", leadingWhitespace);
    }

    [Fact]
    public void GetLeadingWhitespaceOfLineAtPosition_TextSecondLineStartingWithWhitespace2()
    {
        var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition("Goo\r\n \t Goo", 5);
        Assert.Equal(" \t ", leadingWhitespace);
    }

    [Fact]
    public void GetLeadingWhitespaceOfLineAtPosition_TextSecondLineStartingWithWhitespace3()
    {
        var leadingWhitespace = GetLeadingWhitespaceOfLineAtPosition("Goo\r\n\t\tGoo", 5);
        Assert.Equal("\t\t", leadingWhitespace);
    }

    [Fact]
    public void GetSpanTest()
    {
        // each line of sample code contains 4 characters followed by a newline
        var snapshot = GetSampleCodeSnapshot();
        var span = snapshot.GetSpan(0, 2, 1, 1);

        // column 0, index 2 = (0 * 5) + 2 = 2
        Assert.Equal(2, span.Start);

        // column 1, index 1 = (1 * 5) + 1 = 6
        Assert.Equal(6, span.End);
    }

    [Fact]
    public void TryGetPositionTest()
    {
        // each line of sample code contains 4 characters followed by a newline
        var snapshot = GetSampleCodeSnapshot();

        // valid line, valid column
        Assert.True(snapshot.TryGetPosition(3, 2, out var point));
        Assert.Equal(17, point.Position);

        // valid line, invalid column
        Assert.False(snapshot.TryGetPosition(1, 8, out _));
        Assert.False(snapshot.TryGetPosition(3, -2, out _));

        // invalid line, valid column
        Assert.False(snapshot.TryGetPosition(18, 1, out _));
        Assert.False(snapshot.TryGetPosition(-1, 1, out _));
    }

    [Fact]
    public void TryGetPointValueTest()
    {
        var snapshot = GetSampleCodeSnapshot();
        Assert.Equal(new SnapshotPoint(snapshot, 15), snapshot.TryGetPoint(3, 0).Value);
    }

    [Fact]
    public void TryGetPointNullTest()
    {
        var snapshot = GetSampleCodeSnapshot();
        Assert.Null(snapshot.TryGetPoint(3000, 0));
    }

    [Fact]
    public void GetLineAndCharacterTest()
    {
        var snapshot = GetSampleCodeSnapshot();
        snapshot.GetLineAndCharacter(16, out var line, out var character);
        Assert.Equal(3, line);
        Assert.Equal(1, character);
    }

    private static string GetLeadingWhitespaceOfLineAtPosition(string code, int position)
    {
        var exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider();
        var snapshot = EditorFactory.CreateBuffer(exportProvider, code).CurrentSnapshot;
        return snapshot.GetLeadingWhitespaceOfLineAtPosition(position);
    }

    private static ITextSnapshot GetSampleCodeSnapshot()
    {
        var exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider();

        // to make verification simpler, each line of code is 4 characters and will be joined to other lines
        // with a single newline character making the formula to calculate the offset from a given line and
        // column thus:
        //   position = row * 5 + column
        var lines = new string[]
        {
            "goo1",
            "bar1",
            "goo2",
            "bar2",
            "goo3",
            "bar3",
        };
        var code = string.Join("\n", lines);
        var snapshot = EditorFactory.CreateBuffer(exportProvider, code).CurrentSnapshot;
        return snapshot;
    }
}
