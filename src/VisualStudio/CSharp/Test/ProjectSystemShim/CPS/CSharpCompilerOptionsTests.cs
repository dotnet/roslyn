// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim.CPS
{
    public class CSharpCompilerOptionsTests : TestBase
    {
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void DocumentationModeSetToDiagnoseIfProducingDocFile_CPS()
        {
            using (var environment = new TestEnvironment())
            using (var project = CSharpHelpers.CreateCSharpCPSProject(environment, "Test", commandLineArguments: @"/doc:DocFile.xml"))
            {
                Assert.Equal(DocumentationMode.Diagnose, project.CurrentParseOptions.DocumentationMode);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void DocumentationModeSetToParseIfNotProducingDocFile_CPS()
        {
            using (var environment = new TestEnvironment())
            using (var project = CSharpHelpers.CreateCSharpCPSProject(environment, "Test", commandLineArguments: @"/doc:"))
            {
                Assert.Equal(DocumentationMode.Parse, project.CurrentParseOptions.DocumentationMode);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void ProjectSettingsOptionAddAndRemove_CPS()
        {
            using (var environment = new TestEnvironment())
            using (var project = CSharpHelpers.CreateCSharpCPSProject(environment, "Test", commandLineArguments: @"/warnaserror:CS1111"))
            {
                var options = environment.GetUpdatedCompilationOptionOfSingleProject();
                Assert.Equal(expected: ReportDiagnostic.Error, actual: options.SpecificDiagnosticOptions["CS1111"]);

                project.SetCommandLineArguments(@"/warnaserror");
                options = environment.GetUpdatedCompilationOptionOfSingleProject();
                Assert.False(options.SpecificDiagnosticOptions.ContainsKey("CS1111"));
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void ProjectOutputBinPathChange_CPS()
        {
            var initialBinPath = @"C:\test.dll";

            using (var environment = new TestEnvironment())
            using (var project = CSharpHelpers.CreateCSharpCPSProject(environment, "Test", $"/out:{initialBinPath}"))
            {
                Assert.Equal(initialBinPath, project.TryGetBinOutputPath());
                Assert.Equal(initialBinPath, project.TryGetObjOutputPath());

                // Change output folder.
                var newBinPath = @"C:\NewFolder\test.dll";
                project.SetCommandLineArguments($"/out:{newBinPath}");
                Assert.Equal(newBinPath, project.TryGetBinOutputPath());
                Assert.Equal(newBinPath, project.TryGetObjOutputPath());

                // Change output file name.
                newBinPath = @"C:\NewFolder\test2.dll";
                project.SetCommandLineArguments($"/out:{newBinPath}");
                Assert.Equal(newBinPath, project.TryGetBinOutputPath());
                Assert.Equal(newBinPath, project.TryGetObjOutputPath());

                // Change output file name and folder.
                newBinPath = @"C:\NewFolder3\test3.dll";
                project.SetCommandLineArguments($"/out:{newBinPath}");
                Assert.Equal(newBinPath, project.TryGetBinOutputPath());
                Assert.Equal(newBinPath, project.TryGetObjOutputPath());
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void ProjectGuidSetter_CPS()
        {
            var initialGuid = Guid.NewGuid();
            
            using (var environment = new TestEnvironment())
            using (IWorkspaceProjectContext projectContext = CSharpHelpers.CreateCSharpCPSProject(environment, "Test", initialGuid))
            {
                Assert.Equal(initialGuid, projectContext.Guid);

                var newGuid = Guid.NewGuid();
                projectContext.Guid = newGuid;
                Assert.Equal(newGuid, projectContext.Guid);
            }
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void ProjectLastDesignTimeBuildSucceededSetter_CPS()
        {
            using (var environment = new TestEnvironment())
            using (IWorkspaceProjectContext projectContext = CSharpHelpers.CreateCSharpCPSProject(environment, "Test"))
            {
                Assert.True(projectContext.LastDesignTimeBuildSucceeded);

                projectContext.LastDesignTimeBuildSucceeded = false;
                Assert.False(projectContext.LastDesignTimeBuildSucceeded);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void ProjectDisplayNameSetter_CPS()
        {
            using (var environment = new TestEnvironment())
            using (IWorkspaceProjectContext project = CSharpHelpers.CreateCSharpCPSProject(environment, "Test"))
            {
                Assert.Equal("Test", project.DisplayName);
                var initialProjectFilePath = project.ProjectFilePath;

                var newProjectDisplayName = "Test2";
                project.DisplayName = newProjectDisplayName;

                Assert.Equal(newProjectDisplayName, project.DisplayName);
                Assert.Equal(initialProjectFilePath, project.ProjectFilePath);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void ProjectFilePathSetter_CPS()
        {
            using (var environment = new TestEnvironment())
            using (IWorkspaceProjectContext project = CSharpHelpers.CreateCSharpCPSProject(environment, "Test"))
            {
                var initialProjectDisplayName = project.DisplayName;
                var initialProjectFilePath = project.ProjectFilePath;
                var newFilePath = Temp.CreateFile().Path;
                var extension = PathUtilities.GetExtension(initialProjectFilePath);
                if (!string.IsNullOrEmpty(extension))
                {
                    newFilePath = PathUtilities.ChangeExtension(newFilePath, extension);
                }

                project.ProjectFilePath = newFilePath;

                Assert.Equal(newFilePath, project.ProjectFilePath);
                Assert.Equal(initialProjectDisplayName, project.DisplayName);

                SharedResourceHelpers.CleanupAllGeneratedFiles(newFilePath);
            }
        }
    }
}
