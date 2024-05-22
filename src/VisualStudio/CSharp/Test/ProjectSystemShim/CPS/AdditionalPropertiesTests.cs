// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices.CSharp.Utilities;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim.CPS
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
    public class AdditionalPropertiesTests
    {
        [WpfFact]
        public async Task SetProperty_RootNamespace_CPS()
        {
            using (var environment = new TestEnvironment())
            using (var project = await CSharpHelpers.CreateCSharpCPSProjectAsync(environment, "Test"))
            {
                Assert.Null(DefaultNamespaceOfSingleProject(environment));

                var rootNamespace = "Foo.Bar";
                project.SetProperty(BuildPropertyNames.RootNamespace, rootNamespace);
                Assert.Equal(rootNamespace, DefaultNamespaceOfSingleProject(environment));
            }

            static string DefaultNamespaceOfSingleProject(TestEnvironment environment)
                => environment.Workspace.CurrentSolution.Projects.Single().DefaultNamespace;
        }

        [WpfTheory]
        [InlineData(LanguageVersion.CSharp7_3)]
        [InlineData(LanguageVersion.CSharp8)]
        [InlineData(LanguageVersion.CSharp9)]
        [InlineData(LanguageVersion.Latest)]
        [InlineData(LanguageVersion.LatestMajor)]
        [InlineData(LanguageVersion.Preview)]
        [InlineData(null)]
        public async Task SetProperty_MaxSupportedLangVersion_CPS(LanguageVersion? maxSupportedLangVersion)
        {
            const LanguageVersion attemptedVersion = LanguageVersion.CSharp8;

            using var environment = new TestEnvironment(typeof(CSharpParseOptionsChangingService));
            using var cpsProject = await CSharpHelpers.CreateCSharpCPSProjectAsync(environment, "Test");
            var project = environment.Workspace.CurrentSolution.Projects.Single();
            var oldParseOptions = (CSharpParseOptions)project.ParseOptions;

            cpsProject.SetProperty(BuildPropertyNames.MaxSupportedLangVersion, maxSupportedLangVersion?.ToDisplayString());

            var canApply = environment.Workspace.CanApplyParseOptionChange(
                oldParseOptions,
                oldParseOptions.WithLanguageVersion(attemptedVersion),
                project);

            if (maxSupportedLangVersion.HasValue)
            {
                Assert.Equal(attemptedVersion <= maxSupportedLangVersion.Value, canApply);
            }
            else
            {
                Assert.True(canApply);
            }
        }

        [WpfFact]
        public async Task SetProperty_MaxSupportedLangVersion_CPS_NotSet()
        {
            const LanguageVersion attemptedVersion = LanguageVersion.CSharp8;

            using var environment = new TestEnvironment(typeof(CSharpParseOptionsChangingService));
            using var cpsProject = await CSharpHelpers.CreateCSharpCPSProjectAsync(environment, "Test");
            var project = environment.Workspace.CurrentSolution.Projects.Single();
            var oldParseOptions = (CSharpParseOptions)project.ParseOptions;

            var canApply = environment.Workspace.CanApplyParseOptionChange(
                oldParseOptions,
                oldParseOptions.WithLanguageVersion(attemptedVersion),
                project);

            Assert.True(canApply);
        }

        [WpfTheory]
        // RunAnalyzers: Not set, RunAnalyzersDuringLiveAnalysis: Not set, ExpectedRunAnalyzers = true
        [InlineData("", "", true)]
        // RunAnalyzers: true, RunAnalyzersDuringLiveAnalysis: Not set, ExpectedRunAnalyzers = true
        [InlineData("true", "", true)]
        // RunAnalyzers: false, RunAnalyzersDuringLiveAnalysis: Not set, ExpectedRunAnalyzers = false
        [InlineData("false", "", false)]
        // RunAnalyzers: Not set, RunAnalyzersDuringLiveAnalysis: true, ExpectedRunAnalyzers = true
        [InlineData("", "true", true)]
        // RunAnalyzers: Not set, RunAnalyzersDuringLiveAnalysis: false, ExpectedRunAnalyzers = false
        [InlineData("", "false", false)]
        // RunAnalyzers: true, RunAnalyzersDuringLiveAnalysis: true, ExpectedRunAnalyzers = true
        [InlineData("true", "true", true)]
        // RunAnalyzers: true, RunAnalyzersDuringLiveAnalysis: false, ExpectedRunAnalyzers = true
        [InlineData("true", "false", true)]
        // RunAnalyzers: false, RunAnalyzersDuringLiveAnalysis: true, ExpectedRunAnalyzers = false
        [InlineData("false", "true", false)]
        // RunAnalyzers: false, RunAnalyzersDuringLiveAnalysis: false, ExpectedRunAnalyzers = false
        [InlineData("false", "false", false)]
        // Case insensitive
        [InlineData("FALSE", "", false)]
        // Invalid values ignored
        [InlineData("Invalid", "INVALID", true)]
        public async Task SetProperty_RunAnalyzersAndRunAnalyzersDuringLiveAnalysis(string runAnalyzers, string runAnalyzersDuringLiveAnalysis, bool expectedRunAnalyzers)
        {
            await TestCPSProject();
            TestLegacyProject();
            return;

            async Task TestCPSProject()
            {
                using var environment = new TestEnvironment();
                using var cpsProject = await CSharpHelpers.CreateCSharpCPSProjectAsync(environment, "Test");

                cpsProject.SetProperty(BuildPropertyNames.RunAnalyzers, runAnalyzers);
                cpsProject.SetProperty(BuildPropertyNames.RunAnalyzersDuringLiveAnalysis, runAnalyzersDuringLiveAnalysis);

                Assert.Equal(expectedRunAnalyzers, environment.Workspace.CurrentSolution.Projects.Single().State.RunAnalyzers);
            }

            void TestLegacyProject()
            {
                using var environment = new TestEnvironment();

                var hierarchy = environment.CreateHierarchy("CSharpProject", "Bin", projectRefPath: null, projectCapabilities: "CSharp");
                var storage = Assert.IsAssignableFrom<IVsBuildPropertyStorage>(hierarchy);

                Assert.True(ErrorHandler.Succeeded(
                    storage.SetPropertyValue(
                        BuildPropertyNames.RunAnalyzers, null, (uint)_PersistStorageType.PST_PROJECT_FILE, runAnalyzers)));

                Assert.True(ErrorHandler.Succeeded(
                    storage.SetPropertyValue(
                        BuildPropertyNames.RunAnalyzersDuringLiveAnalysis, null, (uint)_PersistStorageType.PST_PROJECT_FILE, runAnalyzersDuringLiveAnalysis)));

                _ = CSharpHelpers.CreateCSharpProject(environment, "Test", hierarchy);

                Assert.Equal(expectedRunAnalyzers, environment.Workspace.CurrentSolution.Projects.Single().State.RunAnalyzers);
            }
        }
    }
}
