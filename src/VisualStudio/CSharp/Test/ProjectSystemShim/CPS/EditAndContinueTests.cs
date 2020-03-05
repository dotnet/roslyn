using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue.Interop;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim.CPS
{
    [UseExportProvider]
    public class EditAndContinueTests
    {
        [WpfFact, WorkItem(31034, "https://github.com/dotnet/roslyn/issues/31034")]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void EditAndContinueInterfacesAreImplemented()
        {
            using var environment = new TestEnvironment();
            using var project = CSharpHelpers.CreateCSharpCPSProject(environment, "Test", binOutputPath: null);
            Assert.IsAssignableFrom<IVsENCRebuildableProjectCfg2>(project);
            Assert.IsAssignableFrom<IVsENCRebuildableProjectCfg4>(project);
        }
    }
}
