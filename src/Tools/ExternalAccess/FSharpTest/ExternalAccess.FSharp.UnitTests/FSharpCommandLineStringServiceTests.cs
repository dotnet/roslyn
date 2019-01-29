using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.Next.UnitTests.Mocks;
using Xunit;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.UnitTests
{

    [UseExportProvider]
    public class CommandLineOptionsTests : TestBase
    {
        [Fact]
        public void SetAndRetrieveCommandLineOptions_Success()
        {
            using (var ws = new AdhocWorkspace(TestHostServices.CreateHostServices(FSharpTestExportProvider.ExportProviderWithFSharp)))
            {
                // Nothing useful on string service, just check if it exists.
                var stringService = ws.Services.GetLanguageServices(LanguageNames.FSharp).GetRequiredService<ICommandLineStringService>();

                var project = ws.AddProject("test.fsproj", LanguageNames.FSharp);

                Assert.Equal(null, project.CommandLineOptions);

                var updatedProject = project.Solution.WithProjectCommandLineOptions(project.Id, "--test").GetProject(project.Id);

                Assert.Equal("--test", updatedProject.CommandLineOptions);

                // You can set null.
                updatedProject = updatedProject.Solution.WithProjectCommandLineOptions(updatedProject.Id, null).GetProject(project.Id);

                Assert.Equal(null, updatedProject.CommandLineOptions);
            }
        }
    }
}
