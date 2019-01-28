using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.CPS;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.UnitTests
{
    internal static class FSharpHelpers
    {
        public static CPSProject CreateFSharpCPSProject(TestEnvironment environment, string projectName, string binOutputPath, params string[] commandLineArguments)
        {
            var projectFilePath = Path.GetTempPath();
            return CreateFSharpCPSProject(environment, projectName, projectFilePath, binOutputPath, projectGuid: Guid.NewGuid(), commandLineArguments: commandLineArguments);
        }

        public static CPSProject CreateFSharpCPSProject(TestEnvironment environment, string projectName, string projectFilePath, string binOutputPath, Guid projectGuid, params string[] commandLineArguments)
        {
            var hierarchy = environment.CreateHierarchy(projectName, projectFilePath, "FSharp");
            var cpsProjectFactory = environment.ExportProvider.GetExportedValue<IWorkspaceProjectContextFactory>();
            var cpsProject = (CPSProject)cpsProjectFactory.CreateProjectContext(
                LanguageNames.FSharp,
                projectName,
                projectFilePath,
                projectGuid,
                hierarchy,
                binOutputPath);

            var commandLineForOptions = string.Join(" ", commandLineArguments);
            cpsProject.SetOptions(commandLineForOptions);

            return cpsProject;
        }
    }
}
