// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.IntegrationTests;

public class DocumentHighlightTests(ITestOutputHelper testOutputHelper) : AbstractRazorEditorTest(testOutputHelper)
{
    [IdeFact]
    public async Task Html()
    {
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.ErrorCshtmlFile, ControlledHangMitigatingCancellationToken);

        // The 5th <p> happens to be one that was problematic, but is otherwise not special. See https://github.com/dotnet/razor/issues/9212
        await TestServices.Editor.PlaceCaretAsync("<p", charsOffset: 1, occurrence: 5, extendSelection: false, selectBlock: false, ControlledHangMitigatingCancellationToken);

        var tags = await TestServices.Editor.WaitForTagsAsync<ITextMarkerTag>(ControlledHangMitigatingCancellationToken);

        Assert.Collection(tags,
            t => AssertEx.EqualOrDiff("<p>", t.Span.GetText()),
            t => AssertEx.EqualOrDiff("</p>", t.Span.GetText()));
    }

    [IdeFact]
    public async Task CSharp()
    {
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.CounterRazorFile, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("current", charsOffset: 1, occurrence: 5, extendSelection: false, selectBlock: false, ControlledHangMitigatingCancellationToken);

        var tags = await TestServices.Editor.WaitForTagsAsync<ITextMarkerTag>(ControlledHangMitigatingCancellationToken);

        Assert.Equal(3, tags.Length);
        Assert.All(tags, t => AssertEx.EqualOrDiff("currentCount", t.Span.GetText()));
    }
}
