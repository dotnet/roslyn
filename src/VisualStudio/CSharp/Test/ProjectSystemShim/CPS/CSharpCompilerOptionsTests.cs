// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim.CPS
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
    public class CSharpCompilerOptionsTests : TestBase
    {
        [WpfFact]
        public async Task DocumentationModeSetToDiagnoseIfProducingDocFile_CPS()
        {
            using var environment = new TestEnvironment();
            using var project = await CSharpHelpers.CreateCSharpCPSProjectAsync(environment, "Test", commandLineArguments: @"/doc:DocFile.xml");
            var parseOptions = environment.Workspace.CurrentSolution.Projects.Single().ParseOptions;
            Assert.Equal(DocumentationMode.Diagnose, parseOptions.DocumentationMode);
        }

        [WpfFact]
        public async Task DocumentationModeSetToParseIfNotProducingDocFile_CPS()
        {
            using var environment = new TestEnvironment();
            using var project = await CSharpHelpers.CreateCSharpCPSProjectAsync(environment, "Test", commandLineArguments: @"/doc:");
            var parseOptions = environment.Workspace.CurrentSolution.Projects.Single().ParseOptions;
            Assert.Equal(DocumentationMode.Parse, parseOptions.DocumentationMode);
        }

        [WpfFact]
        public async Task ProjectSettingsOptionAddAndRemove_CPS()
        {
            using var environment = new TestEnvironment();
            using var project = await CSharpHelpers.CreateCSharpCPSProjectAsync(environment, "Test", commandLineArguments: @"/warnaserror:CS1111");
            var options = environment.GetUpdatedCompilationOptionOfSingleProject();
            Assert.Equal(expected: ReportDiagnostic.Error, actual: options.SpecificDiagnosticOptions["CS1111"]);

            project.SetOptions(ImmutableArray.Create(@"/warnaserror"));
            options = environment.GetUpdatedCompilationOptionOfSingleProject();
            Assert.False(options.SpecificDiagnosticOptions.ContainsKey("CS1111"));
        }

        [WpfFact]
        public async Task ProjectOutputBinPathChange_CPS()
        {
            var initialObjPath = @"C:\test.dll";
            var initialBinPath = initialObjPath;

            using var environment = new TestEnvironment();
            using var project = await CSharpHelpers.CreateCSharpCPSProjectAsync(environment, "Test", commandLineArguments: $"/out:{initialObjPath}");
            Assert.Equal(initialObjPath, project.CompilationOutputAssemblyFilePath);
            Assert.Equal(initialBinPath, project.BinOutputPath);

            // Change obj output folder from command line arguments - verify that objOutputPath changes, but binOutputPath is the same.
            var newObjPath = @"C:\NewFolder\test.dll";
            project.SetOptions(ImmutableArray.Create($"/out:{newObjPath}"));
            Assert.Equal(newObjPath, project.CompilationOutputAssemblyFilePath);
            Assert.Equal(initialBinPath, project.BinOutputPath);

            // Change output file name - verify that objOutputPath changes, but binOutputPath is the same.
            newObjPath = @"C:\NewFolder\test2.dll";
            project.SetOptions(ImmutableArray.Create($"/out:{newObjPath}"));
            Assert.Equal(newObjPath, project.CompilationOutputAssemblyFilePath);
            Assert.Equal(initialBinPath, project.BinOutputPath);

            // Change output file name and folder - verify that objOutputPath changes, but binOutputPath is the same.
            newObjPath = @"C:\NewFolder3\test3.dll";
            project.SetOptions(ImmutableArray.Create($"/out:{newObjPath}"));
            Assert.Equal(newObjPath, project.CompilationOutputAssemblyFilePath);
            Assert.Equal(initialBinPath, project.BinOutputPath);

            // Change bin output folder - verify that binOutputPath changes, but objOutputPath is the same.
            var newBinPath = @"C:\NewFolder4\test.dll";
            project.BinOutputPath = newBinPath;
            Assert.Equal(newObjPath, project.CompilationOutputAssemblyFilePath);
            Assert.Equal(newBinPath, project.BinOutputPath);

            // Change bin output folder to non-absolute path - verify that binOutputPath changes to normalized path, but objOutputPath is the same.
            newBinPath = @"test.dll";
            var expectedNewBinPath = Path.Combine(Path.GetTempPath(), newBinPath);
            project.BinOutputPath = newBinPath;
            Assert.Equal(newObjPath, project.CompilationOutputAssemblyFilePath);
            Assert.Equal(expectedNewBinPath, project.BinOutputPath);

            // Change obj  folder to non-canonical path - verify that objOutputPath changes to normalized path, but binOutputPath is unchanged.
            var relativeObjPath = @"..\folder\\test.dll";
            var absoluteObjPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), relativeObjPath));
            project.SetOptions(ImmutableArray.Create($"/out:{relativeObjPath}"));
            Assert.Equal(absoluteObjPath, project.CompilationOutputAssemblyFilePath);
            Assert.Equal(expectedNewBinPath, project.BinOutputPath);
        }

        [WpfFact]
        public async Task InvalidProjectOutputBinPaths_CPS()
        {
            using var environment = new TestEnvironment();
            await Assert.ThrowsAsync<InvalidProjectDataException>(() => CSharpHelpers.CreateCSharpCPSProjectAsync(environment, "Test2", binOutputPath: ""));
            await Assert.ThrowsAsync<InvalidProjectDataException>(() => CSharpHelpers.CreateCSharpCPSProjectAsync(environment, "Test3", binOutputPath: "Test.dll"));
        }

        [WpfFact]
        public async Task ProjectGuidSetter_CPS()
        {
            var initialGuid = Guid.NewGuid();

            using var environment = new TestEnvironment();
            using IWorkspaceProjectContext projectContext = await CSharpHelpers.CreateCSharpCPSProjectAsync(environment, "Test", initialGuid);
            Assert.Equal(initialGuid, projectContext.Guid);

            var newGuid = Guid.NewGuid();
            projectContext.Guid = newGuid;
            Assert.Equal(newGuid, projectContext.Guid);
        }

        [WpfFact]
        public async Task ProjectLastDesignTimeBuildSucceededSetter_CPS()
        {
            using var environment = new TestEnvironment();
            using IWorkspaceProjectContext projectContext = await CSharpHelpers.CreateCSharpCPSProjectAsync(environment, "Test");
            Assert.True(projectContext.LastDesignTimeBuildSucceeded);

            projectContext.LastDesignTimeBuildSucceeded = false;
            Assert.False(projectContext.LastDesignTimeBuildSucceeded);
        }

        [WpfFact]
        public async Task ProjectDisplayNameSetter_CPS()
        {
            using var environment = new TestEnvironment();
            using IWorkspaceProjectContext project = await CSharpHelpers.CreateCSharpCPSProjectAsync(environment, "Test");
            Assert.Equal("Test", project.DisplayName);
            var initialProjectFilePath = project.ProjectFilePath;

            var newProjectDisplayName = "Test2";
            project.DisplayName = newProjectDisplayName;

            Assert.Equal(newProjectDisplayName, project.DisplayName);
            Assert.Equal(initialProjectFilePath, project.ProjectFilePath);
        }

        [WpfFact]
        public async Task ProjectFilePathSetter_CPS()
        {
            using var environment = new TestEnvironment();
            using IWorkspaceProjectContext project = await CSharpHelpers.CreateCSharpCPSProjectAsync(environment, "Test");
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

        [WpfFact]
        public async Task ChecksumAlgorithm_CPS()
        {
            using var environment = new TestEnvironment();
            using var cpsProject = await CSharpHelpers.CreateCSharpCPSProjectAsync(environment, "Test");

            Assert.Equal(SourceHashAlgorithms.Default, environment.Workspace.CurrentSolution.Projects.Single().State.ChecksumAlgorithm);

            cpsProject.SetOptions(ImmutableArray.Create("/checksumalgorithm:SHA1"));

            var project = environment.Workspace.CurrentSolution.Projects.Single();
            Assert.Equal(SourceHashAlgorithm.Sha1, environment.Workspace.CurrentSolution.Projects.Single().State.ChecksumAlgorithm);
        }
    }
}
