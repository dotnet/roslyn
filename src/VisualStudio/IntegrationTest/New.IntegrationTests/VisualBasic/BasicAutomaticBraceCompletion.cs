// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.InlineRename;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic;

[Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
public class BasicAutomaticBraceCompletion : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.VisualBasic;

    public BasicAutomaticBraceCompletion()
        : base(nameof(BasicAutomaticBraceCompletion))
    {
    }

    [IdeTheory, CombinatorialData]
    public async Task Braces_InsertionAndTabCompleting(bool argumentCompletion)
    {
        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        globalOptions.SetGlobalOption(CompletionViewOptionsStorage.EnableArgumentCompletionSnippets, LanguageNames.VisualBasic, argumentCompletion);

        // Disable new rename UI for now, it's causing these tests to fail.
        // https://github.com/dotnet/roslyn/issues/63576
        globalOptions.SetGlobalOption(InlineRenameUIOptionsStorage.UseInlineAdornment, false);

        await SetUpEditorAsync(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class", HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("Dim x = {", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Dim x = {$$}", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync(
            [
                "New Object",
                VirtualKeyCode.ESCAPE,
                VirtualKeyCode.TAB,
            ],
            HangMitigatingCancellationToken);

        if (argumentCompletion)
        {
            await TestServices.EditorVerifier.CurrentLineTextAsync("        Dim x = {New Object($$)}", assertCaretPosition: true, HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.SignatureHelp], HangMitigatingCancellationToken);

            await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        Dim x = {New Object()$$}", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        Dim x = {New Object()}$$", assertCaretPosition: true, HangMitigatingCancellationToken);
        }
        else
        {
            await TestServices.EditorVerifier.CurrentLineTextAsync("        Dim x = {New Object}$$", assertCaretPosition: true, HangMitigatingCancellationToken);
        }
    }

    [IdeFact]
    public async Task Braces_Overtyping()
    {
        await SetUpEditorAsync(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class", HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("Dim x = {", HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync('}', HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Dim x = {}$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task ParenthesesTypeoverAfterStringLiterals()
    {
        await SetUpEditorAsync(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class", HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("Console.Write(", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Console.Write($$)", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync('"', HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Console.Write(\"$$\")", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync('"', HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Console.Write(\"\"$$)", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync(')', HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Console.Write(\"\")$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task Braces_OnReturnNoFormattingOnlyIndentationBeforeCloseBrace()
    {
        await SetUpEditorAsync(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class", HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("Dim x = {", HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("            $$}", assertCaretPosition: true, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync(@"
Class C
    Sub Goo()
        Dim x = {
            $$}
    End Sub
End Class",
assertCaretPosition: true,
HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task Paren_InsertionAndTabCompleting()
    {
        await SetUpEditorAsync(@"
Class C
    $$
End Class", HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("Sub Goo(", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("    Sub Goo($$)", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("x As Long", HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.ESCAPE, HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("    Sub Goo(x As Long)$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task Paren_Overtyping()
    {
        await SetUpEditorAsync(@"
Class C
    $$
End Class", HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("Sub Goo(", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("    Sub Goo($$)", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.ESCAPE, HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync(')', HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("    Sub Goo()$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task Bracket_Insertion()
    {
        await SetUpEditorAsync(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class", HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("Dim [Dim", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Dim [Dim$$]", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task Bracket_Overtyping()
    {
        await SetUpEditorAsync(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class", HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("Dim [Dim", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Dim [Dim$$]", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("] As Long", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Dim [Dim] As Long$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task DoubleQuote_InsertionAndTabCompletion()
    {
        // Disable new rename UI for now, it's causing these tests to fail.
        // https://github.com/dotnet/roslyn/issues/63576
        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        globalOptions.SetGlobalOption(InlineRenameUIOptionsStorage.UseInlineAdornment, false);

        await SetUpEditorAsync(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class", HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("Dim str = \"", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Dim str = \"$$\"", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Dim str = \"\"$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task Nested_AllKinds_1()
    {
        await SetUpEditorAsync(@"
Class C
    Sub New([dim] As String)
    End Sub

    Sub Goo()
        $$
    End Sub
End Class", HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync(
            [
                "Dim y = {New C([dim",
                VirtualKeyCode.ESCAPE,
                "]:=\"hello({[\")}",
                VirtualKeyCode.RETURN,
            ],
            HangMitigatingCancellationToken);
        var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.Contains("Dim y = {New C([dim]:=\"hello({[\")}", actualText);
    }

    [IdeFact]
    public async Task Nested_AllKinds_2()
    {
        await SetUpEditorAsync(@"
Class C
    Sub New([dim] As String)
    End Sub

    Sub Goo()
        $$
    End Sub
End Class", HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync(
            [
                "Dim y = {New C([dim",
                VirtualKeyCode.ESCAPE,
                VirtualKeyCode.TAB,
                ":=\"hello({[",
                VirtualKeyCode.TAB,
                VirtualKeyCode.TAB,
                VirtualKeyCode.TAB,
                VirtualKeyCode.RETURN,
            ],
            HangMitigatingCancellationToken);
        var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.Contains("Dim y = {New C([dim]:=\"hello({[\")}", actualText);
    }

    [IdeFact]
    public async Task Negative_NoCompletionInComments()
    {
        await SetUpEditorAsync(@"
Class C
    Sub Goo()
        ' $$
    End Sub
End Class", HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("{([\"", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        ' {([\"$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task Negative_NoCompletionInStringLiterals()
    {
        await SetUpEditorAsync(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class", HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("Dim s = \"{([", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Dim s = \"{([$$\"", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task Negative_NoCompletionInXmlDocComment()
    {
        await SetUpEditorAsync(@"
$$
Class C
End Class", HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("'''", HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync('{', HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync('(', HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync('[', HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync('"', HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("''' {([\"$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task Negative_NoCompletionInXmlDocCommentAtEndOfTag()
    {
        await SetUpEditorAsync(@"
Class C
    ''' <summary>
    ''' <see></see>$$
    ''' </summary>
    Sub Goo()
    End Sub
End Class", HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("(", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("    ''' <see></see>($$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [WorkItem(652015, "DevDiv")]
    [IdeFact]
    public async Task LineCommittingIssue()
    {
        await SetUpEditorAsync(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class", HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("Dim x=\"\" '", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Dim x=\"\" '$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [WorkItem(653399, "DevDiv")]
    [IdeFact]
    public async Task VirtualWhitespaceIssue()
    {
        await SetUpEditorAsync(@"
Class C
    Sub Goo()$$
    End Sub
End Class", HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync('(', HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.BACK, HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.CurrentLineTextAsync("        $$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [WorkItem(659684, "DevDiv")]
    [IdeFact]
    public async Task CompletionWithIntelliSenseWindowUp()
    {
        await SetUpEditorAsync(@"
Class C
    Sub Goo()
    End Sub
    Sub Test()
        $$
    End Sub
End Class", HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("Goo(", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Goo($$)", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [WorkItem(657451, "DevDiv")]
    [IdeFact]
    public async Task CompletionAtTheEndOfFile()
    {
        await SetUpEditorAsync(@"
Class C
    $$", HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("Sub Goo(", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("    Sub Goo($$)", assertCaretPosition: true, HangMitigatingCancellationToken);
    }
}
