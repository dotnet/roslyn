// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests;
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
    public class AdditionalPropertiesTests
    {
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void SetProperty_RootNamespace_CPS()
        {
            using (var environment = new TestEnvironment())
            using (var project = CSharpHelpers.CreateCSharpCPSProject(environment, "Test"))
            {
                Assert.Null(DefaultNamespaceOfSingleProject(environment));

                var rootNamespace = "Foo.Bar";
                project.SetProperty(AdditionalPropertyNames.RootNamespace, rootNamespace);
                Assert.Equal(rootNamespace, DefaultNamespaceOfSingleProject(environment));
            }

            static string DefaultNamespaceOfSingleProject(TestEnvironment environment)
                => environment.Workspace.CurrentSolution.Projects.Single().DefaultNamespace;
        }

        [WpfTheory]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        [InlineData(LanguageVersion.CSharp7_3)]
        [InlineData(LanguageVersion.CSharp8)]
        [InlineData(LanguageVersion.Latest)]
        [InlineData(LanguageVersion.LatestMajor)]
        [InlineData(LanguageVersion.Preview)]
        public void SetProperty_MaxSupportedLangVersion_CPS(LanguageVersion maxSupportedLangVersion)
        {
            var catalog = TestEnvironment.s_exportCatalog.Value
                .WithParts(
                    typeof(CSharpParseOptionsChangingService));

            const LanguageVersion attemptedVersion = LanguageVersion.CSharp8;

            var factory = ExportProviderCache.GetOrCreateExportProviderFactory(catalog);

            using (var environment = new TestEnvironment(exportProviderFactory: factory))
            using (var cpsProject = CSharpHelpers.CreateCSharpCPSProject(environment, "Test"))
            {
                var project = environment.Workspace.CurrentSolution.Projects.Single();
                var oldParseOptions = (CSharpParseOptions)project.ParseOptions;

                cpsProject.SetProperty(AdditionalPropertyNames.MaxSupportedLangVersion, maxSupportedLangVersion.ToDisplayString());

                var canApply = environment.Workspace.CanApplyParseOptionChange(
                    oldParseOptions,
                    oldParseOptions.WithLanguageVersion(attemptedVersion),
                    project);

                Assert.Equal(attemptedVersion <= maxSupportedLangVersion, canApply);
            }
        }

        [WpfTheory]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
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
        public void SetProperty_RunAnalyzersAndRunAnalyzersDuringLiveAnalysis(string runAnalyzers, string runAnalyzersDuringLiveAnalysis, bool expectedRunAnalyzers)
        {
            TestCPSProject();
            TestLegacyProject();
            return;

            void TestCPSProject()
            {
                using var environment = new TestEnvironment();
                using var cpsProject = CSharpHelpers.CreateCSharpCPSProject(environment, "Test");

                cpsProject.SetProperty(AdditionalPropertyNames.RunAnalyzers, runAnalyzers);
                cpsProject.SetProperty(AdditionalPropertyNames.RunAnalyzersDuringLiveAnalysis, runAnalyzersDuringLiveAnalysis);

                Assert.Equal(expectedRunAnalyzers, environment.Workspace.CurrentSolution.Projects.Single().State.RunAnalyzers);
            }

            void TestLegacyProject()
            {
                using var environment = new TestEnvironment();

                var hierarchy = environment.CreateHierarchy("CSharpProject", "Bin", projectRefPath: null, projectCapabilities: "CSharp");
                var storage = Assert.IsAssignableFrom<IVsBuildPropertyStorage>(hierarchy);

                Assert.True(ErrorHandler.Succeeded(
                    storage.SetPropertyValue(
                        AdditionalPropertyNames.RunAnalyzers, null, (uint)_PersistStorageType.PST_PROJECT_FILE, runAnalyzers)));

                Assert.True(ErrorHandler.Succeeded(
                    storage.SetPropertyValue(
                        AdditionalPropertyNames.RunAnalyzersDuringLiveAnalysis, null, (uint)_PersistStorageType.PST_PROJECT_FILE, runAnalyzersDuringLiveAnalysis)));

                _ = CSharpHelpers.CreateCSharpProject(environment, "Test", hierarchy);

                Assert.Equal(expectedRunAnalyzers, environment.Workspace.CurrentSolution.Projects.Single().State.RunAnalyzers);
            }
        }
    }
}
