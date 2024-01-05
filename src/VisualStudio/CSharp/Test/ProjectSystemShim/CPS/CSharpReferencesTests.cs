// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim.CPS
{
    using static CSharpHelpers;

    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
    public class CSharpReferenceTests
    {
        [WpfFact]
        public async Task AddRemoveProjectAndMetadataReference_CPS()
        {
            using var environment = new TestEnvironment();
            var project1 = await CreateCSharpCPSProjectAsync(environment, "project1", commandLineArguments: @"/out:c:\project1.dll");
            var project2 = await CreateCSharpCPSProjectAsync(environment, "project2", commandLineArguments: @"/out:c:\project2.dll");
            var project3 = await CreateCSharpCPSProjectAsync(environment, "project3", commandLineArguments: @"/out:c:\project3.dll");
            var project4 = await CreateCSharpCPSProjectAsync(environment, "project4");

            // Add project reference
            project3.AddProjectReference(project1, new MetadataReferenceProperties());

            // Add project reference as metadata reference: since this is known to be the output path of project1, the metadata reference is converted to a project reference
            project3.AddMetadataReference(@"c:\project2.dll", new MetadataReferenceProperties());

            // Add project reference with no output path
            project3.AddProjectReference(project4, new MetadataReferenceProperties());

            // Add metadata reference
            var metadaRefFilePath = @"c:\someAssembly.dll";
            project3.AddMetadataReference(metadaRefFilePath, new MetadataReferenceProperties(embedInteropTypes: true));

            IEnumerable<ProjectReference> GetProject3ProjectReferences()
            {
                return environment.Workspace
                                  .CurrentSolution.GetProject(project3.Id).ProjectReferences;
            }

            IEnumerable<PortableExecutableReference> GetProject3MetadataReferences()
            {
                return environment.Workspace.CurrentSolution.GetProject(project3.Id)
                                  .MetadataReferences
                                  .Cast<PortableExecutableReference>();
            }

            Assert.True(GetProject3ProjectReferences().Any(pr => pr.ProjectId == project1.Id));
            Assert.True(GetProject3ProjectReferences().Any(pr => pr.ProjectId == project2.Id));
            Assert.True(GetProject3ProjectReferences().Any(pr => pr.ProjectId == project4.Id));
            Assert.True(GetProject3MetadataReferences().Any(mr => mr.FilePath == metadaRefFilePath));

            // Change output path for project reference and verify the reference.
            ((IWorkspaceProjectContext)project4).BinOutputPath = @"C:\project4.dll";
            Assert.Equal(@"C:\project4.dll", project4.BinOutputPath);

            Assert.True(GetProject3ProjectReferences().Any(pr => pr.ProjectId == project4.Id));

            // Remove project reference
            project3.RemoveProjectReference(project1);

            // Remove project reference as metadata reference: since this is known to be the output path of project1, the metadata reference is converted to a project reference
            project3.RemoveMetadataReference(@"c:\project2.dll");

            // Remove metadata reference
            project3.RemoveMetadataReference(metadaRefFilePath);

            Assert.False(GetProject3ProjectReferences().Any(pr => pr.ProjectId == project1.Id));
            Assert.False(GetProject3ProjectReferences().Any(pr => pr.ProjectId == project2.Id));
            Assert.False(GetProject3MetadataReferences().Any(mr => mr.FilePath == metadaRefFilePath));

            project1.Dispose();
            project2.Dispose();
            project4.Dispose();
            project3.Dispose();
        }

        [WpfFact]
        public async Task RemoveProjectConvertsProjectReferencesBack()
        {
            using var environment = new TestEnvironment();
            var project1 = await CreateCSharpCPSProjectAsync(environment, "project1", commandLineArguments: @"/out:c:\project1.dll");
            var project2 = await CreateCSharpCPSProjectAsync(environment, "project2");

            // Add project reference as metadata reference: since this is known to be the output path of project1, the metadata reference is converted to a project reference
            project2.AddMetadataReference(@"c:\project1.dll", new MetadataReferenceProperties());
            Assert.Single(environment.Workspace.CurrentSolution.GetProject(project2.Id).AllProjectReferences);

            // Remove project1. project2's reference should have been converted back
            project1.Dispose();
            Assert.Empty(environment.Workspace.CurrentSolution.GetProject(project2.Id).AllProjectReferences);
            Assert.Single(environment.Workspace.CurrentSolution.GetProject(project2.Id).MetadataReferences);

            project2.Dispose();
        }

        [WpfFact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/461967")]
        [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/727173")]
        public async Task AddingMetadataReferenceToProjectThatCannotCompileInTheIdeKeepsMetadataReference()
        {
            using var environment = new TestEnvironment(typeof(NoCompilationLanguageService));
            var project1 = await CreateCSharpCPSProjectAsync(environment, "project1", commandLineArguments: @"/out:c:\project1.dll");
            var project2 = await CreateNonCompilableProjectAsync(environment, "project2", @"C:\project2.fsproj", targetPath: @"c:\project2.dll");

            project1.AddMetadataReference(project2.BinOutputPath, MetadataReferenceProperties.Assembly);

            // We should not have converted that to a project reference, because we would have no way to produce the compilation
            Assert.Empty(environment.Workspace.CurrentSolution.GetProject(project1.Id).AllProjectReferences);

            project2.Dispose();
            project1.Dispose();
        }

        [WpfFact]
        public async Task AddRemoveAnalyzerReference_CPS()
        {
            using var environment = new TestEnvironment();
            using var project = await CreateCSharpCPSProjectAsync(environment, "project1");
            // Add analyzer reference
            using var tempRoot = new TempRoot();
            var analyzerAssemblyFullPath = tempRoot.CreateFile().Path;

            bool AnalyzersContainsAnalyzer()
            {
                return environment.Workspace.CurrentSolution.Projects.Single()
                                  .AnalyzerReferences.Cast<AnalyzerReference>()
                                  .Any(a => a.FullPath == analyzerAssemblyFullPath);
            }

            project.AddAnalyzerReference(analyzerAssemblyFullPath);
            Assert.True(AnalyzersContainsAnalyzer());

            // Remove analyzer reference
            project.RemoveAnalyzerReference(analyzerAssemblyFullPath);
            Assert.False(AnalyzersContainsAnalyzer());
        }
    }
}
