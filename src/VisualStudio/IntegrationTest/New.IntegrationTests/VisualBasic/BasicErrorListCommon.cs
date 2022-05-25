// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic
{
    public abstract class BasicErrorListCommon : AbstractEditorTest
    {
        protected BasicErrorListCommon(string templateName)
            : base(nameof(BasicErrorListCommon), templateName)
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        [IdeFact]
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
                "(Compiler) Class1.vb(4, 24): error BC30002: Type 'P' is not defined.",
                "(Compiler) Class1.vb(9, 9): error BC30451: 'Goo' is not declared. It may be inaccessible due to its protection level.",
            };
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(new[] { FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawler, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles, FeatureAttribute.ErrorList }, HangMitigatingCancellationToken);
            var actualContents = await TestServices.ErrorList.GetErrorsAsync(HangMitigatingCancellationToken);
            AssertEx.EqualOrDiff(
                string.Join(Environment.NewLine, expectedContents),
                string.Join(Environment.NewLine, actualContents));

            await TestServices.ErrorList.NavigateToErrorListItemAsync(0, isPreview: false, shouldActivate: true, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CaretPositionAsync(43, HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.BuildSolutionAsync(waitForBuildToFinish: true, HangMitigatingCancellationToken);
            await TestServices.ErrorList.ShowErrorListAsync(HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(new[] { FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawler, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles, FeatureAttribute.ErrorList }, HangMitigatingCancellationToken);
            actualContents = await TestServices.ErrorList.GetErrorsAsync(HangMitigatingCancellationToken);
            AssertEx.EqualOrDiff(
                string.Join(Environment.NewLine, expectedContents),
                string.Join(Environment.NewLine, actualContents));
        }

        [IdeFact]
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
            await TestServices.Input.SendAsync("F = 0");
            await TestServices.ErrorList.ShowErrorListAsync(HangMitigatingCancellationToken);
            var expectedContents = Array.Empty<string>();
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(new[] { FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawler, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles, FeatureAttribute.ErrorList }, HangMitigatingCancellationToken);
            var actualContents = await TestServices.ErrorList.GetErrorsAsync(HangMitigatingCancellationToken);
            AssertEx.EqualOrDiff(
                string.Join(Environment.NewLine, expectedContents),
                string.Join(Environment.NewLine, actualContents));

            await TestServices.Editor.ActivateAsync(HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("F = 0 ' Comment", charsOffset: -1, HangMitigatingCancellationToken);
            await TestServices.Input.SendAsync("F");
            await TestServices.ErrorList.ShowErrorListAsync(HangMitigatingCancellationToken);
            expectedContents = new[] {
                "(Compiler) Class1.vb(6, 13): error BC30451: 'FF' is not declared. It may be inaccessible due to its protection level.",
            };
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(new[] { FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawler, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles, FeatureAttribute.ErrorList }, HangMitigatingCancellationToken);
            actualContents = await TestServices.ErrorList.GetErrorsAsync(HangMitigatingCancellationToken);
            AssertEx.EqualOrDiff(
                string.Join(Environment.NewLine, expectedContents),
                string.Join(Environment.NewLine, actualContents));

            await TestServices.Editor.ActivateAsync(HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("FF = 0 ' Comment", charsOffset: -1, HangMitigatingCancellationToken);
            await TestServices.Input.SendAsync(VirtualKey.Delete);
            await TestServices.ErrorList.ShowErrorListAsync(HangMitigatingCancellationToken);
            expectedContents = Array.Empty<string>();
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(new[] { FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawler, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles, FeatureAttribute.ErrorList }, HangMitigatingCancellationToken);
            actualContents = await TestServices.ErrorList.GetErrorsAsync(HangMitigatingCancellationToken);
            AssertEx.EqualOrDiff(
                string.Join(Environment.NewLine, expectedContents),
                string.Join(Environment.NewLine, actualContents));
        }
    }
}
