// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic;

[Trait(Traits.Feature, Traits.Features.DocumentationComments)]
public sealed class DocumentationCommentTests() : AbstractEditorTest(nameof(DocumentationCommentTests))
{
    protected override string LanguageName => LanguageNames.VisualBasic;

    [IdeTheory, WorkItem("https://github.com/dotnet/roslyn/issues/17383")]
    [InlineData("\r\n", "\r\n", "\r\n")]
    [InlineData("\n", "\r\n", "\n")]
    [InlineData("\r\n", "\n", "\n")]
    public async Task Paste_MultilineText(string documentNewLine, string editorNewLine, string pastedNewLine)
    {
        await SetUpEditorAsync(
            JoinLines(documentNewLine,
                "",
                "Public Class C",
                "    ''' <summary>",
                "    ''' $$",
                "    ''' </summary>",
                "End Class",
                ""),
            HangMitigatingCancellationToken);
        await TestServices.Editor.SetNewLineCharacterAsync(editorNewLine, HangMitigatingCancellationToken);

        await TestServices.Editor.PasteAsync(
            "Line 1" + pastedNewLine + "Line 2 with A & B", HangMitigatingCancellationToken);

        AssertEx.EqualOrDiff(
            "" + documentNewLine +
            "Public Class C" + documentNewLine +
            "    ''' <summary>" + documentNewLine +
            "    ''' Line 1" + documentNewLine +
            "    ''' Line 2 with A &amp; B" + documentNewLine +
            "    ''' </summary>" + documentNewLine +
            "End Class" + documentNewLine,
            await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
    }

    [IdeFact, WorkItem("https://github.com/dotnet/roslyn/issues/17383")]
    public async Task Paste_RemovesEditorIndentationFromContinuationLines()
    {
        await SetUpEditorAsync(
            JoinLines("\r\n",
                "",
                "Public Class C",
                "    ''' <summary>",
                "    ''' $$",
                "    ''' </summary>",
                "End Class",
                ""),
            HangMitigatingCancellationToken);

        await TestServices.Editor.PasteAsync("Line 1\r\n    Line 2", HangMitigatingCancellationToken);

        AssertEx.EqualOrDiff(
            JoinLines("\r\n",
                "",
                "Public Class C",
                "    ''' <summary>",
                "    ''' Line 1",
                "    ''' Line 2",
                "    ''' </summary>",
                "End Class",
                ""),
            await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
    }

    [IdeTheory, WorkItem("https://github.com/dotnet/roslyn/issues/17383")]
    [InlineData("'", "", "A & B")]
    [InlineData("'''", "''' ", "A &amp; B")]
    public async Task Paste_ReplacesMultipleSelections(string commentPrefix, string continuationPrefix, string escapedFirstLine)
    {
        await SetUpEditorAsync(
            JoinLines("\r\n",
                "",
                $"{commentPrefix} <summary>Replace {{|selection:first|}}</summary>",
                $"{commentPrefix} <remarks>Replace {{|selection:second|}}</remarks>",
                "Public Class C",
                "End Class",
                ""),
            HangMitigatingCancellationToken);

        // With three clipboard lines and two selections, the editor inserts the full text at each selection.
        // Compare ordinary comments with documentation comments to isolate the XML formatting adjustment.
        await TestServices.Editor.PasteAsync("A & B\r\nLine 2\r\nLine 3", HangMitigatingCancellationToken);

        AssertEx.EqualOrDiff(
            JoinLines("\r\n",
                "",
                $"{commentPrefix} <summary>Replace {escapedFirstLine}",
                $"{continuationPrefix}Line 2",
                $"{continuationPrefix}Line 3</summary>",
                $"{commentPrefix} <remarks>Replace {escapedFirstLine}",
                $"{continuationPrefix}Line 2",
                $"{continuationPrefix}Line 3</remarks>",
                "Public Class C",
                "End Class",
                ""),
            await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
    }

    [IdeTheory, WorkItem("https://github.com/dotnet/roslyn/issues/17383")]
    [InlineData("'", "A & B")]
    [InlineData("'''", "A &amp; B")]
    public async Task Paste_DistributesLinesAcrossSelections(string commentPrefix, string escapedFirstLine)
    {
        await SetUpEditorAsync(
            JoinLines("\r\n",
                "",
                $"{commentPrefix} <summary>Replace {{|selection:first|}}</summary>",
                $"{commentPrefix} <remarks>Replace {{|selection:second|}}</remarks>",
                "Public Class C",
                "End Class",
                ""),
            HangMitigatingCancellationToken);

        // With two clipboard lines and two selections, the editor distributes one line to each selection.
        // Documentation formatting must preserve that distribution and only escape the inserted text.
        await TestServices.Editor.PasteAsync("A & B\r\nLine 2", HangMitigatingCancellationToken);

        AssertEx.EqualOrDiff(
            JoinLines("\r\n",
                "",
                $"{commentPrefix} <summary>Replace {escapedFirstLine}</summary>",
                $"{commentPrefix} <remarks>Replace Line 2</remarks>",
                "Public Class C",
                "End Class",
                ""),
            await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
    }

    [IdeFact, WorkItem("https://github.com/dotnet/roslyn/issues/17383")]
    public async Task Paste_MixedEligibleAndIneligibleSelections()
    {
        await SetUpEditorAsync(
            JoinLines("\r\n",
                "",
                "''' <summary>Replace {|selection:documentation|}</summary>",
                "' Replace {|selection:ordinary|}",
                "Public Class C",
                "End Class",
                ""),
            HangMitigatingCancellationToken);

        await TestServices.Editor.PasteAsync("A & B\r\nLine 2\r\nLine 3", HangMitigatingCancellationToken);

        AssertEx.EqualOrDiff(
            JoinLines("\r\n",
                "",
                "''' <summary>Replace A &amp; B",
                "''' Line 2",
                "''' Line 3</summary>",
                "' Replace A & B",
                "Line 2",
                "Line 3",
                "Public Class C",
                "End Class",
                ""),
            await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
    }

    [IdeFact, WorkItem("https://github.com/dotnet/roslyn/issues/17383")]
    public async Task Paste_UndoSmartAdjustmentThenNormalPaste()
    {
        await SetUpEditorAsync(
            JoinLines("\r\n",
                "",
                "''' <summary>",
                "''' $$",
                "''' </summary>",
                "Public Class C",
                "End Class",
                ""),
            HangMitigatingCancellationToken);

        await TestServices.Editor.PasteAsync("A & B\r\nLine 2", HangMitigatingCancellationToken);

        AssertEx.EqualOrDiff(
            JoinLines("\r\n", "", "''' <summary>", "''' A &amp; B", "''' Line 2", "''' </summary>", "Public Class C", "End Class", ""),
            await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));

        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.Undo, HangMitigatingCancellationToken);
        AssertEx.EqualOrDiff(
            JoinLines("\r\n", "", "''' <summary>", "''' A & B", "Line 2", "''' </summary>", "Public Class C", "End Class", ""),
            await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));

        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.Undo, HangMitigatingCancellationToken);
        AssertEx.EqualOrDiff(
            JoinLines("\r\n", "", "''' <summary>", "''' ", "''' </summary>", "Public Class C", "End Class", ""),
            await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
    }

    private static string JoinLines(string newLine, params string[] lines)
        => string.Join(newLine, lines);
}
