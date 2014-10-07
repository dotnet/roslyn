using System;
using System.ComponentModel.Composition;
using Roslyn.Services;

namespace Roslyn.Services.Host
{
    [ExportWorkspaceServiceFactory(typeof(ISolutionFactoryService), WorkspaceKind.Any)]
    internal class SolutionFactoryServiceFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(IWorkspaceServiceProvider workspaceServices)
        {
            return new SolutionFactoryService(workspaceServices);
        }

        public class SolutionFactoryService : ISolutionFactoryService
        {
            private readonly IWorkspaceServiceProvider workspaceServices;

            public SolutionFactoryService(
                IWorkspaceServiceProvider workspaceServices)
            {
                this.workspaceServices = workspaceServices;
            }

            public ISolution CreateSolution(SolutionId id)
            {
                if (id == null)
                {
                    throw new ArgumentNullException("id");
                }

                return new Solution(
                    id,
                    filePath: null,
                    version: VersionStamp.Create(),  // use a new version
                    latestProjectVersion: VersionStamp.Default,   // no projects yet, so default
                    workspaceServices: this.workspaceServices);
            }

            public ISolution CreateSolution(ISolutionInfo solutionInfo)
            {
                if (solutionInfo == null)
                {
                    throw new ArgumentNullException("solutionInfo");
                }

                return new Solution(
                    solutionInfo,
                    workspaceServices: this.workspaceServices);
            }
        }
    }
}