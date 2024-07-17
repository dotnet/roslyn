// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Utilities;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

public class CSharpReplIntellisense : AbstractInteractiveWindowTest
{
    [IdeFact]
    public async Task VerifyCompletionListOnEmptyTextAtTopLevel()
    {
        await TestServices.InteractiveWindow.InvokeCompletionListAsync(HangMitigatingCancellationToken);
        var completionItems = (await TestServices.InteractiveWindow.GetCompletionItemsAsync(HangMitigatingCancellationToken)).SelectAsArray(item => item.DisplayText);
        Assert.All(
            [
                "var",
                "public",
                "readonly",
                "goto",
            ],
            item => Assert.Contains(item, completionItems));
    }

    [IdeFact]
    public async Task VerifySharpRCompletionList()
    {
        await TestServices.InteractiveWindow.InsertCodeAsync("#r \"", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.InvokeCompletionListAsync(HangMitigatingCancellationToken);
        Assert.Contains("System", (await TestServices.InteractiveWindow.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(item => item.DisplayText));
    }

    [IdeFact]
    public async Task VerifyCommitCompletionOnTopLevel()
    {
        await TestServices.InteractiveWindow.InsertCodeAsync("pub", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.InvokeCompletionListAsync(HangMitigatingCancellationToken);
        Assert.Contains("public", (await TestServices.InteractiveWindow.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(item => item.DisplayText));
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
        Assert.Equal("public", await TestServices.InteractiveWindow.GetLastReplInputAsync(HangMitigatingCancellationToken));
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.ESCAPE, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task VerifyCompletionListForAmbiguousParsingCases()
    {
        await TestServices.InteractiveWindow.InsertCodeAsync(@"class C { }
public delegate R Del<T, R>(T arg);
Del<C, System", HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.OEM_PERIOD, HangMitigatingCancellationToken);
        Assert.Contains("ArgumentException", (await TestServices.InteractiveWindow.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(item => item.DisplayText));
    }

    [IdeFact]
    public async Task VerifySharpLoadCompletionList()
    {
        await TestServices.InteractiveWindow.InsertCodeAsync("#load \"", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.InvokeCompletionListAsync(HangMitigatingCancellationToken);
        Assert.Contains("C:", (await TestServices.InteractiveWindow.GetCompletionItemsAsync(HangMitigatingCancellationToken)).Select(item => item.DisplayText));
    }

    [IdeFact]
    public async Task VerifyNoCrashOnEnter()
    {
        await TestServices.Editor.SetUseSuggestionModeAsync(false, HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync(["#help", VirtualKeyCode.RETURN, VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task VerifyCorrectIntellisenseSelectionOnEnter()
    {
        await TestServices.Editor.SetUseSuggestionModeAsync(false, HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync("TimeSpan.FromMin", HangMitigatingCancellationToken);
        await TestServices.Editor.InvokeCompletionListAsync(HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync([VirtualKeyCode.RETURN, "(0d)", VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForReplOutputAsync("[00:00:00]", HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task VerifyCompletionListForLoadMembers()
    {
        using var temporaryTextFile = new TemporaryTextFile(
            "c.csx",
            "int x = 2; class Complex { public int goo() { return 4; } }");
        temporaryTextFile.Create();
        await TestServices.InteractiveWindow.SubmitTextAsync(string.Format("#load \"{0}\"", temporaryTextFile.FullName), HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.InvokeCompletionListAsync(HangMitigatingCancellationToken);
        var completionItems = (await TestServices.InteractiveWindow.GetCompletionItemsAsync(HangMitigatingCancellationToken)).SelectAsArray(item => item.DisplayText);
        Assert.All(
            [
                "x",
                "Complex",
            ],
            item => Assert.Contains(item, completionItems));
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.ESCAPE, HangMitigatingCancellationToken);
    }
}
