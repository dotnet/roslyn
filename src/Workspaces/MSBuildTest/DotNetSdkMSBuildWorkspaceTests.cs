// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.IO;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests.TestFiles;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.MSBuild.UnitTests
{
    public class DotNetSdkMSBuildWorkspaceTests : MSBuildWorkspaceTestBase
    {
        [ConditionalFact(typeof(DotNetSdkMSBuildInstalled)), Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
        public void TestOpenProject_WithInvalidFilePath_Fails()
        {
            CreateFiles(GetSimpleCSharpSolutionFiles());
            var projectFilePath = GetSolutionFileName(@"http://localhost/Invalid/InvalidProject.csproj");

            using var workspace = CreateMSBuildWorkspace();

            AssertEx.Throws<FileNotFoundException>(() => workspace.OpenProjectAsync(projectFilePath).Wait());
        }

        [ConditionalFact(typeof(DotNetSdkMSBuildInstalled)), Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
        public void TestOpenProject_WithInvalidProjectReference_SkipFalse_Fails()
        {
            CreateFiles(GetMultiProjectSolutionFiles()
                .WithFile(@"VisualBasicProject\VisualBasicProject.vbproj", Resources.ProjectFiles.VisualBasic.InvalidProjectReference));
            var projectFilePath = GetSolutionFileName(@"VisualBasicProject\VisualBasicProject.vbproj");

            using var workspace = CreateMSBuildWorkspace();
            workspace.SkipUnrecognizedProjects = false;

            AssertEx.Throws<FileNotFoundException>(() => workspace.OpenProjectAsync(projectFilePath).Wait());
        }

        [ConditionalFact(typeof(DotNetSdkMSBuildInstalled)), Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
        public void TestOpenSolution_WithInvalidProjectPath_SkipFalse_Fails()
        {
            // when not skipped we should get an exception for the invalid project

            CreateFiles(GetSimpleCSharpSolutionFiles()
                .WithFile(@"TestSolution.sln", Resources.SolutionFiles.InvalidProjectPath));
            var solutionFilePath = GetSolutionFileName(@"TestSolution.sln");

            using var workspace = CreateMSBuildWorkspace();
            workspace.SkipUnrecognizedProjects = false;

            AssertEx.Throws<FileNotFoundException>(() => workspace.OpenSolutionAsync(solutionFilePath).Wait());
        }

        [ConditionalFact(typeof(DotNetSdkMSBuildInstalled)), Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
        public void TestOpenSolution_WithInvalidSolutionFile_Fails()
        {
            CreateFiles(GetSimpleCSharpSolutionFiles());
            var solutionFilePath = GetSolutionFileName(@"http://localhost/Invalid/InvalidSolution.sln");

            using var workspace = CreateMSBuildWorkspace();

            AssertEx.Throws<FileNotFoundException>(() => workspace.OpenSolutionAsync(solutionFilePath).Wait());
        }
    }
}
