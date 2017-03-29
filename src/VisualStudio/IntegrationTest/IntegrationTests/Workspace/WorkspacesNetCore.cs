// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.VisualStudio.IntegrationTests.Extensions;
using Roslyn.VisualStudio.IntegrationTests.Extensions.SolutionExplorer;
using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.Workspace
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class WorkspacesNetCore : WorkspaceBase
    {
        public WorkspacesNetCore(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, WellKnownProjectTemplates.CSharpNetCoreClassLibrary)
        {
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn-project-system/issues/1825"), Trait(Traits.Feature, Traits.Features.Workspace)]
        public override void OpenCSharpThenVBSolution()
        {
            base.OpenCSharpThenVBSolution();
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn-project-system/issues/1826"), Trait(Traits.Feature, Traits.Features.Workspace)]
        public override void MetadataReference()
        {
            var project = new ProjectUtils.Project(ProjectName);
            this.EditProjectFile(project);
            Editor.SetText(@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net461</TargetFramework>
  </PropertyGroup>
</Project>");
            this.SaveAll();
            this.WaitForAsyncOperations(FeatureAttribute.Workspace);
            this.OpenFile("Class1.cs", project);
            base.MetadataReference();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public override void ProjectReference()
        {
            base.ProjectReference();
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn-project-system/issues/1825"), Trait(Traits.Feature, Traits.Features.Workspace)]
        public override void ProjectProperties()
        {
            VisualStudio.Instance.SolutionExplorer.CreateSolution(nameof(WorkspacesDesktop));
            VisualStudio.Instance.SolutionExplorer.AddProject(ProjectName, WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic);
            base.ProjectProperties();
        }
    }
}
