// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim.CPS
{
    public class CSharpCompilerOptionsTests : TestBase
    {
        [Fact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void DocumentationModeSetToDiagnoseIfProducingDocFile_CPS()
        {
            using (var environment = new TestEnvironment())
            {
                var project = CSharpHelpers.CreateCSharpCPSProject(environment, "Test", commandLineArguments: @"/doc:DocFile.xml");

                var workspaceProject = environment.Workspace.CurrentSolution.Projects.Single();
                var options = (CSharpParseOptions)workspaceProject.ParseOptions;

                Assert.Equal(DocumentationMode.Diagnose, options.DocumentationMode);
            }
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void DocumentationModeSetToParseIfNotProducingDocFile_CPS()
        {
            using (var environment = new TestEnvironment())
            {
                var project = CSharpHelpers.CreateCSharpCPSProject(environment, "Test", commandLineArguments: @"/doc:");

                var workspaceProject = environment.Workspace.CurrentSolution.Projects.Single();
                var options = (CSharpParseOptions)workspaceProject.ParseOptions;

                Assert.Equal(DocumentationMode.Parse, options.DocumentationMode);
            }
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void ProjectSettingsOptionAddAndRemove_CPS()
        {
            using (var environment = new TestEnvironment())
            {
                var project = CSharpHelpers.CreateCSharpCPSProject(environment, "Test", commandLineArguments: @"/warnaserror:CS1111");

                var options = environment.GetUpdatedCompilationOptionOfSingleProject();
                Assert.Equal(expected: ReportDiagnostic.Error, actual: options.SpecificDiagnosticOptions["CS1111"]);

                CSharpHelpers.SetCommandLineArguments(project, @"/warnaserror");
                options = environment.GetUpdatedCompilationOptionOfSingleProject();
                Assert.False(options.SpecificDiagnosticOptions.ContainsKey("CS1111"));
            }
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void ProjectOutputBinPathChange_CPS()
        {
            using (var environment = new TestEnvironment())
            {
                var initialBinPath = @"C:\test.dll";
                var project = CSharpHelpers.CreateCSharpCPSProject(environment, "Test", $"/out:{initialBinPath}");
                
                Assert.Equal(initialBinPath, project.TryGetBinOutputPath());
                Assert.Equal(initialBinPath, project.TryGetObjOutputPath());

                // Change output folder.
                var newBinPath = @"C:\NewFolder\test.dll";
                CSharpHelpers.SetCommandLineArguments(project, $"/out:{newBinPath}");
                Assert.Equal(newBinPath, project.TryGetBinOutputPath());
                Assert.Equal(newBinPath, project.TryGetObjOutputPath());

                // Change output file name.
                newBinPath = @"C:\NewFolder\test2.dll";
                CSharpHelpers.SetCommandLineArguments(project, $"/out:{newBinPath}");
                Assert.Equal(newBinPath, project.TryGetBinOutputPath());
                Assert.Equal(newBinPath, project.TryGetObjOutputPath());

                // Change output file name and folder.
                newBinPath = @"C:\NewFolder3\test3.dll";
                CSharpHelpers.SetCommandLineArguments(project, $"/out:{newBinPath}");
                Assert.Equal(newBinPath, project.TryGetBinOutputPath());
                Assert.Equal(newBinPath, project.TryGetObjOutputPath());
            }
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void ProjectGuidSetter_CPS()
        {
            using (var environment = new TestEnvironment())
            {
                var initialGuid = new Guid();
                var intialProjectType = new Guid().ToString();
                var projectContext = (IProjectContext)CSharpHelpers.CreateCSharpCPSProject(environment, "Test", initialGuid, intialProjectType);
                
                Assert.Equal(initialGuid, projectContext.Guid);
                Assert.Equal(intialProjectType, projectContext.ProjectType);

                var newGuid = new Guid();
                projectContext.Guid = newGuid;
                Assert.Equal(newGuid, projectContext.Guid);

                var newProjectTypeGuid = new Guid().ToString();
                projectContext.ProjectType = newProjectTypeGuid;
                Assert.Equal(newProjectTypeGuid, projectContext.ProjectType);
            }
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void ProjectDisplayNameSetter_CPS()
        {
            using (var environment = new TestEnvironment())
            {
                var project = (IProjectContext)CSharpHelpers.CreateCSharpCPSProject(environment, "Test");
                Assert.Equal("Test", project.DisplayName);
                var initialProjectFilePath = project.ProjectFilePath;

                var newProjectDisplayName = "Test2";
                project.DisplayName = newProjectDisplayName;

                Assert.Equal(newProjectDisplayName, project.DisplayName);
                Assert.Equal(initialProjectFilePath, project.ProjectFilePath);
            }
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void ProjectFilePathSetter_CPS()
        {
            using (var environment = new TestEnvironment())
            {
                var project = (IProjectContext)CSharpHelpers.CreateCSharpCPSProject(environment, "Test");
                
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
