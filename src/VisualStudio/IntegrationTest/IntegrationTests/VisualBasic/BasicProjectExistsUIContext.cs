// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using Guids = Microsoft.VisualStudio.LanguageServices.Guids;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicProjectExistsUIContext : AbstractIntegrationTest
    {
        public BasicProjectExistsUIContext(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper)
        {
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(true);
            VisualStudio.SolutionExplorer.CreateSolution(nameof(BasicProjectExistsUIContext));
        }

        [WpfFact]
        public void ProjectContextChanges()
        {
            Assert.False(VisualStudio.Shell.IsUIContextActive(Guids.VisualBasicProjectExistsInWorkspaceUIContext));

            VisualStudio.SolutionExplorer.AddProject(new ProjectUtils.Project("TestVisualBasicProject"), WellKnownProjectTemplates.ConsoleApplication, LanguageNames.VisualBasic);

            Assert.True(VisualStudio.Shell.IsUIContextActive(Guids.VisualBasicProjectExistsInWorkspaceUIContext));

            VisualStudio.SolutionExplorer.CloseSolution();

            Assert.False(VisualStudio.Shell.IsUIContextActive(Guids.VisualBasicProjectExistsInWorkspaceUIContext));
        }
    }
}
