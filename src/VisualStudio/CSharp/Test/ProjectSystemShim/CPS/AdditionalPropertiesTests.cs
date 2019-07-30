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

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void SetProperty_MaxSupportedLangVersion_CPS()
        {
            var catalog = TestEnvironment.s_exportCatalog.Value
                .WithParts(
                    typeof(CSharpParseOptionsChangingService));

            var factory = ExportProviderCache.GetOrCreateExportProviderFactory(catalog);

            using (var environment = new TestEnvironment(exportProviderFactory: factory))
            using (var project = CSharpHelpers.CreateCSharpCPSProject(environment, "Test"))
            {
                Assert.True(CanApplyVersionChange(environment, LanguageVersion.CSharp8));

                // Test max version is less than attempted version
                var maxSupportedLangVersion = LanguageVersion.CSharp7_3;
                project.SetProperty(AdditionalPropertyNames.MaxSupportedLangVersion, maxSupportedLangVersion.ToDisplayString());
                Assert.False(CanApplyVersionChange(environment, LanguageVersion.CSharp8));

                // Test max version equals attempted version
                maxSupportedLangVersion = LanguageVersion.CSharp8;
                project.SetProperty(AdditionalPropertyNames.MaxSupportedLangVersion, maxSupportedLangVersion.ToDisplayString());
                Assert.True(CanApplyVersionChange(environment, LanguageVersion.CSharp8));

                // Test max version is greater than attempted version
                maxSupportedLangVersion = LanguageVersion.Preview;
                project.SetProperty(AdditionalPropertyNames.MaxSupportedLangVersion, maxSupportedLangVersion.ToDisplayString());
                Assert.True(CanApplyVersionChange(environment, LanguageVersion.CSharp8));

            }

            static bool CanApplyVersionChange(TestEnvironment environment, LanguageVersion newVersion)
            {
                var project = environment.Workspace.CurrentSolution.Projects.Single();
                var oldParseOptions = (CSharpParseOptions)project.ParseOptions;

                return environment.Workspace.CanApplyParseOptionChange(
                    oldParseOptions,
                    oldParseOptions.WithLanguageVersion(newVersion),
                    project);
            }
        }
    }
}
