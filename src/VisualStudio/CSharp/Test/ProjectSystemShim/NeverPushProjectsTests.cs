using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim
{
    public class NeverPushProjectsTests
    {
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void NeverPushProjectsDoesNotPushProjects()
        {
            using (var environment = new TestEnvironment(neverPushProjects: true))
            {
                var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

                // This should be empty since we opted out
                Assert.Empty(environment.Workspace.CurrentSolution.Projects);

                // Mutate the project so we don't crash when not pushing projects
                project.OnImportAdded("Z:\\Reference.dll", "Reference");

                project.Disconnect();
            }
        }
    }
}
