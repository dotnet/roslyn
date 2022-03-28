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

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    public abstract class CSharpErrorListCommon : AbstractEditorTest
    {
        protected CSharpErrorListCommon(string templateName)
            : base(nameof(CSharpErrorListCommon), templateName)
        {
        }

        protected override string LanguageName => LanguageNames.CSharp;

        [IdeFact]
        public virtual async Task ErrorList()
        {
            await TestServices.Editor.SetTextAsync(@"
class C
{
    void M(P p)
    {
        System.Console.WriteLin();
    }

    static void Main(string[] args)
    {
    }
}
", HangMitigatingCancellationToken);
            await TestServices.ErrorList.ShowErrorListAsync(HangMitigatingCancellationToken);
            var expectedContents = new[] {
                "(Compiler) Class1.cs(4, 12): error CS0246: The type or namespace name 'P' could not be found (are you missing a using directive or an assembly reference?)",
                "(Compiler) Class1.cs(6, 24): error CS0117: 'Console' does not contain a definition for 'WriteLin'",
            };
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(new[] { FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawler, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles, FeatureAttribute.ErrorList }, HangMitigatingCancellationToken);
            var actualContents = await TestServices.ErrorList.GetErrorsAsync(HangMitigatingCancellationToken);
            AssertEx.EqualOrDiff(
                string.Join(Environment.NewLine, expectedContents),
                string.Join(Environment.NewLine, actualContents));

            var target = await TestServices.ErrorList.NavigateToErrorListItemAsync(0, isPreview: false, shouldActivate: true, HangMitigatingCancellationToken);
            Assert.Equal(expectedContents[0], target);
            Assert.Equal(25, await TestServices.Editor.GetCaretPositionAsync(HangMitigatingCancellationToken));
            await TestServices.SolutionExplorer.BuildSolutionAsync(waitForBuildToFinish: true, HangMitigatingCancellationToken);
            await TestServices.ErrorList.ShowErrorListAsync(HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(new[] { FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawler, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles, FeatureAttribute.ErrorList }, HangMitigatingCancellationToken);
            actualContents = await TestServices.ErrorList.GetErrorsAsync(HangMitigatingCancellationToken);
            AssertEx.EqualOrDiff(
                string.Join(Environment.NewLine, expectedContents),
                string.Join(Environment.NewLine, actualContents));
        }

        [IdeFact]
        public virtual async Task ErrorLevelWarning()
        {
            await TestServices.Editor.SetTextAsync(@"
class C
{
    static void Main(string[] args)
    {
        int unused = 0;
    }
}
", HangMitigatingCancellationToken);
            await TestServices.ErrorList.ShowErrorListAsync(HangMitigatingCancellationToken);
            var expectedContents = new[] {
                "(Compiler) Class1.cs(6, 13): warning CS0219: The variable 'unused' is assigned but its value is never used",
            };
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(new[] { FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawler, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles, FeatureAttribute.ErrorList }, HangMitigatingCancellationToken);
            var actualContents = await TestServices.ErrorList.GetErrorsAsync(HangMitigatingCancellationToken);
            AssertEx.EqualOrDiff(
                string.Join(Environment.NewLine, expectedContents),
                string.Join(Environment.NewLine, actualContents));
        }

        [IdeFact]
        public virtual async Task ErrorsDuringMethodBodyEditing()
        {
            await TestServices.Editor.SetTextAsync(@"
class Program2
{
    static void Main(string[] args)
    {
        int aa = 7;
        int a = aa;
    }
}
", HangMitigatingCancellationToken);
            await TestServices.ErrorList.ShowErrorListAsync(HangMitigatingCancellationToken);
            var expectedContents = new string[] { };
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(new[] { FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawler, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles, FeatureAttribute.ErrorList }, HangMitigatingCancellationToken);
            var actualContents = await TestServices.ErrorList.GetErrorsAsync(HangMitigatingCancellationToken);
            AssertEx.EqualOrDiff(
                string.Join(Environment.NewLine, expectedContents),
                string.Join(Environment.NewLine, actualContents));

            await TestServices.Editor.ActivateAsync(HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("a = aa", charsOffset: -1, HangMitigatingCancellationToken);
            await TestServices.Input.SendAsync("a");
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(new[] { FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawler, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles, FeatureAttribute.ErrorList }, HangMitigatingCancellationToken);
            await TestServices.ErrorList.ShowErrorListAsync(HangMitigatingCancellationToken);
            expectedContents =
                new[]
                {
                    "(Compiler) Class1.cs(7, 13): error CS0128: A local variable or function named 'aa' is already defined in this scope",
                };
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(new[] { FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawler, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles, FeatureAttribute.ErrorList }, HangMitigatingCancellationToken);
            actualContents = await TestServices.ErrorList.GetErrorsAsync(HangMitigatingCancellationToken);
            AssertEx.EqualOrDiff(
                string.Join(Environment.NewLine, expectedContents),
                string.Join(Environment.NewLine, actualContents));

            await TestServices.Editor.ActivateAsync(HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("aa = aa", charsOffset: -1, HangMitigatingCancellationToken);
            await TestServices.Input.SendAsync(VirtualKey.Delete);
            await TestServices.ErrorList.ShowErrorListAsync(HangMitigatingCancellationToken);
            expectedContents = new string[] { };
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(new[] { FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawler, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles, FeatureAttribute.ErrorList }, HangMitigatingCancellationToken);
            actualContents = await TestServices.ErrorList.GetErrorsAsync(HangMitigatingCancellationToken);
            AssertEx.EqualOrDiff(
                string.Join(Environment.NewLine, expectedContents),
                string.Join(Environment.NewLine, actualContents));
        }

        [IdeFact]
        public virtual async Task ErrorsAfterClosingFile()
        {
            await TestServices.Editor.SetTextAsync(@"
class Program2
{
    static void Main(string[] args)
    {
        int aa = 7;
        int a = aa;
    }
}
", HangMitigatingCancellationToken);
            await TestServices.ErrorList.ShowErrorListAsync(HangMitigatingCancellationToken);
            var expectedContents = new string[] { };
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(new[] { FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawler, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles, FeatureAttribute.ErrorList }, HangMitigatingCancellationToken);
            var actualContents = await TestServices.ErrorList.GetErrorsAsync(HangMitigatingCancellationToken);
            AssertEx.EqualOrDiff(
                string.Join<string>(Environment.NewLine, expectedContents),
                string.Join<string>(Environment.NewLine, actualContents));

            await TestServices.Editor.ActivateAsync(HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("a = aa", charsOffset: -1, HangMitigatingCancellationToken);
            await TestServices.Input.SendAsync("a");
            await TestServices.ErrorList.ShowErrorListAsync(HangMitigatingCancellationToken);
            expectedContents = new[] {
                "(Compiler) Class1.cs(7, 13): error CS0128: A local variable or function named 'aa' is already defined in this scope",
            };
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(new[] { FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawler, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles, FeatureAttribute.ErrorList }, HangMitigatingCancellationToken);
            actualContents = await TestServices.ErrorList.GetErrorsAsync(HangMitigatingCancellationToken);
            AssertEx.EqualOrDiff(
                string.Join<string>(Environment.NewLine, expectedContents),
                string.Join<string>(Environment.NewLine, actualContents));

            // Close the current document and verify diagnostics for closed document are not removed from error list.
            await TestServices.SolutionExplorer.SaveAllAsync(HangMitigatingCancellationToken);
            await TestServices.Editor.SendExplicitFocusAsync(HangMitigatingCancellationToken);

            // Assert the window title is Class1.cs, which also means the file has no unsaved changes
            Assert.Equal("Class1.cs", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));

            await TestServices.Input.SendAsync(new KeyPress(VirtualKey.F4, ShiftState.Ctrl));
            await TestServices.ErrorList.ShowErrorListAsync(HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(new[] { FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawler, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles, FeatureAttribute.ErrorList }, HangMitigatingCancellationToken);
            actualContents = await TestServices.ErrorList.GetErrorsAsync(HangMitigatingCancellationToken);
            AssertEx.EqualOrDiff(
                string.Join<string>(Environment.NewLine, expectedContents),
                string.Join<string>(Environment.NewLine, actualContents));
        }
    }
}
