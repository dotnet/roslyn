// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.Workspace
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class WorkspacesNetCore : WorkspaceBase
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public WorkspacesNetCore(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
            VisualStudio.Instance.SolutionExplorer.CreateSolution(nameof(WorkspacesNetCore));
            VisualStudio.Instance.SolutionExplorer.AddProject(ProjectName, WellKnownProjectTemplates.CSharpNetCoreClassLibrary, LanguageName);
        }

        [Test.Utilities.WorkItem(1825, "https://github.com/dotnet/roslyn-project-system/issues/1825")]
        [Fact(Skip = "1825"), Trait(Traits.Feature, Traits.Features.Workspace)]
        public void OpenCSharpThenVBSolution()
        {
            OpenCSharpThenVBSolutionCommon();
        }

        [Test.Utilities.WorkItem(1826, "https://github.com/dotnet/roslyn-project-system/issues/1826")]
        [Fact(Skip = "1826"), Trait(Traits.Feature, Traits.Features.Workspace)]
        public void MetadataReference()
        {
            EditProjectFile(ProjectName);
            Editor.SetText(@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net461</TargetFramework>
  </PropertyGroup>
</Project>");
            SaveAll();
            WaitForAsyncOperations(FeatureAttribute.Workspace);
            OpenFile("Class1.cs");
            MetadataReferenceCommon();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void ProjectReference()
        {
            ProjectReferenceCommon(WellKnownProjectTemplates.CSharpNetCoreClassLibrary);
        }

        [Test.Utilities.WorkItem(1825, "https://github.com/dotnet/roslyn-project-system/issues/1825")]
        [Fact(Skip = "1825"), Trait(Traits.Feature, Traits.Features.Workspace)]
        public void ProjectProperties()
        {
            VisualStudio.Instance.SolutionExplorer.CreateSolution(nameof(WorkspacesDesktop));
            VisualStudio.Instance.SolutionExplorer.AddProject(ProjectName, WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic);
            ProjectPropertiesCommon();
        }
    }
}
