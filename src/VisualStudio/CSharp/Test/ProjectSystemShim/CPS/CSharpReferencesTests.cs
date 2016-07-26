// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim.CPS
{
    using static CSharpHelpers;

    public class CSharpReferenceTests
    {
        [Fact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void AddRemoveProjectAndMetadataReference_CPS()
        {
            using (var environment = new TestEnvironment())
            {
                var project1 = CreateCSharpCPSProject(environment, "project1", commandLineArguments: @"/out:c:\project1.dll");
                var project2 = CreateCSharpCPSProject(environment, "project2", commandLineArguments: @"/out:c:\project2.dll");
                var project3 = CreateCSharpCPSProject(environment, "project3", commandLineArguments: @"/out:c:\project3.dll");

                // Add project reference
                project3.AddProjectReference(project1, new MetadataReferenceProperties());

                // Add project reference as metadata reference: since this is known to be the output path of project1, the metadata reference is converted to a project reference
                project3.AddMetadataReference(@"c:\project2.dll", new MetadataReferenceProperties());

                // Add metadata reference
                var metadaRefFilePath = @"c:\someAssembly.dll";
                project3.AddMetadataReference(metadaRefFilePath, new MetadataReferenceProperties(embedInteropTypes: true));

                Assert.True(project3.GetCurrentProjectReferences().Any(pr => pr.ProjectId == project1.Id));
                Assert.True(project3.GetCurrentProjectReferences().Any(pr => pr.ProjectId == project2.Id));
                Assert.True(project3.GetCurrentMetadataReferences().Any(mr => mr.FilePath == metadaRefFilePath));

                // Remove project reference
                project3.RemoveProjectReference(project1);

                // Remove project reference as metadata reference: since this is known to be the output path of project1, the metadata reference is converted to a project reference
                project3.RemoveMetadataReference(@"c:\project2.dll");

                // Remove metadata reference
                project3.RemoveMetadataReference(metadaRefFilePath);

                Assert.False(project3.GetCurrentProjectReferences().Any(pr => pr.ProjectId == project1.Id));
                Assert.False(project3.GetCurrentProjectReferences().Any(pr => pr.ProjectId == project2.Id));
                Assert.False(project3.GetCurrentMetadataReferences().Any(mr => mr.FilePath == metadaRefFilePath));

                project3.Disconnect();
                project2.Disconnect();
                project1.Disconnect();
            }
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void AddRemoveAnalyzerReference_CPS()
        {
            using (var environment = new TestEnvironment())
            {
                var project = CreateCSharpCPSProject(environment, "project1");

                // Add analyzer reference
                var analyzerAssemblyFullPath = @"c:\someAssembly.dll";
                project.AddAnalyzerAssembly(analyzerAssemblyFullPath);
                Assert.True(project.CurrentProjectAnalyzersContains(analyzerAssemblyFullPath));

                // Remove analyzer reference
                project.RemoveAnalyzerAssembly(analyzerAssemblyFullPath);
                Assert.False(project.CurrentProjectAnalyzersContains(analyzerAssemblyFullPath));

                project.Disconnect();
            }
        }
    }
}