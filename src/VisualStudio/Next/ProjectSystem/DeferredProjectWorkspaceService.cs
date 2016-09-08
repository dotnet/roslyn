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
using Roslyn.Utilities;

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

        public async Task<ValueTuple<ImmutableArray<string>, ImmutableArray<string>>> GetCommandLineArgumentsAndProjectReferencesForProjectAsync(string projectFilePath)
        {
            var commandLineInfos = await _solutionWorkspaceService.GetManagedCommandLineInfoAsync(projectFilePath).ConfigureAwait(false);

            // Due to https://devdiv.visualstudio.com/web/wi.aspx?pcguid=011b8bdf-6d56-4f87-be0d-0092136884d9&id=260811,
            // try to find any configuration that has commandlineinfo.
            var info = commandLineInfos.FirstOrDefault(cli => !cli.CommandLineArgs.IsDefaultOrEmpty);
            if (!info.CommandLineArgs.IsDefaultOrEmpty)
            {
                return ValueTuple.Create(info.CommandLineArgs, info.ProjectReferences);
            }
            else
            {
                return ValueTuple.Create(ImmutableArray<string>.Empty, ImmutableArray<string>.Empty);
            }
        }
    }
}
