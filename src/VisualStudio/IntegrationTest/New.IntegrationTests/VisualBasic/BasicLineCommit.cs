// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using WindowsInput.Native;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic;

[Trait(Traits.Feature, Traits.Features.LineCommit)]
public class BasicLineCommit : AbstractEditorTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    protected override string LanguageName => LanguageNames.VisualBasic;

    public BasicLineCommit(ITestOutputHelper testOutputHelper)
        : base(nameof(BasicLineCommit))
    {
        _testOutputHelper = testOutputHelper;
    }

    private void Log(string message)
        => _testOutputHelper.WriteLine(message);

    [IdeFact]
    public async Task CaseCorrection()
    {
        await TestServices.Editor.SetTextAsync("""
            Module Goo
                Sub M()
            Dim x = Sub()
                End Sub
            End Module
            """, HangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("Sub()", charsOffset: 1, HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
        Assert.Equal(48, (await TestServices.Editor.GetCaretPositionAsync(HangMitigatingCancellationToken)).BufferPosition.Position);
    }

    [IdeFact]
    public async Task UndoWithEndConstruct()
    {
        await TestServices.Editor.SetTextAsync("""
            Module Module1
                Sub Main()
                End Sub
                REM
            End Module
            """, HangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("    REM", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync(["sub", VirtualKeyCode.ESCAPE, " goo()", VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        AssertEx.EqualOrDiff("""
            Module Module1
                Sub Main()
                End Sub
                Sub goo()

                End Sub
            End Module
            """, await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.Undo, HangMitigatingCancellationToken);
        Assert.Equal(54, (await TestServices.Editor.GetCaretPositionAsync(HangMitigatingCancellationToken)).BufferPosition.Position);
    }

    [IdeFact]
    public async Task UndoWithoutEndConstruct()
    {
        Log("Setting up the initial editor text");
        await TestServices.Editor.SetTextAsync("""
            Module Module1

                ''' <summary></summary>
                Sub Main()
                End Sub
            End Module
            """, HangMitigatingCancellationToken);

        Log("Placing the caret at 'Module1'");
        await TestServices.Editor.PlaceCaretAsync("Module1", charsOffset: 0, HangMitigatingCancellationToken);
        Log("Pressing DOWN then RETURN");
        await TestServices.Input.SendAsync([VirtualKeyCode.DOWN, VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        Log("Verifying the resulting editor text");
        AssertEx.EqualOrDiff("""
            Module Module1


                ''' <summary></summary>
                Sub Main()
                End Sub
            End Module
            """, await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
        Log("Verifying the caret position after the edit");
        Assert.Equal(18, (await TestServices.Editor.GetCaretPositionAsync(HangMitigatingCancellationToken)).BufferPosition.Position);
        Log("Executing Undo");
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.Undo, HangMitigatingCancellationToken);
        Log("Verifying the caret position after Undo");
        Assert.Equal(16, (await TestServices.Editor.GetCaretPositionAsync(HangMitigatingCancellationToken)).BufferPosition.Position);
    }

    [IdeFact]
    public async Task CommitOnSave()
    {
        Log("Setting up the initial editor text");
        await TestServices.Editor.SetTextAsync("""
            Module Module1
                Sub Main()
                End Sub
            End Module

            """, HangMitigatingCancellationToken);

        Log("Placing the caret inside 'Main('");
        await TestServices.Editor.PlaceCaretAsync("(", charsOffset: 1, HangMitigatingCancellationToken);
        Log("Typing the parameter declaration and pressing TAB");
        await TestServices.Input.SendAsync(["x   As   integer", VirtualKeyCode.TAB], HangMitigatingCancellationToken);

        Log("Verifying the document is not yet saved");
        Assert.False(await TestServices.Editor.IsSavedAsync(HangMitigatingCancellationToken));
        Log("Sending Ctrl+S to save the document");
        await TestServices.Input.SendAsync((VirtualKeyCode.VK_S, VirtualKeyCode.CONTROL), HangMitigatingCancellationToken);

        // Wait for async save operations to complete before proceeding
        Log("Waiting for async workspace operations to complete");
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace], HangMitigatingCancellationToken);

        Log("Verifying the active document is saved");
        await TestServices.SolutionExplorerVerifier.ActiveDocumentIsSavedAsync(HangMitigatingCancellationToken);
        Assert.True(await TestServices.Editor.IsSavedAsync(HangMitigatingCancellationToken));
        Log("Verifying the committed (formatted) text");
        AssertEx.EqualOrDiff("""
            Module Module1
                Sub Main(x As Integer)
                End Sub
            End Module

            """, await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));

        Log("Executing Undo");
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.Undo, HangMitigatingCancellationToken);
        Log("Verifying the text reverted to the uncommitted spacing");
        AssertEx.EqualOrDiff("""
            Module Module1
                Sub Main(x   As   Integer)
                End Sub
            End Module

            """, await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
        Log("Verifying the caret position after Undo");
        Assert.Equal(45, (await TestServices.Editor.GetCaretPositionAsync(HangMitigatingCancellationToken)).BufferPosition.Position);
    }

    [IdeFact]
    public async Task CommitOnFocusLost()
    {
        await TestServices.Editor.SetTextAsync("""
            Module M
                Sub M()
                End Sub
            End Module
            """, HangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("End Sub", charsOffset: -1, HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync(" ", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "TestZ.vb", open: true, cancellationToken: HangMitigatingCancellationToken); // Cause focus lost
        await TestServices.Input.SendAsync("                  ", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.CloseCodeFileAsync(ProjectName, "TestZ.vb", saveFile: false, cancellationToken: HangMitigatingCancellationToken);
        AssertEx.EqualOrDiff("""
            Module M
                Sub M()
                End Sub
            End Module
            """, await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
    }

    [IdeFact]
    public async Task CommitOnFocusLostDoesNotFormatWithPrettyListingOff()
    {
        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        globalOptions.SetGlobalOption(LineCommitOptionsStorage.PrettyListing, LanguageNames.VisualBasic, false);

        await TestServices.Editor.SetTextAsync("""
            Module M
                Sub M()
                End Sub
            End Module
            """, HangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("End Sub", charsOffset: -1, HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync(" ", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "TestZ.vb", open: true, cancellationToken: HangMitigatingCancellationToken); // Cause focus lost
        await TestServices.Input.SendAsync("                  ", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.CloseCodeFileAsync(ProjectName, "TestZ.vb", saveFile: false, HangMitigatingCancellationToken);
        AssertEx.EqualOrDiff("""
            Module M
                Sub M()
                 End Sub
            End Module
            """, await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
    }
}
