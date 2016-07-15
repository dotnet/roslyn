// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim
{
    public partial class CSharpCompilerOptionsTests : TestBase
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
                var projectShim = CSharpHelpers.CreateCSharpCPSProject(environment, "Test", $"/out:{initialBinPath}");
                var project = (AbstractProject)projectShim;

                Assert.Equal(initialBinPath, project.TryGetBinOutputPath());
                Assert.Equal(initialBinPath, project.TryGetObjOutputPath());

                // Change output folder.
                var newBinPath = @"C:\NewFolder\test.dll";
                CSharpHelpers.SetCommandLineArguments(projectShim, $"/out:{newBinPath}");
                Assert.Equal(newBinPath, project.TryGetBinOutputPath());
                Assert.Equal(newBinPath, project.TryGetObjOutputPath());

                // Change output file name.
                newBinPath = @"C:\NewFolder\test2.dll";
                CSharpHelpers.SetCommandLineArguments(projectShim, $"/out:{newBinPath}");
                Assert.Equal(newBinPath, project.TryGetBinOutputPath());
                Assert.Equal(newBinPath, project.TryGetObjOutputPath());

                // Change output file name and folder.
                newBinPath = @"C:\NewFolder3\test3.dll";
                CSharpHelpers.SetCommandLineArguments(projectShim, $"/out:{newBinPath}");
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
                var projectShim = CSharpHelpers.CreateCSharpCPSProject(environment, "Test", initialGuid);
                var project = (AbstractProject)projectShim;

                Assert.Equal(initialGuid, project.Guid);

                var newGuid = new Guid();
                projectShim.SetProjectGuid(newGuid);
                Assert.Equal(newGuid, project.Guid);
            }
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void ProjectFilePathSetter_CPS()
        {
            using (var environment = new TestEnvironment())
            {
                var projectShim = CSharpHelpers.CreateCSharpCPSProject(environment, "Test");
                var project = (AbstractProject)projectShim;

                var initialProjectFilePath = projectShim.ProjectFilePath;
                var newFilePath = Temp.CreateFile().Path;
                var extension = PathUtilities.GetExtension(initialProjectFilePath);
                if (!string.IsNullOrEmpty(extension))
                {
                    newFilePath = PathUtilities.ChangeExtension(newFilePath, extension);
                }

                projectShim.SetProjectFilePath(newFilePath);

                Assert.Equal(PathUtilities.GetFileName(newFilePath, includeExtension: false), project.DisplayName);
                Assert.Equal(newFilePath, project.ProjectFilePath);

                SharedResourceHelpers.CleanupAllGeneratedFiles(newFilePath);
            }
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void ProjectIsWebsiteProjectSetter_CPS()
        {
            using (var environment = new TestEnvironment())
            {
                var projectShim = CSharpHelpers.CreateCSharpCPSProject(environment, "Test");
                var project = (AbstractProject)projectShim;
                Assert.False(project.IsWebSite);

                projectShim.SetIsWebsiteProject();
                Assert.True(project.IsWebSite);
            }
        }
    }
}
