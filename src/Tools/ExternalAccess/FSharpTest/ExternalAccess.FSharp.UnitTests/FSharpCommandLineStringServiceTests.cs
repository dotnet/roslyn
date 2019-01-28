using System;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Xunit;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Composition.Reflection;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using System.Linq;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.UnitTests
{

    [UseExportProvider]
    public class CommandLineOptionsTests : TestBase
    {
        [Fact]
        public void SetAndRetrieveCommandLineOptions_Success()
        {
            using (var ws = new AdhocWorkspace(VisualStudioMefHostServices.Create(FSharpTestExportProvider.ExportProviderWithFSharp)))
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
