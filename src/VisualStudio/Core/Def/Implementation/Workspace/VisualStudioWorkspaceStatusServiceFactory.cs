// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Experimentation;
using Microsoft.VisualStudio.Shell;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceServiceFactory(typeof(IWorkspaceStatusService), ServiceLayer.Host), Shared]
    internal class VisualStudioWorkspaceStatusServiceFactory : IWorkspaceServiceFactory
    {
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioWorkspaceStatusServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            if (workspaceServices.Workspace is VisualStudioWorkspace vsWorkspace)
            {
                var experimentationService = vsWorkspace.Services.GetService<IExperimentationService>();
                if (!experimentationService.IsExperimentEnabled(WellKnownExperimentNames.PartialLoadMode))
                {
                    // don't enable partial load mode for ones that are not in experiment yet
                    return WorkspaceStatusService.Default;
                }

                // only VSWorkspace supports partial load mode
                return new Service(vsWorkspace);
            }

            return WorkspaceStatusService.Default;
        }

        /// <summary>
        /// for prototype, we won't care about what solution is actually fully loaded. 
        /// we will just see whatever solution VS has at this point of time has actually fully loaded
        /// </summary>
        private class Service : IWorkspaceStatusService
        {
            private readonly VisualStudioWorkspace _workspace;

            public Service(VisualStudioWorkspace workspace)
            {
                // until we get new platform API, use legacy one that is not fully do what we want
                _workspace = workspace;
            }

            public async System.Threading.Tasks.Task WaitUntilFullyLoadedAsync(CancellationToken cancellationToken)
            {
                if (_workspace.Options.GetOption(ExperimentationOptions.SolutionStatusService_ForceDelay))
                {
                    await System.Threading.Tasks.Task.Delay(_workspace.Options.GetOption(ExperimentationOptions.SolutionStatusService_DelayInMS)).ConfigureAwait(false);
                }

                if (await IsFullyLoadedAsync(cancellationToken).ConfigureAwait(false))
                {
                    // already fully loaded
                    return;
                }

                var taskCompletionSource = new TaskCompletionSource<object>();

                // we are using this API for now, until platform provide us new API for prototype
                KnownUIContexts.SolutionExistsAndFullyLoadedContext.WhenActivated(() => taskCompletionSource.SetResult(null));

                await taskCompletionSource.Task.ConfigureAwait(false);
            }

            public Task<bool> IsFullyLoadedAsync(CancellationToken cancellationToken)
            {
                // we are using this API for now, until platform provide us new API for prototype
                return KnownUIContexts.SolutionExistsAndFullyLoadedContext.IsActive ? SpecializedTasks.True : SpecializedTasks.False;
            }
        }
    }
}
