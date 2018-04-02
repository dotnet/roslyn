﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim.LegacyProject
{
    using static CSharpHelpers;

    [UseExportProvider]
    public class CSharpReferenceTests
    {
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void AddingReferenceToProjectMetadataPromotesToProjectReference()
        {
            using (var environment = new TestEnvironment())
            {
                var project1 = CreateCSharpProject(environment, "project1");
                project1.SetBinOutputPathAndRelatedData(@"c:\project1.dll");

                var project2 = CreateCSharpProject(environment, "project2");
                project2.SetBinOutputPathAndRelatedData(@"c:\project2.dll");

                // since this is known to be the output path of project1, the metadata reference is converted to a project reference
                project2.OnImportAdded(@"c:\project1.dll", "project1");

                Assert.Equal(true, project2.GetCurrentProjectReferences().Any(pr => pr.ProjectId == project1.Id));

                project2.Disconnect();
                project1.Disconnect();
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void AddCyclicProjectMetadataReferences()
        {
            using (var environment = new TestEnvironment())
            {
                var project1 = CreateCSharpProject(environment, "project1");
                project1.SetBinOutputPathAndRelatedData(@"c:\project1.dll");

                var project2 = CreateCSharpProject(environment, "project2");
                project2.SetBinOutputPathAndRelatedData(@"c:\project2.dll");

                project1.AddProjectReference(new ProjectReference(project2.Id));

                // normally this metadata reference would be elevated to a project reference, but fails because of cyclicness
                project2.OnImportAdded(@"c:\project1.dll", "project1");

                Assert.Equal(true, project1.GetCurrentProjectReferences().Any(pr => pr.ProjectId == project2.Id));
                Assert.Equal(false, project2.GetCurrentProjectReferences().Any(pr => pr.ProjectId == project1.Id));

                project2.Disconnect();
                project1.Disconnect();
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void AddCyclicProjectReferences()
        {
            using (var environment = new TestEnvironment())
            {
                var project1 = CreateCSharpProject(environment, "project1");
                var project2 = CreateCSharpProject(environment, "project2");

                project1.AddProjectReference(new ProjectReference(project2.Id));
                project2.AddProjectReference(new ProjectReference(project1.Id));

                Assert.Equal(true, project1.GetCurrentProjectReferences().Any(pr => pr.ProjectId == project2.Id));
                Assert.Equal(false, project2.GetCurrentProjectReferences().Any(pr => pr.ProjectId == project1.Id));

                project2.Disconnect();
                project1.Disconnect();
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void AddCyclicProjectReferencesDeep()
        {
            using (var environment = new TestEnvironment())
            {
                var project1 = CreateCSharpProject(environment, "project1");
                var project2 = CreateCSharpProject(environment, "project2");
                var project3 = CreateCSharpProject(environment, "project3");
                var project4 = CreateCSharpProject(environment, "project4");

                project1.AddProjectReference(new ProjectReference(project2.Id));
                project2.AddProjectReference(new ProjectReference(project3.Id));
                project3.AddProjectReference(new ProjectReference(project4.Id));
                project4.AddProjectReference(new ProjectReference(project1.Id));

                Assert.Equal(true, project1.GetCurrentProjectReferences().Any(pr => pr.ProjectId == project2.Id));
                Assert.Equal(true, project2.GetCurrentProjectReferences().Any(pr => pr.ProjectId == project3.Id));
                Assert.Equal(true, project3.GetCurrentProjectReferences().Any(pr => pr.ProjectId == project4.Id));
                Assert.Equal(false, project4.GetCurrentProjectReferences().Any(pr => pr.ProjectId == project1.Id));

                project4.Disconnect();
                project3.Disconnect();
                project2.Disconnect();
                project1.Disconnect();
            }
        }

        [WorkItem(12707, "https://github.com/dotnet/roslyn/issues/12707")]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void AddingProjectReferenceAndUpdateReferenceBinPath()
        {
            using (var environment = new TestEnvironment())
            {
                var project1 = CreateCSharpProject(environment, "project1");
                project1.SetBinOutputPathAndRelatedData(@"c:\project1.dll");

                var project2 = CreateCSharpProject(environment, "project2");
                project2.SetBinOutputPathAndRelatedData(@"c:\project2.dll");

                // since this is known to be the output path of project1, the metadata reference is converted to a project reference
                project2.OnImportAdded(@"c:\project1.dll", "project1");

                Assert.Single(project2.GetCurrentProjectReferences().Where(pr => pr.ProjectId == project1.Id));

                // update bin bath for project1.
                project1.SetBinOutputPathAndRelatedData(@"c:\new_project1.dll");

                // Verify project reference updated after bin path change.
                Assert.Empty(project2.GetCurrentProjectReferences());

                // This is a metadata reference to the original path
                var metadataReference = Assert.Single(project2.GetCurrentMetadataReferences());
                Assert.Equal(@"c:\project1.dll", metadataReference.FilePath);

                project2.Disconnect();
                project1.Disconnect();
            }
        }

        [WorkItem(12707, "https://github.com/dotnet/roslyn/issues/12707")]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void DisconnectingProjectShouldConvertConvertedReferencesBack()
        {
            using (var environment = new TestEnvironment())
            {
                var project1 = CreateCSharpProject(environment, "project1");
                project1.SetBinOutputPathAndRelatedData(@"c:\project1.dll");

                var project2 = CreateCSharpProject(environment, "project2");
                project2.SetBinOutputPathAndRelatedData(@"c:\project2.dll");

                // since this is known to be the output path of project1, the metadata reference is converted to a project reference
                project2.OnImportAdded(@"c:\project1.dll", "project1");

                Assert.Single(project2.GetCurrentProjectReferences().Where(pr => pr.ProjectId == project1.Id));

                project1.Disconnect();

                // Verify project reference updated after bin path change.
                Assert.Empty(project2.GetCurrentProjectReferences());
                Assert.Single(project2.GetCurrentMetadataReferences().Where(r => r.FilePath == @"c:\project1.dll"));

                project2.Disconnect();
            }
        }

        [WorkItem(461967, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/461967")]
        [WpfFact()]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void AddingMetadataReferenceToProjectThatCannotCompileInTheIdeKeepsMetadataReference()
        {
            using (var environment = new TestEnvironment())
            {
                var project1 = CreateCSharpProject(environment, "project1");
                project1.SetBinOutputPathAndRelatedData(@"c:\project1.dll");

                var project2 = CreateNonCompilableProject(environment, "project2", @"C:\project2.fsproj");
                project2.SetBinOutputPathAndRelatedData(@"c:\project2.dll");

                project1.OnImportAdded(@"c:\project2.dll", "project2");

                // We shoudl not have converted that to a project reference, because we would have no way to produce the compilation
                Assert.Empty(project1.GetCurrentProjectReferences());

                project2.Disconnect();
                project1.Disconnect();
            }
        }
    }
}
