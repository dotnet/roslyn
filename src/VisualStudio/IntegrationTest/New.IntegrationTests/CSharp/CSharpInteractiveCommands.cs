// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

public class CSharpInteractiveCommands : AbstractInteractiveWindowTest
{
    [IdeFact]
    public async Task VerifyPreviousAndNextHistory()
    {
        await TestServices.InteractiveWindow.SubmitTextAsync("1 + 2", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.SubmitTextAsync("1.ToString()", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("""
            "1"
            """, HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync((VirtualKeyCode.UP, VirtualKeyCode.MENU), HangMitigatingCancellationToken);
        Assert.Equal("1.ToString()", await TestServices.InteractiveWindow.GetLastReplInputAsync(HangMitigatingCancellationToken));
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);

        // Since this output is the same as the previous, make sure to wait for it to finish executing and not just
        // check the previous result
        await TestServices.InteractiveWindow.WaitForLastReplInputAsync("", HangMitigatingCancellationToken);

        await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("""
            "1"
            """, HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync((VirtualKeyCode.UP, VirtualKeyCode.MENU), HangMitigatingCancellationToken);
        Assert.Equal("1.ToString()", await TestServices.InteractiveWindow.GetLastReplInputAsync(HangMitigatingCancellationToken));
        await TestServices.Input.SendWithoutActivateAsync((VirtualKeyCode.UP, VirtualKeyCode.MENU), HangMitigatingCancellationToken);
        Assert.Equal("1 + 2", await TestServices.InteractiveWindow.GetLastReplInputAsync(HangMitigatingCancellationToken));
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("3", HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync((VirtualKeyCode.DOWN, VirtualKeyCode.MENU), HangMitigatingCancellationToken);
        Assert.Equal("1.ToString()", await TestServices.InteractiveWindow.GetLastReplInputAsync(HangMitigatingCancellationToken));
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("""
            "1"
            """, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task VerifyMaybeExecuteInput()
    {
        await TestServices.InteractiveWindow.InsertCodeAsync("2 + 3", HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("5", HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task VerifyNewLineAndIndent()
    {
        await TestServices.InteractiveWindow.InsertCodeAsync("3 + ", HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.InsertCodeAsync("4", HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("7", HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task VerifyExecuteInput()
    {
        await TestServices.InteractiveWindow.SubmitTextAsync("1 + ", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputContainsAsync("CS1733", HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task VerifyForceNewLineAndIndent()
    {
        await TestServices.InteractiveWindow.InsertCodeAsync("1 + 2", HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.SubmitTextAsync("+ 3", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputContainsAsync("3", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindowVerifier.ReplPromptConsistencyAsync("<![CDATA[1 + 2 + 3]]>", "6", HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task VerifyCancelInput()
    {
        await TestServices.InteractiveWindow.InsertCodeAsync("1 + 4", HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync((VirtualKeyCode.RETURN, VirtualKeyCode.SHIFT), HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.ESCAPE, HangMitigatingCancellationToken);
        Assert.Equal(string.Empty, await TestServices.InteractiveWindow.GetLastReplInputAsync(HangMitigatingCancellationToken));
    }

    [IdeFact]
    public async Task VerifyUndoAndRedo()
    {
        await TestServices.InteractiveWindow.ClearReplTextAsync(HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.InsertCodeAsync(" 2 + 4 ", HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync((VirtualKeyCode.VK_Z, VirtualKeyCode.CONTROL), HangMitigatingCancellationToken);
        await TestServices.InteractiveWindowVerifier.ReplPromptConsistencyAsync("< ![CDATA[]] >", string.Empty, HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync((VirtualKeyCode.VK_Y, VirtualKeyCode.CONTROL), HangMitigatingCancellationToken);
        Assert.Equal(" 2 + 4 ", await TestServices.InteractiveWindow.GetLastReplInputAsync(HangMitigatingCancellationToken));
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("6", HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task CutDeletePasteSelectAll()
    {
        await ClearInteractiveWindowAsync(HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.InsertCodeAsync("Text", HangMitigatingCancellationToken);
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.LineStart, HangMitigatingCancellationToken);
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.LineEnd, HangMitigatingCancellationToken);
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.LineStartExtend, HangMitigatingCancellationToken);
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.SelectionCancel, HangMitigatingCancellationToken);
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.LineEndExtend, HangMitigatingCancellationToken);
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.SelectAll, HangMitigatingCancellationToken);
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.SelectAll, HangMitigatingCancellationToken);
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.Copy, HangMitigatingCancellationToken);
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.Cut, HangMitigatingCancellationToken);
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.Paste, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplInputContainsAsync("Text", HangMitigatingCancellationToken);
        Assert.Equal("Text", await TestServices.InteractiveWindow.GetLastReplInputAsync(HangMitigatingCancellationToken));
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.Delete, HangMitigatingCancellationToken);
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.LineUp, HangMitigatingCancellationToken);
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.LineDown, HangMitigatingCancellationToken);
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.Paste, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplInputContainsAsync("TextText", HangMitigatingCancellationToken);
        Assert.Equal("TextText", await TestServices.InteractiveWindow.GetLastReplInputAsync(HangMitigatingCancellationToken));
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.Paste, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplInputContainsAsync("TextTextText", HangMitigatingCancellationToken);
        Assert.Equal("TextTextText", await TestServices.InteractiveWindow.GetLastReplInputAsync(HangMitigatingCancellationToken));
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.ESCAPE, HangMitigatingCancellationToken);
    }

    //<!-- Regression test for bug 13731.
    //     Unfortunately we don't have good unit-test infrastructure to test InteractiveWindow.cs.
    //     For now, since we don't have coverage of InteractiveWindow.IndentCurrentLine at all,
    //     I'd rather have a quick integration test scenario rather than no coverage at all.
    //     At some point when we start investing in Interactive work again, we'll go through some
    //     of these tests and convert them to unit-tests.
    //     -->
    //<!-- TODO(https://github.com/dotnet/roslyn/issues/4235)
    [IdeFact]
    public async Task VerifyReturnIndentCurrentLine()
    {
        await TestServices.InteractiveWindow.ClearScreenAsync(HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync(" (", HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync(")", HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.LEFT, HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
        Assert.Equal(12, (await TestServices.InteractiveWindow.GetCaretPositionAsync(HangMitigatingCancellationToken)).BufferPosition.Position);
    }
}
