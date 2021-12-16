// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public BasicProjectExistsUIContext(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
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
