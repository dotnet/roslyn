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
        [ConditionalFact(typeof(VisualStudioMSBuildInstalled)), Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
        public async Task TestOpenProject_NetCoreApp2()
        {
            CreateFiles(GetNetCoreApp2Files());

            var projectFilePath = GetSolutionFileName("NetCoreApp2.csproj");

            DotNetHelper.Restore("NetCoreApp2.csproj", workingDirectory: this.SolutionDirectory.Path);

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
