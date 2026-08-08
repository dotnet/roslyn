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
public sealed class DocumentationCommentTests : AbstractEditorTest
{
    public DocumentationCommentTests()
        : base(nameof(DocumentationCommentTests))
    {
    }

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
