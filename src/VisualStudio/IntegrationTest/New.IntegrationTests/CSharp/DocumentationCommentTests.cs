// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

[Trait(Traits.Feature, Traits.Features.DocumentationComments)]
public sealed class DocumentationCommentTests : AbstractEditorTest
{
    public DocumentationCommentTests()
        : base(nameof(DocumentationCommentTests))
    {
    }

    protected override string LanguageName => LanguageNames.CSharp;

    [IdeFact, WorkItem("https://github.com/dotnet/roslyn/issues/54391")]
    public async Task TypingCharacter_MultiCaret()
    {
        await SetUpEditorAsync("""

            //{|selection:|}
            class C1 { }

            //{|selection:|}
            class C2 { }

            //{|selection:|}
            class C3 { }

            """, HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync('/', HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync("""

            /// <summary>
            /// $$
            /// </summary>
            class C1 { }

            /// <summary>
            /// 
            /// </summary>
            class C2 { }

            /// <summary>
            /// 
            /// </summary>
            class C3 { }

            """, assertCaretPosition: true, cancellationToken: HangMitigatingCancellationToken);
    }

    [IdeTheory, WorkItem("https://github.com/dotnet/roslyn/issues/17383")]
    [InlineData("\r\n", "\r\n", "\r\n")]
    [InlineData("\n", "\r\n", "\n")]
    [InlineData("\r\n", "\n", "\n")]
    public async Task Paste_MultilineText(string documentNewLine, string editorNewLine, string pastedNewLine)
    {
        await SetUpEditorAsync(
            JoinLines(documentNewLine,
                "",
                "class C",
                "{",
                "    /// <summary>",
                "    /// $$",
                "    /// </summary>",
                "}",
                ""),
            HangMitigatingCancellationToken);
        await TestServices.Editor.SetNewLineCharacterAsync(editorNewLine, HangMitigatingCancellationToken);

        await TestServices.Editor.PasteAsync(
            "Line 1" + pastedNewLine + "Line 2 with List<int> & value", HangMitigatingCancellationToken);

        AssertEx.EqualOrDiff(
            "" + documentNewLine +
            "class C" + documentNewLine +
            "{" + documentNewLine +
            "    /// <summary>" + documentNewLine +
            "    /// Line 1" + documentNewLine +
            "    /// Line 2 with List<int> &amp; value" + documentNewLine +
            "    /// </summary>" + documentNewLine +
            "}" + documentNewLine,
            await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
    }

    [IdeFact, WorkItem("https://github.com/dotnet/roslyn/issues/17383")]
    public async Task Paste_MixedSelections()
    {
        await SetUpEditorAsync(
            JoinLines("\r\n",
                "",
                "/// <summary>Replace {|selection:documentation|}</summary>",
                "// Replace {|selection:ordinary|}",
                "class C",
                "{",
                "}",
                ""),
            HangMitigatingCancellationToken);

        await TestServices.Editor.PasteAsync("A & B\r\nLine 2\r\nLine 3", HangMitigatingCancellationToken);

        AssertEx.EqualOrDiff(
            JoinLines("\r\n",
                "",
                "/// <summary>Replace A &amp; B",
                "/// Line 2",
                "/// Line 3</summary>",
                "// Replace A & B",
                "Line 2",
                "Line 3",
                "class C",
                "{",
                "}",
                ""),
            await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
    }

    [IdeFact, WorkItem("https://github.com/dotnet/roslyn/issues/17383")]
    public async Task Paste_Undo()
    {
        await SetUpEditorAsync(
            JoinLines("\r\n",
                "",
                "/// <summary>",
                "/// $$",
                "/// </summary>",
                "class C",
                "{",
                "}",
                ""),
            HangMitigatingCancellationToken);

        await TestServices.Editor.PasteAsync("A & B\r\nLine 2", HangMitigatingCancellationToken);

        AssertEx.EqualOrDiff(
            JoinLines("\r\n", "", "/// <summary>", "/// A &amp; B", "/// Line 2", "/// </summary>", "class C", "{", "}", ""),
            await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));

        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.Undo, HangMitigatingCancellationToken);
        AssertEx.EqualOrDiff(
            JoinLines("\r\n", "", "/// <summary>", "/// A & B", "Line 2", "/// </summary>", "class C", "{", "}", ""),
            await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));

        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.Undo, HangMitigatingCancellationToken);
        AssertEx.EqualOrDiff(
            JoinLines("\r\n", "", "/// <summary>", "/// ", "/// </summary>", "class C", "{", "}", ""),
            await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
    }

    private static string JoinLines(string newLine, params string[] lines)
        => string.Join(newLine, lines);
}
