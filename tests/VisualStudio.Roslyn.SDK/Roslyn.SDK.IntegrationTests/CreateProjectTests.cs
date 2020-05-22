// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.CodeAnalysis.Testing
{
    public class CreateProjectTests : AbstractIdeIntegrationTest
    {
        public override async Task DisposeAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            await TestServices.SolutionExplorer.CloseSolutionAsync();

            await base.DisposeAsync();
        }

        [VsFact]
        public async Task CreateFromTemplateAsync()
        {
            await TestServices.SolutionExplorer.CreateSolutionAsync(nameof(CreateProjectTests));
            await TestServices.SolutionExplorer.AddProjectAsync("TestProj", WellKnownProjectTemplates.ClassLibrary, languageName: LanguageNames.CSharp);
            await TestServices.SolutionExplorer.RestoreNuGetPackagesAsync(HangMitigatingCancellationToken);

            await TestServices.Editor.SetTextAsync(@"using System");

            await TestServices.SolutionExplorer.BuildSolutionAsync(waitForBuildToFinish: true);

            await Task.Delay(TimeSpan.FromSeconds(5));

            // Verify that intentional errors get validated by the test
            Assert.Equal(1, await TestServices.ErrorList.GetErrorCountAsync(__VSERRORCATEGORY.EC_ERROR));
        }

        [VsFact]
        public async Task CreateAnalyzerFromCSharpTemplateAsync()
        {
            await TestServices.SolutionExplorer.CreateSolutionAsync(nameof(CreateProjectTests));
            await TestServices.SolutionExplorer.AddProjectAsync("TestProj", "Microsoft.CSharp.Analyzer", languageName: LanguageNames.CSharp);
            await TestServices.SolutionExplorer.RestoreNuGetPackagesAsync(HangMitigatingCancellationToken);

            await TestServices.SolutionExplorer.BuildSolutionAsync(waitForBuildToFinish: true);

            await Task.Delay(TimeSpan.FromSeconds(5));

            Assert.Equal(0, await TestServices.ErrorList.GetErrorCountAsync(__VSERRORCATEGORY.EC_ERROR));

            // Currently have two analyzer warnings in the template.
            Assert.Equal(2, await TestServices.ErrorList.GetErrorCountAsync(__VSERRORCATEGORY.EC_WARNING));
        }

        [VsFact]
        public async Task CreateRefactoringFromCSharpTemplateAsync()
        {
            await TestServices.SolutionExplorer.CreateSolutionAsync(nameof(CreateProjectTests));
            await TestServices.SolutionExplorer.AddProjectAsync("TestProj", "Microsoft.CSharp.CodeRefactoring", languageName: LanguageNames.CSharp);
            await TestServices.SolutionExplorer.RestoreNuGetPackagesAsync(HangMitigatingCancellationToken);

            await TestServices.SolutionExplorer.BuildSolutionAsync(waitForBuildToFinish: true);

            await Task.Delay(TimeSpan.FromSeconds(5));

            Assert.Equal(0, await TestServices.ErrorList.GetErrorCountAsync(__VSERRORCATEGORY.EC_ERROR));
            Assert.Equal(0, await TestServices.ErrorList.GetErrorCountAsync(__VSERRORCATEGORY.EC_WARNING));
        }

        [VsFact]
        public async Task CreateStandaloneToolFromCSharpTemplateAsync()
        {
            await TestServices.SolutionExplorer.CreateSolutionAsync(nameof(CreateProjectTests));
            await TestServices.SolutionExplorer.AddProjectAsync("TestProj", "Microsoft.CSharp.StandaloneCodeAnalysis", languageName: LanguageNames.CSharp);
            await TestServices.SolutionExplorer.RestoreNuGetPackagesAsync(HangMitigatingCancellationToken);

            await TestServices.SolutionExplorer.BuildSolutionAsync(waitForBuildToFinish: true);

            await Task.Delay(TimeSpan.FromSeconds(5));

            Assert.Equal(0, await TestServices.ErrorList.GetErrorCountAsync(__VSERRORCATEGORY.EC_ERROR));
            Assert.Equal(0, await TestServices.ErrorList.GetErrorCountAsync(__VSERRORCATEGORY.EC_WARNING));
        }

        [VsFact]
        public async Task CreateAnalyzerFromVisualBasicTemplateAsync()
        {
            await TestServices.SolutionExplorer.CreateSolutionAsync(nameof(CreateProjectTests));
            await TestServices.SolutionExplorer.AddProjectAsync("TestProj", "Microsoft.VisualBasic.Analyzer", languageName: LanguageNames.VisualBasic);
            await TestServices.SolutionExplorer.RestoreNuGetPackagesAsync(HangMitigatingCancellationToken);

            await TestServices.SolutionExplorer.BuildSolutionAsync(waitForBuildToFinish: true);

            await Task.Delay(TimeSpan.FromSeconds(5));

            Assert.Equal(0, await TestServices.ErrorList.GetErrorCountAsync(__VSERRORCATEGORY.EC_ERROR));

            // Currently have two analyzer warnings in the template.
            Assert.Equal(2, await TestServices.ErrorList.GetErrorCountAsync(__VSERRORCATEGORY.EC_WARNING));
        }

        [VsFact]
        public async Task CreateRefactoringFromVisualBasicTemplateAsync()
        {
            await TestServices.SolutionExplorer.CreateSolutionAsync(nameof(CreateProjectTests));
            await TestServices.SolutionExplorer.AddProjectAsync("TestProj", "Microsoft.VisualBasic.CodeRefactoring", languageName: LanguageNames.VisualBasic);
            await TestServices.SolutionExplorer.RestoreNuGetPackagesAsync(HangMitigatingCancellationToken);

            await TestServices.SolutionExplorer.BuildSolutionAsync(waitForBuildToFinish: true);

            await Task.Delay(TimeSpan.FromSeconds(5));

            Assert.Equal(0, await TestServices.ErrorList.GetErrorCountAsync(__VSERRORCATEGORY.EC_ERROR));
            Assert.Equal(0, await TestServices.ErrorList.GetErrorCountAsync(__VSERRORCATEGORY.EC_WARNING));
        }

        [VsFact]
        public async Task CreateStandaloneToolFromVisualBasicTemplateAsync()
        {
            await TestServices.SolutionExplorer.CreateSolutionAsync(nameof(CreateProjectTests));
            await TestServices.SolutionExplorer.AddProjectAsync("TestProj", "Microsoft.VisualBasic.StandaloneCodeAnalysis", languageName: LanguageNames.VisualBasic);
            await TestServices.SolutionExplorer.RestoreNuGetPackagesAsync(HangMitigatingCancellationToken);

            await TestServices.SolutionExplorer.BuildSolutionAsync(waitForBuildToFinish: true);

            await Task.Delay(TimeSpan.FromSeconds(5));

            Assert.Equal(0, await TestServices.ErrorList.GetErrorCountAsync(__VSERRORCATEGORY.EC_ERROR));
            Assert.Equal(0, await TestServices.ErrorList.GetErrorCountAsync(__VSERRORCATEGORY.EC_WARNING));
        }
    }
}
