// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.Workspace
{
    [TestClass]
    public class WorkspacesNetCore : WorkspaceBase
    {
        public WorkspacesNetCore(VisualStudioInstanceFactory instanceFactory)
            : base(WellKnownProjectTemplates.CSharpNetCoreClassLibrary)
        {
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.Workspace)]
        [TestProperty(Traits.Feature, Traits.Features.NetCore)]
        public override void OpenCSharpThenVBSolution()
        {
            base.OpenCSharpThenVBSolution();
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.Workspace)]
        [TestProperty(Traits.Feature, Traits.Features.NetCore)]
        public override void MetadataReference()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.EditProjectFile(project);
            VisualStudioInstance.Editor.SetText(@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net461</TargetFramework>
  </PropertyGroup>
</Project>");
            VisualStudioInstance.SolutionExplorer.SaveAll();
            VisualStudioInstance.SolutionExplorer.RestoreNuGetPackages(project);
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Class1.cs");
            base.MetadataReference();
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.Workspace)]
        public override void ProjectReference()
        {
            base.ProjectReference();
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.Workspace)]
        [TestProperty(Traits.Feature, Traits.Features.NetCore)]
        public override void ProjectProperties()
        {
            VisualStudioInstance.SolutionExplorer.CreateSolution(nameof(WorkspacesDesktop));
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.AddProject(project, WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic);
            VisualStudioInstance.SolutionExplorer.RestoreNuGetPackages(project);
            base.ProjectProperties();
        }
    }
}
