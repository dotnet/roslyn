// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic;

public abstract class BasicErrorListCommon : AbstractEditorTest
{
    protected BasicErrorListCommon(string templateName)
        : base(nameof(BasicErrorListCommon), templateName)
    {
    }

    protected override string LanguageName => LanguageNames.VisualBasic;

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/63041")]
    public virtual async Task ErrorList()
    {
        await TestServices.Editor.SetTextAsync(@"
Module Module1

    Function Good() As P
        Return Nothing
    End Function

    Sub Main()
        Goo()
    End Sub

End Module
", HangMitigatingCancellationToken);
        await TestServices.ErrorList.ShowErrorListAsync(HangMitigatingCancellationToken);
        var expectedContents = new[] {
            "Class1.vb(4, 24): error BC30002: Type 'P' is not defined.",
            "Class1.vb(9, 9): error BC30451: 'Goo' is not declared. It may be inaccessible due to its protection level.",
        };
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawlerLegacy, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles, FeatureAttribute.ErrorList], HangMitigatingCancellationToken);
        var actualContents = await TestServices.ErrorList.GetErrorsAsync(HangMitigatingCancellationToken);
        AssertEx.EqualOrDiff(
            string.Join(Environment.NewLine, expectedContents),
            string.Join(Environment.NewLine, actualContents));

        await TestServices.ErrorList.NavigateToErrorListItemAsync(0, isPreview: false, shouldActivate: true, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CaretPositionAsync(43, HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.BuildSolutionAndWaitAsync(HangMitigatingCancellationToken);
        await TestServices.ErrorList.ShowErrorListAsync(HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawlerLegacy, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles, FeatureAttribute.ErrorList], HangMitigatingCancellationToken);
        actualContents = await TestServices.ErrorList.GetErrorsAsync(HangMitigatingCancellationToken);
        AssertEx.EqualOrDiff(
            string.Join(Environment.NewLine, expectedContents),
            string.Join(Environment.NewLine, actualContents));
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/63981 and https://github.com/dotnet/roslyn/issues/63982")]
    public virtual async Task ErrorsDuringMethodBodyEditing()
    {
        await TestServices.Editor.SetTextAsync(@"
Namespace N
    Class C
        Private F As Integer
        Sub S()
             ' Comment
        End Sub
    End Class
End Namespace
", HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync(" Comment", charsOffset: -2, HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync("F = 0", HangMitigatingCancellationToken);
        await TestServices.ErrorList.ShowErrorListAsync(HangMitigatingCancellationToken);
        var expectedContents = Array.Empty<string>();
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawlerLegacy, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles, FeatureAttribute.ErrorList], HangMitigatingCancellationToken);
        var actualContents = await TestServices.ErrorList.GetErrorsAsync(HangMitigatingCancellationToken);
        AssertEx.EqualOrDiff(
            string.Join(Environment.NewLine, expectedContents),
            string.Join(Environment.NewLine, actualContents));

        await TestServices.Editor.ActivateAsync(HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("F = 0 ' Comment", charsOffset: -1, HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync("F", HangMitigatingCancellationToken);
        await TestServices.ErrorList.ShowErrorListAsync(HangMitigatingCancellationToken);
        expectedContents = [
            "Class1.vb(6, 13): error BC30451: 'FF' is not declared. It may be inaccessible due to its protection level.",
        ];
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawlerLegacy, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles, FeatureAttribute.ErrorList], HangMitigatingCancellationToken);
        actualContents = await TestServices.ErrorList.GetErrorsAsync(HangMitigatingCancellationToken);
        AssertEx.EqualOrDiff(
            string.Join(Environment.NewLine, expectedContents),
            string.Join(Environment.NewLine, actualContents));

        await TestServices.Editor.ActivateAsync(HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("FF = 0 ' Comment", charsOffset: -1, HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync(VirtualKeyCode.DELETE, HangMitigatingCancellationToken);
        await TestServices.ErrorList.ShowErrorListAsync(HangMitigatingCancellationToken);
        expectedContents = [];
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawlerLegacy, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles, FeatureAttribute.ErrorList], HangMitigatingCancellationToken);
        actualContents = await TestServices.ErrorList.GetErrorsAsync(HangMitigatingCancellationToken);
        AssertEx.EqualOrDiff(
            string.Join(Environment.NewLine, expectedContents),
            string.Join(Environment.NewLine, actualContents));
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/72428"), WorkItem("https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1643350")]
    public virtual async Task BuildErrorsInClosedFiles()
    {
        // Enter code with compiler error.
        await TestServices.Editor.SetTextAsync(@"Class Class1
", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.SaveAllAsync(HangMitigatingCancellationToken);

        // Close active tab before build.
        await TestServices.SolutionExplorer.CloseActiveWindow(HangMitigatingCancellationToken);

        // Build and verify build failure in the output window.
        var succeed = await TestServices.SolutionExplorer.BuildSolutionAndWaitAsync(HangMitigatingCancellationToken);
        Assert.False(succeed);

        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawlerLegacy, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles, FeatureAttribute.ErrorList], HangMitigatingCancellationToken);

        // Verify the build error is listed in the error list for closed file.
        await TestServices.ErrorList.ShowBuildErrorsAsync(HangMitigatingCancellationToken);
        var expectedErrors = new[] {
            "Class1.vb(1, 1): error BC30481: 'Class' statement must end with a matching 'End Class'.",
        };

        while (true)
        {
            this.HangMitigatingCancellationToken.ThrowIfCancellationRequested();

            var actualErrors = await TestServices.ErrorList.GetBuildErrorsAsync(HangMitigatingCancellationToken);
            if (string.Join(Environment.NewLine, expectedErrors) == string.Join(Environment.NewLine, actualErrors))
                return;

            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }
}
