// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.VisualStudio.Shell.Interop;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.CodeAnalysis.Testing
{
    public class CreateProjectTests : AbstractIntegrationTest
    {
        private static readonly XUnitVerifier s_verifier = new XUnitVerifier();

        [IdeFact]
        public async Task CreateFromTemplateAsync()
        {
            await TestServices.SolutionExplorer.CreateSolutionAsync(nameof(CreateProjectTests), HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.AddProjectAsync("TestProj", WellKnownProjectTemplates.CSharpNetCoreClassLibrary, languageName: LanguageNames.CSharp, HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.RestoreNuGetPackagesAsync(HangMitigatingCancellationToken);

            await TestServices.Editor.SetTextAsync(@"using System", HangMitigatingCancellationToken);

            var buildSummary = await TestServices.SolutionExplorer.BuildSolutionAsync(waitForBuildToFinish: true, HangMitigatingCancellationToken);
            s_verifier.EqualOrDiff("========== Build: 0 succeeded, 1 failed, 0 up-to-date, 0 skipped ==========", buildSummary!);

            await TestServices.ErrorList.ShowBuildErrorsAsync(HangMitigatingCancellationToken);

            // Verify that intentional errors get validated by the test
            var errors = await TestServices.ErrorList.GetBuildErrorsAsync(__VSERRORCATEGORY.EC_ERROR, HangMitigatingCancellationToken);
            var expected = "(Compiler) Class1.cs(1, 13): error CS1002: ; expected";
            s_verifier.EqualOrDiff(expected, string.Join(Environment.NewLine, errors));
            Assert.Equal(1, await TestServices.ErrorList.GetErrorCountAsync(__VSERRORCATEGORY.EC_ERROR, HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task CreateAnalyzerFromCSharpTemplateAsync()
        {
            await TestServices.SolutionExplorer.CreateSolutionAsync(nameof(CreateProjectTests), HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.AddProjectAsync("TestProj", "Microsoft.CSharp.Analyzer", languageName: LanguageNames.CSharp, HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.RestoreNuGetPackagesAsync(HangMitigatingCancellationToken);

            var buildSummary = await TestServices.SolutionExplorer.BuildSolutionAsync(waitForBuildToFinish: true, HangMitigatingCancellationToken);
            s_verifier.EqualOrDiff("========== Build: 5 succeeded, 0 failed, 0 up-to-date, 0 skipped ==========", buildSummary!);

            await TestServices.ErrorList.ShowBuildErrorsAsync(HangMitigatingCancellationToken);

            var errors = await TestServices.ErrorList.GetBuildErrorsAsync(__VSERRORCATEGORY.EC_ERROR, HangMitigatingCancellationToken);
            s_verifier.EqualOrDiff(string.Empty, string.Join(Environment.NewLine, errors));
            Assert.Equal(0, await TestServices.ErrorList.GetErrorCountAsync(__VSERRORCATEGORY.EC_ERROR, HangMitigatingCancellationToken));

            // Currently have two analyzer warnings in the template.
            var warnings = await TestServices.ErrorList.GetBuildErrorsAsync(__VSERRORCATEGORY.EC_WARNING, HangMitigatingCancellationToken);
            s_verifier.EqualOrDiff(string.Empty, string.Join(Environment.NewLine, warnings));
            Assert.Equal(0, await TestServices.ErrorList.GetErrorCountAsync(__VSERRORCATEGORY.EC_WARNING, HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task CreateRefactoringFromCSharpTemplateAsync()
        {
            await TestServices.SolutionExplorer.CreateSolutionAsync(nameof(CreateProjectTests), HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.AddProjectAsync("TestProj", "Microsoft.CSharp.CodeRefactoring", languageName: LanguageNames.CSharp, HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.RestoreNuGetPackagesAsync(HangMitigatingCancellationToken);

            var buildSummary = await TestServices.SolutionExplorer.BuildSolutionAsync(waitForBuildToFinish: true, HangMitigatingCancellationToken);
            s_verifier.EqualOrDiff("========== Build: 2 succeeded, 0 failed, 0 up-to-date, 0 skipped ==========", buildSummary!);

            await TestServices.ErrorList.ShowBuildErrorsAsync(HangMitigatingCancellationToken);

            Assert.Equal(0, await TestServices.ErrorList.GetErrorCountAsync(__VSERRORCATEGORY.EC_ERROR, HangMitigatingCancellationToken));
            Assert.Equal(0, await TestServices.ErrorList.GetErrorCountAsync(__VSERRORCATEGORY.EC_WARNING, HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task CreateStandaloneToolFromCSharpTemplateAsync()
        {
            await TestServices.SolutionExplorer.CreateSolutionAsync(nameof(CreateProjectTests), HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.AddProjectAsync("TestProj", "Microsoft.CSharp.StandaloneCodeAnalysis", languageName: LanguageNames.CSharp, HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.RestoreNuGetPackagesAsync(HangMitigatingCancellationToken);

            var buildSummary = await TestServices.SolutionExplorer.BuildSolutionAsync(waitForBuildToFinish: true, HangMitigatingCancellationToken);
            s_verifier.EqualOrDiff("========== Build: 1 succeeded, 0 failed, 0 up-to-date, 0 skipped ==========", buildSummary!);

            await TestServices.ErrorList.ShowBuildErrorsAsync(HangMitigatingCancellationToken);

            Assert.Equal(0, await TestServices.ErrorList.GetErrorCountAsync(__VSERRORCATEGORY.EC_ERROR, HangMitigatingCancellationToken));
            Assert.Equal(0, await TestServices.ErrorList.GetErrorCountAsync(__VSERRORCATEGORY.EC_WARNING, HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task CreateAnalyzerFromVisualBasicTemplateAsync()
        {
            await TestServices.SolutionExplorer.CreateSolutionAsync(nameof(CreateProjectTests), HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.AddProjectAsync("TestProj", "Microsoft.VisualBasic.Analyzer", languageName: LanguageNames.VisualBasic, HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.RestoreNuGetPackagesAsync(HangMitigatingCancellationToken);

            var buildSummary = await TestServices.SolutionExplorer.BuildSolutionAsync(waitForBuildToFinish: true, HangMitigatingCancellationToken);
            s_verifier.EqualOrDiff("========== Build: 5 succeeded, 0 failed, 0 up-to-date, 0 skipped ==========", buildSummary!);

            await TestServices.ErrorList.ShowBuildErrorsAsync(HangMitigatingCancellationToken);

            var errors = await TestServices.ErrorList.GetBuildErrorsAsync(__VSERRORCATEGORY.EC_ERROR, HangMitigatingCancellationToken);
            s_verifier.EqualOrDiff(string.Empty, string.Join(Environment.NewLine, errors));
            Assert.Equal(0, await TestServices.ErrorList.GetErrorCountAsync(__VSERRORCATEGORY.EC_ERROR, HangMitigatingCancellationToken));

            // Currently have two analyzer warnings in the template.
            var warnings = await TestServices.ErrorList.GetBuildErrorsAsync(__VSERRORCATEGORY.EC_WARNING, HangMitigatingCancellationToken);
            s_verifier.EqualOrDiff(string.Empty, string.Join(Environment.NewLine, warnings));
            Assert.Equal(0, await TestServices.ErrorList.GetErrorCountAsync(__VSERRORCATEGORY.EC_WARNING, HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task CreateRefactoringFromVisualBasicTemplateAsync()
        {
            await TestServices.SolutionExplorer.CreateSolutionAsync(nameof(CreateProjectTests), HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.AddProjectAsync("TestProj", "Microsoft.VisualBasic.CodeRefactoring", languageName: LanguageNames.VisualBasic, HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.RestoreNuGetPackagesAsync(HangMitigatingCancellationToken);

            var buildSummary = await TestServices.SolutionExplorer.BuildSolutionAsync(waitForBuildToFinish: true, HangMitigatingCancellationToken);
            s_verifier.EqualOrDiff("========== Build: 2 succeeded, 0 failed, 0 up-to-date, 0 skipped ==========", buildSummary!);

            await TestServices.ErrorList.ShowBuildErrorsAsync(HangMitigatingCancellationToken);

            Assert.Equal(0, await TestServices.ErrorList.GetErrorCountAsync(__VSERRORCATEGORY.EC_ERROR, HangMitigatingCancellationToken));
            Assert.Equal(0, await TestServices.ErrorList.GetErrorCountAsync(__VSERRORCATEGORY.EC_WARNING, HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task CreateStandaloneToolFromVisualBasicTemplateAsync()
        {
            await TestServices.SolutionExplorer.CreateSolutionAsync(nameof(CreateProjectTests), HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.AddProjectAsync("TestProj", "Microsoft.VisualBasic.StandaloneCodeAnalysis", languageName: LanguageNames.VisualBasic, HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.RestoreNuGetPackagesAsync(HangMitigatingCancellationToken);

            var buildSummary = await TestServices.SolutionExplorer.BuildSolutionAsync(waitForBuildToFinish: true, HangMitigatingCancellationToken);
            s_verifier.EqualOrDiff("========== Build: 1 succeeded, 0 failed, 0 up-to-date, 0 skipped ==========", buildSummary!);

            await TestServices.ErrorList.ShowBuildErrorsAsync(HangMitigatingCancellationToken);

            Assert.Equal(0, await TestServices.ErrorList.GetErrorCountAsync(__VSERRORCATEGORY.EC_ERROR, HangMitigatingCancellationToken));
            Assert.Equal(0, await TestServices.ErrorList.GetErrorCountAsync(__VSERRORCATEGORY.EC_WARNING, HangMitigatingCancellationToken));
        }
    }
}
