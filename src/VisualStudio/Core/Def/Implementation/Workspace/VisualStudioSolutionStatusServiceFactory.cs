// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Experimentation;
using Microsoft.VisualStudio.Shell;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceServiceFactory(typeof(ISolutionStatusService), ServiceLayer.Host), Shared]
    internal class VisualStudioSolutionStatusServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioSolutionStatusServiceFactory(IThreadingContext threadingContext)
        {
            _threadingContext = threadingContext;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            if (workspaceServices.Workspace is VisualStudioWorkspace vsWorkspace)
            {
                var experimentationService = vsWorkspace.Services.GetService<IExperimentationService>();
                if (!experimentationService.IsExperimentEnabled(WellKnownExperimentNames.PartialLoadMode))
                {
                    // don't enable partial load mode for ones that are not in experiement yet
                    return SolutionStatusService.Default;
                }

                // only VSWorkspace supports partial load mode
                return new Service(_threadingContext, vsWorkspace);
            }

            return SolutionStatusService.Default;
        }

        private class Service : ISolutionStatusService
        {
            private readonly IThreadingContext _threadingContext;
            private readonly VisualStudioWorkspace _workspace;

            public Service(IThreadingContext threadingContext, VisualStudioWorkspace workspace)
            {
                // until we get new platform API, use legacy one that is not fully do what we want
                _threadingContext = threadingContext;
                _workspace = workspace;
            }

            public async System.Threading.Tasks.Task WaitForAsync(Solution solution, CancellationToken cancellationToken)
            {
                if (_workspace.Options.GetOption(ExperimentationOptions.SolutionStatusService_ForceDelay))
                {
                    await System.Threading.Tasks.Task.Delay(_workspace.Options.GetOption(ExperimentationOptions.SolutionStatusService_DelayInMS)).ConfigureAwait(false);
                }

                if (await IsFullyLoadedAsync(solution, cancellationToken).ConfigureAwait(false))
                {
                    // already fully loaded
                    return;
                }

                var taskCompletionSource = new TaskCompletionSource<object>();

                KnownUIContexts.SolutionExistsAndFullyLoadedContext.WhenActivated(() => taskCompletionSource.SetResult(null));

                await taskCompletionSource.Task.ConfigureAwait(false);
            }

            public System.Threading.Tasks.Task WaitForAsync(Project project, CancellationToken cancellationToken)
            {
                return WaitForAsync(project.Solution, cancellationToken);
            }

            public Task<bool> IsFullyLoadedAsync(Solution solution, CancellationToken cancellationToken)
            {
                return KnownUIContexts.SolutionExistsAndFullyLoadedContext.IsActive ? SpecializedTasks.True : SpecializedTasks.False;
            }

            public Task<bool> IsFullyLoadedAsync(Project project, CancellationToken cancellationToken)
            {
                return IsFullyLoadedAsync(project.Solution, cancellationToken);
            }
        }
    }
}
