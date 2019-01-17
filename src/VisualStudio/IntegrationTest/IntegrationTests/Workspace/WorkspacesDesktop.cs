// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.Workspace
{
    [TestClass]
    public class WorkspacesDesktop : WorkspaceBase
    {
        public WorkspacesDesktop()
            : base(WellKnownProjectTemplates.ClassLibrary)
        {
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.Workspace)]
        public override void OpenCSharpThenVBSolution()
        {
            base.OpenCSharpThenVBSolution();
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.Workspace)]
        public override void MetadataReference()
        {
            base.MetadataReference();
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.Workspace)]
        public override void ProjectReference()
        {
            base.ProjectReference();
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.Workspace)]
        public override void ProjectProperties()
        {
            VisualStudioInstance.SolutionExplorer.CreateSolution(nameof(WorkspacesDesktop));
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.AddProject(project, WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic);
            base.ProjectProperties();
        }
    }
}
