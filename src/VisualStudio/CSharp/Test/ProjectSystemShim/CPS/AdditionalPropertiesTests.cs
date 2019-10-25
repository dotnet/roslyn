// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.CSharp.Utilities;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
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
        [InlineData(null)]
        public void SetProperty_MaxSupportedLangVersion_CPS(LanguageVersion? maxSupportedLangVersion)
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

                cpsProject.SetProperty(AdditionalPropertyNames.MaxSupportedLangVersion, maxSupportedLangVersion?.ToDisplayString());

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
        }

        [WpfFact]
        public void SetProperty_MaxSupportedLangVersion_CPS_NotSet()
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

                var canApply = environment.Workspace.CanApplyParseOptionChange(
                    oldParseOptions,
                    oldParseOptions.WithLanguageVersion(attemptedVersion),
                    project);

                Assert.True(canApply);
            }
        }
    }
}
