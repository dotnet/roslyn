// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.ReferenceHighlighting;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

public class CSharpReplIdeFeatures : AbstractInteractiveWindowTest
{
    public override async Task DisposeAsync()
    {
        await TestServices.Editor.SetUseSuggestionModeAsync(false, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.ClearReplTextAsync(HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.ResetAsync(HangMitigatingCancellationToken);
        await base.DisposeAsync();
    }

    [IdeFact]
    public async Task VerifyDefaultUsingStatements()
    {
        await TestServices.InteractiveWindow.SubmitTextAsync("Console.WriteLine(42);", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("42", HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task VerifyCodeActionsNotAvailableInPreviousSubmission()
    {
        await TestServices.InteractiveWindow.InsertCodeAsync("Console.WriteLine(42);", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindowVerifier.CodeActionsNotShowingAsync(HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task VerifyQuickInfoOnStringDocCommentsFromMetadata()
    {
        await TestServices.InteractiveWindow.InsertCodeAsync("static void Goo(string[] args) { }", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.PlaceCaretAsync("[]", charsOffset: -2, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.InvokeQuickInfoAsync(HangMitigatingCancellationToken);
        var s = await TestServices.InteractiveWindow.GetQuickInfoAsync(HangMitigatingCancellationToken);
        Assert.Equal("class System.String", s);
    }

    [IdeFact]
    public async Task International()
    {
        await TestServices.InteractiveWindow.InsertCodeAsync(@"delegate void العربية();
العربية func = () => System.Console.WriteLine(2);", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.PlaceCaretAsync("func", charsOffset: -1, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.InvokeQuickInfoAsync(HangMitigatingCancellationToken);
        var s = await TestServices.InteractiveWindow.GetQuickInfoAsync(HangMitigatingCancellationToken);
        Assert.Equal("(field) العربية func", s);
    }

    [IdeFact]
    public async Task HighlightRefsSingleSubmissionVerifyRenameTagsShowUpWhenInvokedOnUnsubmittedText()
    {
        await TestServices.InteractiveWindow.InsertCodeAsync("int someint; someint = 22; someint = 23;", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.PlaceCaretAsync("someint = 22", charsOffset: -6, HangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.ReferenceHighlighting, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.VerifyTagsAsync<WrittenReferenceHighlightTag>(2, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.VerifyTagsAsync<DefinitionHighlightTag>(1, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task HighlightRefsSingleSubmissionVerifyRenameTagsGoAway()
    {
        await TestServices.InteractiveWindow.InsertCodeAsync("int someint; someint = 22; someint = 23;", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.PlaceCaretAsync("someint = 22", charsOffset: -6, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.ReferenceHighlighting, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.VerifyTagsAsync<WrittenReferenceHighlightTag>(2, HangMitigatingCancellationToken);

        await TestServices.InteractiveWindow.PlaceCaretAsync("22", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.ReferenceHighlighting, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.VerifyTagsAsync<DefinitionHighlightTag>(0, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.VerifyTagsAsync<ReferenceHighlightTag>(0, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.VerifyTagsAsync<WrittenReferenceHighlightTag>(0, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task HighlightRefsMultipleSubmisionsVerifyRenameTagsShowUpWhenInvokedOnSubmittedText()
    {
        await TestServices.InteractiveWindow.SubmitTextAsync("class Goo { }", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.SubmitTextAsync("Goo something = new Goo();", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.SubmitTextAsync("something.ToString();", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.PlaceCaretAsync("someth", charsOffset: 1, occurrence: 2, extendSelection: false, selectBlock: false, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.ReferenceHighlighting, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.VerifyTagsAsync<DefinitionHighlightTag>(1, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.VerifyTagsAsync<ReferenceHighlightTag>(1, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task HighlightRefsMultipleSubmisionsVerifyRenameTagsShowUpOnUnsubmittedText()
    {
        await TestServices.InteractiveWindow.SubmitTextAsync("class Goo { }", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.SubmitTextAsync("Goo something = new Goo();", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.InsertCodeAsync("something.ToString();", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.PlaceCaretAsync("someth", charsOffset: 1, occurrence: 2, extendSelection: false, selectBlock: false, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.ReferenceHighlighting, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.VerifyTagsAsync<DefinitionHighlightTag>(1, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.VerifyTagsAsync<ReferenceHighlightTag>(1, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task HighlightRefsMultipleSubmisionsVerifyRenameTagsShowUpOnTypesWhenInvokedOnSubmittedText()
    {
        await TestServices.InteractiveWindow.SubmitTextAsync("class Goo { }", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.SubmitTextAsync("Goo a;", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.SubmitTextAsync("Goo b;", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.PlaceCaretAsync("Goo b", charsOffset: -1, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.ReferenceHighlighting, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.VerifyTagsAsync<DefinitionHighlightTag>(1, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.VerifyTagsAsync<ReferenceHighlightTag>(2, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task HighlightRefsMultipleSubmisionsVerifyRenameTagsShowUpOnTypesWhenInvokedOnUnsubmittedText()
    {
        await TestServices.InteractiveWindow.SubmitTextAsync("class Goo { }", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.SubmitTextAsync("Goo a;", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.InsertCodeAsync("Goo b;", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.PlaceCaretAsync("Goo b", charsOffset: -1, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.ReferenceHighlighting, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.VerifyTagsAsync<DefinitionHighlightTag>(1, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.VerifyTagsAsync<ReferenceHighlightTag>(2, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task HighlightRefsMultipleSubmisionsVerifyRenameTagsGoAwayWhenInvokedOnUnsubmittedText()
    {
        await TestServices.InteractiveWindow.SubmitTextAsync("class Goo { }", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.SubmitTextAsync("Goo a;", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.InsertCodeAsync("Goo b;Something();", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.PlaceCaretAsync("Something();", charsOffset: -1, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.VerifyTagsAsync<DefinitionHighlightTag>(0, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.VerifyTagsAsync<ReferenceHighlightTag>(0, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task HighlightRefsMultipleSubmisionsVerifyRenameTagsOnRedefinedVariable()
    {
        await TestServices.InteractiveWindow.SubmitTextAsync("string abc = null;", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.SubmitTextAsync("abc = string.Empty;", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.InsertCodeAsync("int abc = 42;", HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.PlaceCaretAsync("abc", charsOffset: 0, occurrence: 3, extendSelection: false, selectBlock: false, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.ReferenceHighlighting, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.VerifyTagsAsync<DefinitionHighlightTag>(1, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.VerifyTagsAsync<ReferenceHighlightTag>(0, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task DisabledCommandsPart1()
    {
        await TestServices.InteractiveWindow.InsertCodeAsync(@"public class Class
{
    int field;

    public void Method(int x)
    {
         int abc = 1 + 1;
     }
}", HangMitigatingCancellationToken);

        await TestServices.InteractiveWindow.PlaceCaretAsync("abc", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace, HangMitigatingCancellationToken);
        Assert.False(await TestServices.Shell.IsCommandAvailableAsync(WellKnownCommands.Refactor.Rename, HangMitigatingCancellationToken));

        await TestServices.InteractiveWindow.PlaceCaretAsync("1 + 1", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace, HangMitigatingCancellationToken);
        Assert.False(await TestServices.Shell.IsCommandAvailableAsync(WellKnownCommands.Refactor.ExtractMethod, HangMitigatingCancellationToken));

        await TestServices.InteractiveWindow.PlaceCaretAsync("Class", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace, HangMitigatingCancellationToken);
        Assert.False(await TestServices.Shell.IsCommandAvailableAsync(WellKnownCommands.Refactor.ExtractInterface, HangMitigatingCancellationToken));

        await TestServices.InteractiveWindow.PlaceCaretAsync("field", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace, HangMitigatingCancellationToken);
        Assert.False(await TestServices.Shell.IsCommandAvailableAsync(WellKnownCommands.Refactor.EncapsulateField, HangMitigatingCancellationToken));

        await TestServices.InteractiveWindow.PlaceCaretAsync("Method", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace, HangMitigatingCancellationToken);
        Assert.False(await TestServices.Shell.IsCommandAvailableAsync(WellKnownCommands.Refactor.RemoveParameters, HangMitigatingCancellationToken));
        Assert.False(await TestServices.Shell.IsCommandAvailableAsync(WellKnownCommands.Refactor.ReorderParameters, HangMitigatingCancellationToken));
    }

    [IdeFact]
    public async Task AddUsing()
    {
        await TestServices.InteractiveWindow.InsertCodeAsync("typeof(ArrayList)", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.PlaceCaretAsync("ArrayList", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
        await TestServices.InteractiveWindowVerifier.CodeActionsAsync(
            new string[] { "using System.Collections;", "System.Collections.ArrayList" },
            "using System.Collections;", cancellationToken: HangMitigatingCancellationToken);

        Assert.Equal(@"using System.Collections;

typeof(ArrayList)", await TestServices.InteractiveWindow.GetLastReplInputAsync(HangMitigatingCancellationToken));
    }

    [IdeFact]
    public async Task QualifyName()
    {
        await TestServices.InteractiveWindow.InsertCodeAsync("typeof(ArrayList)", HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.PlaceCaretAsync("ArrayList", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindowVerifier.CodeActionsAsync(
new string[] { "using System.Collections;", "System.Collections.ArrayList" },
"System.Collections.ArrayList", cancellationToken: HangMitigatingCancellationToken);
        Assert.Equal("typeof(System.Collections.ArrayList)", await TestServices.InteractiveWindow.GetLastReplInputAsync(HangMitigatingCancellationToken));
    }
}
