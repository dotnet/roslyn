// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.IntegrationTests.InProcess;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    public class CSharpInlineDiagnostics : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpInlineDiagnostics()
            : base(nameof(CSharpInlineDiagnostics))
        {
        }

        [IdeFact]
        public async Task TestInlineDiagnosticsUnusedVariable()
        {
            await TestServices.InlineDiagnostics.EnableOptionsAsync(LanguageName, cancellationToken: HangMitigatingCancellationToken);
            await SetUpEditorAsync(@"
class Program
{
    public void Method()
    {
        int x = 0;$$
    }
}
", HangMitigatingCancellationToken);

            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(new[] { FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawlerLegacy, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles, FeatureAttribute.ErrorList, FeatureAttribute.InlineDiagnostics }, HangMitigatingCancellationToken);

            var (count, tags) = await TestServices.InlineDiagnostics.EnsureInlineDiagnosticsCountAndLocation(HangMitigatingCancellationToken);
            Assert.Equal(expected: 1, actual: count);
            Assert.Equal(expected: "compiler warning", actual: tags[0].ErrorType);

            await TestServices.Editor.DeleteTextAsync("int x = 0;", HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(new[] { FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawlerLegacy, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles, FeatureAttribute.ErrorList, FeatureAttribute.InlineDiagnostics }, HangMitigatingCancellationToken);
            var (countAfterDeletion, _) = await TestServices.InlineDiagnostics.EnsureInlineDiagnosticsCountAndLocation(HangMitigatingCancellationToken);
            Assert.Equal(expected: 0, actual: countAfterDeletion);
        }

        [IdeFact]
        public async Task TestInlineDiagnosticsMultipleBlankLines()
        {
            await TestServices.InlineDiagnostics.EnableOptionsAsync(LanguageName, cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.AddFileAsync(ProjectName, ".editorconfig",
                contents: @"
[*.cs]

dotnet_style_allow_multiple_blank_lines_experimental = false:warning",
                cancellationToken: HangMitigatingCancellationToken);

            await SetUpEditorAsync(@"
class Program
{
    public void Method()
    {
        
$$
    }
}
", HangMitigatingCancellationToken);

            Thread.Sleep(5000);
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(new[] { FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawlerLegacy, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles, FeatureAttribute.ErrorList, FeatureAttribute.InlineDiagnostics }, HangMitigatingCancellationToken);
            Thread.Sleep(5000);
            var (count, tags) = await TestServices.InlineDiagnostics.EnsureInlineDiagnosticsCountAndLocation(HangMitigatingCancellationToken);
            Assert.Equal(expected: 1, actual: count);
            Assert.Equal(expected: "compiler warning", actual: tags[0].ErrorType);

            await TestServices.Input.SendAsync(VirtualKeyCode.BACK, HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(new[] { FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawlerLegacy, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles, FeatureAttribute.ErrorList, FeatureAttribute.InlineDiagnostics }, HangMitigatingCancellationToken);
            var (countAfterDeletion, _) = await TestServices.InlineDiagnostics.EnsureInlineDiagnosticsCountAndLocation(HangMitigatingCancellationToken);
            Assert.Equal(expected: 0, actual: countAfterDeletion);
        }
    }
}
