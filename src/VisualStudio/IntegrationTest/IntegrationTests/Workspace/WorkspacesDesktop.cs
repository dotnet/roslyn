// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.Workspace
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class WorkspacesDesktop : WorkspaceBase
    {
        public WorkspacesDesktop()
            : base(WellKnownProjectTemplates.ClassLibrary)
        {
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public override async Task OpenCSharpThenVBSolutionAsync()
        {
            await base.OpenCSharpThenVBSolutionAsync();
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public override async Task MetadataReferenceAsync()
        {
            await base.MetadataReferenceAsync();
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public override async Task ProjectReferenceAsync()
        {
            await base.ProjectReferenceAsync();
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public override async Task ProjectPropertiesAsync()
        {
            await VisualStudio.SolutionExplorer.CreateSolutionAsync(nameof(WorkspacesDesktop));
            await VisualStudio.SolutionExplorer.AddProjectAsync(ProjectName, WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic);
            await base.ProjectPropertiesAsync();
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public override async Task RenamingOpenFilesAsync()
        {
            await base.RenamingOpenFilesAsync();
        }
    }
}
