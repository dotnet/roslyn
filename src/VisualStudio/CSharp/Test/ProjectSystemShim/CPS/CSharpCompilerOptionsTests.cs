// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim.CPS
{
    [UseExportProvider]
    public class CSharpCompilerOptionsTests : TestBase
    {
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void DocumentationModeSetToDiagnoseIfProducingDocFile_CPS()
        {
            using var environment = new TestEnvironment();
            using var project = CSharpHelpers.CreateCSharpCPSProject(environment, "Test", commandLineArguments: @"/doc:DocFile.xml");
            var parseOptions = environment.Workspace.CurrentSolution.Projects.Single().ParseOptions;
            Assert.Equal(DocumentationMode.Diagnose, parseOptions.DocumentationMode);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void DocumentationModeSetToParseIfNotProducingDocFile_CPS()
        {
            using var environment = new TestEnvironment();
            using var project = CSharpHelpers.CreateCSharpCPSProject(environment, "Test", commandLineArguments: @"/doc:");
            var parseOptions = environment.Workspace.CurrentSolution.Projects.Single().ParseOptions;
            Assert.Equal(DocumentationMode.Parse, parseOptions.DocumentationMode);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void ProjectSettingsOptionAddAndRemove_CPS()
        {
            using var environment = new TestEnvironment();
            using var project = CSharpHelpers.CreateCSharpCPSProject(environment, "Test", commandLineArguments: @"/warnaserror:CS1111");
            var options = environment.GetUpdatedCompilationOptionOfSingleProject();
            Assert.Equal(expected: ReportDiagnostic.Error, actual: options.SpecificDiagnosticOptions["CS1111"]);

            project.SetOptions(@"/warnaserror");
            options = environment.GetUpdatedCompilationOptionOfSingleProject();
            Assert.False(options.SpecificDiagnosticOptions.ContainsKey("CS1111"));
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void ProjectOutputBinPathChange_CPS()
        {
            var initialObjPath = @"C:\test.dll";
            var initialBinPath = initialObjPath;

            using var environment = new TestEnvironment();
            using var project = CSharpHelpers.CreateCSharpCPSProject(environment, "Test", commandLineArguments: $"/out:{initialObjPath}");
            Assert.Equal(initialObjPath, project.GetIntermediateOutputFilePath());
            Assert.Equal(initialBinPath, project.BinOutputPath);

            // Change obj output folder from command line arguments - verify that objOutputPath changes, but binOutputPath is the same.
            var newObjPath = @"C:\NewFolder\test.dll";
            project.SetOptions($"/out:{newObjPath}");
            Assert.Equal(newObjPath, project.GetIntermediateOutputFilePath());
            Assert.Equal(initialBinPath, project.BinOutputPath);

            // Change output file name - verify that objOutputPath changes, but binOutputPath is the same.
            newObjPath = @"C:\NewFolder\test2.dll";
            project.SetOptions($"/out:{newObjPath}");
            Assert.Equal(newObjPath, project.GetIntermediateOutputFilePath());
            Assert.Equal(initialBinPath, project.BinOutputPath);

            // Change output file name and folder - verify that objOutputPath changes, but binOutputPath is the same.
            newObjPath = @"C:\NewFolder3\test3.dll";
            project.SetOptions($"/out:{newObjPath}");
            Assert.Equal(newObjPath, project.GetIntermediateOutputFilePath());
            Assert.Equal(initialBinPath, project.BinOutputPath);

            // Change bin output folder - verify that binOutputPath changes, but objOutputPath is the same.
            var newBinPath = @"C:\NewFolder4\test.dll";
            project.BinOutputPath = newBinPath;
            Assert.Equal(newObjPath, project.GetIntermediateOutputFilePath());
            Assert.Equal(newBinPath, project.BinOutputPath);

            // Change bin output folder to non-normalized path - verify that binOutputPath changes to normalized path, but objOutputPath is the same.
            newBinPath = @"test.dll";
            var expectedNewBinPath = Path.Combine(Path.GetTempPath(), newBinPath);
            project.BinOutputPath = newBinPath;
            Assert.Equal(newObjPath, project.GetIntermediateOutputFilePath());
            Assert.Equal(expectedNewBinPath, project.BinOutputPath);
        }

        [WpfFact, WorkItem(14520, "https://github.com/dotnet/roslyn/issues/14520")]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void InvalidProjectOutputBinPaths_CPS1()
        {
            using var environment = new TestEnvironment();
            using var project1 = CSharpHelpers.CreateCSharpCPSProject(environment, "Test", binOutputPath: null);
            // Null output path is allowed.
            Assert.Null(project1.BinOutputPath);
        }

        [WpfFact, WorkItem(14520, "https://github.com/dotnet/roslyn/issues/14520")]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void InvalidProjectOutputBinPaths_CPS2()
        {
            using var environment = new TestEnvironment();
            using var project2 = CSharpHelpers.CreateCSharpCPSProject(environment, "Test2", binOutputPath: String.Empty);
            // Empty output path is not allowed, it gets reset to null.
            Assert.Null(project2.BinOutputPath);
        }

        [WpfFact, WorkItem(14520, "https://github.com/dotnet/roslyn/issues/14520")]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void InvalidProjectOutputBinPaths_CPS3()
        {
            using var environment = new TestEnvironment();
            using var project3 = CSharpHelpers.CreateCSharpCPSProject(environment, "Test3", binOutputPath: "Test.dll");
            // Non-rooted output path is not allowed, it gets reset to a temp rooted path.
            Assert.Equal(Path.Combine(Path.GetTempPath(), "Test.dll"), project3.BinOutputPath);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void ProjectGuidSetter_CPS()
        {
            var initialGuid = Guid.NewGuid();

            using var environment = new TestEnvironment();
            using IWorkspaceProjectContext projectContext = CSharpHelpers.CreateCSharpCPSProject(environment, "Test", initialGuid);
            Assert.Equal(initialGuid, projectContext.Guid);

            var newGuid = Guid.NewGuid();
            projectContext.Guid = newGuid;
            Assert.Equal(newGuid, projectContext.Guid);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void ProjectLastDesignTimeBuildSucceededSetter_CPS()
        {
            using var environment = new TestEnvironment();
            using IWorkspaceProjectContext projectContext = CSharpHelpers.CreateCSharpCPSProject(environment, "Test");
            Assert.True(projectContext.LastDesignTimeBuildSucceeded);

            projectContext.LastDesignTimeBuildSucceeded = false;
            Assert.False(projectContext.LastDesignTimeBuildSucceeded);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void ProjectDisplayNameSetter_CPS()
        {
            using var environment = new TestEnvironment();
            using IWorkspaceProjectContext project = CSharpHelpers.CreateCSharpCPSProject(environment, "Test");
            Assert.Equal("Test", project.DisplayName);
            var initialProjectFilePath = project.ProjectFilePath;

            var newProjectDisplayName = "Test2";
            project.DisplayName = newProjectDisplayName;

            Assert.Equal(newProjectDisplayName, project.DisplayName);
            Assert.Equal(initialProjectFilePath, project.ProjectFilePath);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void ProjectFilePathSetter_CPS()
        {
            using var environment = new TestEnvironment();
            using IWorkspaceProjectContext project = CSharpHelpers.CreateCSharpCPSProject(environment, "Test");
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
