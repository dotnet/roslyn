using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
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

        public async Task<IReadOnlyDictionary<string, DeferredProjectInformation>> GetCommandLineArgumentsAndProjectReferencesForProjectAsync(
            string solutionConfiguration,
            CancellationToken cancellationToken)
        {
            var commandLineInfos = await _solutionWorkspaceService.GetManagedCommandLineInfoAsync(
                solutionConfiguration, cancellationToken).ConfigureAwait(false);

            var result = ImmutableDictionary.CreateRange(
                commandLineInfos.Select(kvp => KeyValuePair.Create(
                    kvp.Key,
                    new DeferredProjectInformation(
                        kvp.Value.TargetPath,
                        kvp.Value.CommandLineArgs,
                        kvp.Value.ProjectReferences))));
            return result;
        }
    }
}
