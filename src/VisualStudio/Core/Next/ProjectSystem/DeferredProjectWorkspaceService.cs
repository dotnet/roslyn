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
        private readonly Lazy<IVsSolutionWorkspaceService> _solutionWorkspaceService;

        [ImportingConstructor]
        public DeferredProjectWorkspaceService(SVsServiceProvider serviceProvider)
        {
            _solutionWorkspaceService = new Lazy<IVsSolutionWorkspaceService>(
                () => (IVsSolutionWorkspaceService)serviceProvider.GetService(typeof(SVsSolutionWorkspaceService)));
        }

        public async Task<IReadOnlyDictionary<string, DeferredProjectInformation>> GetDeferredProjectInfoForConfigurationAsync(
            string solutionConfiguration,
            CancellationToken cancellationToken)
        {
            var commandLineInfos = await _solutionWorkspaceService.Value.GetManagedCommandLineInfoAsync(
                solutionConfiguration, cancellationToken).ConfigureAwait(false);

            // NOTE: Anycode gives us the project references as if they were command line arguments with
            // "/ProjectReference:" prepended.  Strip that off here.
            var result = ImmutableDictionary.CreateRange(
                commandLineInfos.Select(kvp => KeyValuePair.Create(
                    kvp.Key,
                    new DeferredProjectInformation(
                        kvp.Value.TargetPath,
                        kvp.Value.CommandLineArgs,
                        kvp.Value.ProjectReferences.Select(p => p.Substring("/ProjectReference:".Length)).ToImmutableArray()))));
            return result;
        }
    }
}
