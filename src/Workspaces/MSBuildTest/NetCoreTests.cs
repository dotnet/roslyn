// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.MSBuild.UnitTests
{
    public class NetCoreTests : MSBuildWorkspaceTestBase
    {
        [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
        [Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public async Task TestOpenProject_NetCoreApp2()
        {
            CreateFiles(GetNetCoreApp2Files());

            var projectFilePath = GetSolutionFileName("Project.csproj");

            DotNetHelper.Restore("Project.csproj", workingDirectory: this.SolutionDirectory.Path);

            using (var workspace = CreateMSBuildWorkspace())
            {
                var project = await workspace.OpenProjectAsync(projectFilePath);
                var document = project.Documents.First(d => d.Name == "Program.cs");
                var semanticModel = await document.GetSemanticModelAsync();
                var diagnostics = semanticModel.GetDiagnostics();
                Assert.Empty(diagnostics);
            }
        }

        [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
        [Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public async Task TestOpenProject_NetCoreMultiTFM()
        {
            CreateFiles(GetNetCoreMultiTFMFiles());

            var projectFilePath = GetSolutionFileName("Project.csproj");

            DotNetHelper.Restore("Project.csproj", workingDirectory: this.SolutionDirectory.Path);

            using (var workspace = CreateMSBuildWorkspace())
            {
                var project = await workspace.OpenProjectAsync(projectFilePath);
                var document = project.Documents.First(d => d.Name == "Program.cs");
                var semanticModel = await document.GetSemanticModelAsync();
                var diagnostics = semanticModel.GetDiagnostics();
                Assert.Empty(diagnostics);
            }
        }

        [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
        [Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public async Task TestOpenProject_NetCoreMultiTFM_ProjectReference()
        {
            CreateFiles(GetNetCoreMultiTFMFiles_ProjectReference());

            var output = DotNetHelper.Restore(@"Project\Project.csproj", workingDirectory: this.SolutionDirectory.Path);
            output = DotNetHelper.Restore(@"Library\Library.csproj", workingDirectory: this.SolutionDirectory.Path);

            var projectFilePath = GetSolutionFileName(@"Project\Project.csproj");

            using (var workspace = CreateMSBuildWorkspace())
            {
                var project = await workspace.OpenProjectAsync(projectFilePath);
                var document = project.Documents.First(d => d.Name == "Program.cs");
                var semanticModel = await document.GetSemanticModelAsync();
                var diagnostics = semanticModel.GetDiagnostics();
                Assert.Empty(diagnostics);
            }
        }
    }
}
