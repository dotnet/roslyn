// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.EditorUtilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.EditorAdapter;

[UseExportProvider]
public sealed class TextSnapshotImplementationTest
{
    private static Tuple<ITextSnapshot, SourceText> Create(params string[] lines)
    {
        var exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider();
        var buffer = EditorFactory.CreateBuffer(exportProvider, lines);
        var text = buffer.CurrentSnapshot.AsText();
        return Tuple.Create(buffer.CurrentSnapshot, text);
    }

    [Fact]
    public void Basic1()
    {
        var tuple = Create("goo", "bar");
        var text = tuple.Item2;
        Assert.Equal(tuple.Item1.LineCount, text.Lines.Count);
        Assert.Equal(tuple.Item1.Length, text.Length);
        Assert.Equal(tuple.Item1.GetText(), text.ToString());
    }

    [Fact]
    public void GetLineFromLineNumber1()
    {
        var tuple = Create("goo", "bar");
        var text = tuple.Item2;
        var line1 = text.Lines[0];
        Assert.Equal(new TextSpan(0, 3), line1.Span);
        Assert.Equal(new TextSpan(0, 5), line1.SpanIncludingLineBreak);
        Assert.Equal("goo", line1.ToString());
    }

    [Fact]
    public void GetLineFromLineNumber2()
    {
        var tuple = Create("goo", "bar");
        var text = tuple.Item2;
        var line1 = text.Lines[1];
        Assert.Equal(new TextSpan(5, 3), line1.Span);
        Assert.Equal(new TextSpan(5, 3), line1.SpanIncludingLineBreak);
        Assert.Equal("bar", line1.ToString());
    }

    [Fact]
    public void Lines1()
    {
        var tuple = Create("goo", "bar");
        var lines = tuple.Item2.Lines;
        Assert.Equal(2, lines.Count);
    }
}
