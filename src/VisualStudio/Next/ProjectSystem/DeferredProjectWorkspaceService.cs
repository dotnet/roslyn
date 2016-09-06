using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Workspace.Extensions;
using Microsoft.VisualStudio.Workspace.VSIntegration;

namespace Microsoft.VisualStudio.LanguageServices.ProjectSystem
{
    [ExportWorkspaceService(typeof(IDeferredProjectWorkspaceService)), Shared]
    internal class DeferredProjectWorkspaceService : IDeferredProjectWorkspaceService
    {
        private readonly IVsSolutionWorkspaceService _solutionWorkspaceService;

        [ImportingConstructor]
        public DeferredProjectWorkspaceService(SVsServiceProvider serviceProvider)
        {
            _solutionWorkspaceService = (IVsSolutionWorkspaceService)serviceProvider.GetService(typeof(SVsSolutionWorkspaceService));

            IsDeferredProjectLoadEnabled = ((IVsSolution7)serviceProvider.GetService(typeof(SVsSolution))).IsSolutionLoadDeferred();
        }

        public bool IsDeferredProjectLoadEnabled { get; }

        public async Task<ImmutableArray<string>> GetCommandLineArgumentsForProjectAsync(string projectFilePath)
        {
            var commandLineInfos = await _solutionWorkspaceService.GetManagedCommandLineInfoAsync(projectFilePath).ConfigureAwait(false);
            //return commandLineInfos.Any() ? commandLineInfos.First().CommandLineArgs : ImmutableArray<string>.Empty;
            if (commandLineInfos.Any())
            {
                return commandLineInfos.First().CommandLineArgs;
            }
            else
            {
                return ImmutableArray<string>.Empty;
            }
        }
    }
}
