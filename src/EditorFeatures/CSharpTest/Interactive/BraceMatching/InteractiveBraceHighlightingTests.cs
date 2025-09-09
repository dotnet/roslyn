// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BraceMatching;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.BraceHighlighting;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.BraceHighlighting)]
public sealed class InteractiveBraceHighlightingTests
{
    private static IEnumerable<T> Enumerable<T>(params T[] array)
        => array;

    private static async Task<IEnumerable<TagSpan<BraceHighlightTag>>> ProduceTagsAsync(
        EditorTestWorkspace workspace,
        ITextBuffer buffer,
        int position)
    {
        var producer = new BraceHighlightingViewTaggerProvider(
            workspace.GetService<TaggerHost>(),
            workspace.GetService<IBraceMatchingService>());

        var context = new TaggerContext<BraceHighlightTag>(
            buffer.CurrentSnapshot.GetRelatedDocumentsWithChanges().FirstOrDefault(),
            buffer.CurrentSnapshot,
            frozenPartialSemantics: false,
            new SnapshotPoint(buffer.CurrentSnapshot, position));
        await producer.GetTestAccessor().ProduceTagsAsync(context);

        return context.TagSpans;
    }

    [WpfFact]
    public async Task TestCurlies()
    {
        var code = "public class C {\r\n}";
        using var workspace = EditorTestWorkspace.CreateCSharp(code, parseOptions: TestOptions.Script);
        var buffer = workspace.Documents.First().GetTextBuffer();

        // Before open curly
        var result = await ProduceTagsAsync(workspace, buffer, 14);
        Assert.True(result.IsEmpty());

        // At open curly
        result = await ProduceTagsAsync(workspace, buffer, 15);
        Assert.True(result.Select(ts => ts.Span.Span).SetEquals(Enumerable(Span.FromBounds(15, 16), Span.FromBounds(18, 19))));

        // After open curly
        result = await ProduceTagsAsync(workspace, buffer, 16);
        Assert.True(result.IsEmpty());

        // At close curly
        result = await ProduceTagsAsync(workspace, buffer, 18);
        Assert.True(result.IsEmpty());

        // After close curly
        result = await ProduceTagsAsync(workspace, buffer, 19);
        Assert.True(result.Select(ts => ts.Span.Span).SetEquals(Enumerable(Span.FromBounds(15, 16), Span.FromBounds(18, 19))));
    }

    [WpfFact]
    public async Task TestTouchingItems()
    {
        var code = "public class C {\r\n  public void Goo(){}\r\n}";
        using var workspace = EditorTestWorkspace.CreateCSharp(code, TestOptions.Script);
        var buffer = workspace.Documents.First().GetTextBuffer();

        // Before open curly
        var result = await ProduceTagsAsync(workspace, buffer, 35);
        Assert.True(result.Select(ts => ts.Span.Span).SetEquals(Enumerable(Span.FromBounds(35, 36), Span.FromBounds(36, 37))));

        // At open curly
        result = await ProduceTagsAsync(workspace, buffer, 36);
        Assert.True(result.IsEmpty());

        // After open curly
        result = await ProduceTagsAsync(workspace, buffer, 37);
        Assert.True(result.Select(ts => ts.Span.Span).SetEquals(
            Enumerable(Span.FromBounds(35, 36), Span.FromBounds(36, 37), Span.FromBounds(37, 38), Span.FromBounds(38, 39))));

        // At close curly
        result = await ProduceTagsAsync(workspace, buffer, 38);
        Assert.True(result.IsEmpty());

        // After close curly
        result = await ProduceTagsAsync(workspace, buffer, 39);
        Enumerable(Span.FromBounds(37, 38), Span.FromBounds(38, 39));
    }

    [WpfFact]
    public async Task TestAngles()
    {
        var code = "/// <summary>Goo</summary>\r\npublic class C<T> {\r\n  void Goo() {\r\n    bool a = b < c;\r\n    bool d = e > f;\r\n  }\r\n} ";
        using var workspace = EditorTestWorkspace.CreateCSharp(code, parseOptions: TestOptions.Script);
        var buffer = workspace.Documents.First().GetTextBuffer();

        // Before open angle of generic
        var result = await ProduceTagsAsync(workspace, buffer, 42);
        Assert.True(result.Select(ts => ts.Span.Span).SetEquals(Enumerable(Span.FromBounds(42, 43), Span.FromBounds(44, 45))));

        // After close angle of generic
        result = await ProduceTagsAsync(workspace, buffer, 45);
        Assert.True(result.Select(ts => ts.Span.Span).SetEquals(Enumerable(Span.FromBounds(42, 43), Span.FromBounds(44, 45))));

        async Task assertNoTags(int position, char expectedChar)
        {
            Assert.Equal(expectedChar, buffer.CurrentSnapshot[position]);
            result = await ProduceTagsAsync(workspace, buffer, position);
            Assert.True(result.IsEmpty());
            result = await ProduceTagsAsync(workspace, buffer, position + 1);
            Assert.True(result.IsEmpty());
        }

        // Doesn't highlight angles of XML doc comments
        var xmlTagStartPosition = 4;
        await assertNoTags(xmlTagStartPosition, '<');

        var xmlTagEndPosition = 12;
        await assertNoTags(xmlTagEndPosition, '>');

        var xmlEndTagStartPosition = 16;
        await assertNoTags(xmlEndTagStartPosition, '<');

        var xmlEndTagEndPosition = 25;
        await assertNoTags(xmlEndTagEndPosition, '>');

        // Doesn't highlight operators
        var openAnglePosition = 15 + buffer.CurrentSnapshot.GetLineFromLineNumber(3).Start;
        await assertNoTags(openAnglePosition, '<');

        var closeAnglePosition = 15 + buffer.CurrentSnapshot.GetLineFromLineNumber(4).Start;
        await assertNoTags(closeAnglePosition, '>');
    }

    [WpfFact]
    public async Task TestSwitch()
    {
        var code = """

            class C
            {
                void M(int variable)
                {
                    switch (variable)
                    {
                        case 0:
                            break;
                    }
                }
            } 
            """;
        using var workspace = EditorTestWorkspace.CreateCSharp(code, parseOptions: TestOptions.Script);
        var buffer = workspace.Documents.First().GetTextBuffer();

        // At switch open paren
        var result = await ProduceTagsAsync(workspace, buffer, 62);
        AssertEx.Equal(Enumerable(new Span(62, 1), new Span(71, 1)), result.Select(ts => ts.Span.Span).OrderBy(s => s.Start));

        // After switch open paren
        result = await ProduceTagsAsync(workspace, buffer, 83);
        Assert.True(result.IsEmpty());

        // At switch close paren
        result = await ProduceTagsAsync(workspace, buffer, 71);
        Assert.True(result.IsEmpty());

        // After switch close paren
        result = await ProduceTagsAsync(workspace, buffer, 72);
        AssertEx.Equal(Enumerable(new Span(62, 1), new Span(71, 1)), result.Select(ts => ts.Span.Span).OrderBy(s => s.Start));

        // At switch open curly
        result = await ProduceTagsAsync(workspace, buffer, 82);
        AssertEx.Equal(Enumerable(new Span(82, 1), new Span(138, 1)), result.Select(ts => ts.Span.Span).OrderBy(s => s.Start));

        // After switch open curly
        result = await ProduceTagsAsync(workspace, buffer, 83);
        Assert.True(result.IsEmpty());

        // At switch close curly
        result = await ProduceTagsAsync(workspace, buffer, 138);
        Assert.True(result.IsEmpty());

        // After switch close curly
        result = await ProduceTagsAsync(workspace, buffer, 139);
        AssertEx.Equal(Enumerable(new Span(82, 1), new Span(138, 1)), result.Select(ts => ts.Span.Span).OrderBy(s => s.Start));
    }
}
