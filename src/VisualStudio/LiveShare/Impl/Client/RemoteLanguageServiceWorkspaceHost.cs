// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Projects;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.LiveShare;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
{
    /// <summary>
    /// Remote language service workspace host
    /// </summary>
    [Export(typeof(RemoteLanguageServiceWorkspaceHost))]
    [ExportCollaborationService(typeof(RemoteLanguageServiceSession),
                                Scope = SessionScope.Guest,
                                Role = ServiceRole.LocalService,
                                Features = "LspServices",
                                CreationPriority = (int)ServiceRole.LocalService + 2100)]

    internal sealed class RemoteLanguageServiceWorkspaceHost : ICollaborationServiceFactory
    {
        // A collection of loaded Roslyn Project IDs, indexed by project path.
        private ImmutableDictionary<string, ProjectId> _loadedProjects = ImmutableDictionary.Create<string, ProjectId>(StringComparer.OrdinalIgnoreCase);
        private ImmutableDictionary<string, ProjectInfo> _loadedProjectInfo = ImmutableDictionary.Create<string, ProjectInfo>(StringComparer.OrdinalIgnoreCase);
        private TaskCompletionSource<bool> _projectsLoadedTaskCompletionSource = new TaskCompletionSource<bool>();
        private readonly RemoteLanguageServiceWorkspace _remoteLanguageServiceWorkspace;
        private readonly RemoteProjectInfoProvider _remoteProjectInfoProvider;

        private readonly SVsServiceProvider _serviceProvider;
        private readonly IThreadingContext _threadingContext;

        public RemoteLanguageServiceWorkspace Workspace => _remoteLanguageServiceWorkspace;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteLanguageServiceWorkspaceHost"/> class.
        /// </summary>
        /// <param name="remoteLanguageServiceWorkspace">The workspace</param>
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteLanguageServiceWorkspaceHost(RemoteLanguageServiceWorkspace remoteLanguageServiceWorkspace,
                                                  RemoteProjectInfoProvider remoteProjectInfoProvider,
                                                  SVsServiceProvider serviceProvider,
                                                  IThreadingContext threadingContext)
        {
            _remoteLanguageServiceWorkspace = Requires.NotNull(remoteLanguageServiceWorkspace, nameof(remoteLanguageServiceWorkspace));
            _remoteProjectInfoProvider = Requires.NotNull(remoteProjectInfoProvider, nameof(remoteProjectInfoProvider));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _threadingContext = Requires.NotNull(threadingContext, nameof(threadingContext));
        }

        public async Task<ICollaborationService> CreateServiceAsync(CollaborationSession collaborationSession, CancellationToken cancellationToken)
        {
            await LoadRoslynPackageAsync(cancellationToken).ConfigureAwait(false);

            await _remoteLanguageServiceWorkspace.SetSessionAsync(collaborationSession).ConfigureAwait(false);

            // Kick off loading the projects in the background.
            // Clients can call EnsureProjectsLoadedAsync to await completion.
            LoadProjectsAsync(CancellationToken.None).Forget();

            var lifeTimeService = new RemoteLanguageServiceSession();
            lifeTimeService.Disposed += (s, e) =>
            {
                _remoteLanguageServiceWorkspace.EndSession();
                CloseAllProjects();
                _remoteLanguageServiceWorkspace.Dispose();
                _projectsLoadedTaskCompletionSource = new TaskCompletionSource<bool>();
            };

            return lifeTimeService;
        }

        /// <summary>
        /// Ensures LoadProjectsAsync has completed
        /// </summary>
        public async Task EnsureProjectsLoadedAsync(CancellationToken cancellationToken)
        {
            using (var token = cancellationToken.Register(() =>
            {
                _projectsLoadedTaskCompletionSource.SetCanceled();
            }))
            {
                await _projectsLoadedTaskCompletionSource.Task.ConfigureAwait(false);
            }
        }

        private async Task LoadRoslynPackageAsync(CancellationToken cancellationToken)
        {
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Explicitly trigger the load of the Roslyn package. This ensures that UI-bound services are appropriately prefetched,
            // that FatalError is correctly wired up, etc. Ideally once the things happening in the package initialize are cleaned up with
            // better patterns, this would go away.
            var shellService = (IVsShell7)_serviceProvider.GetService(typeof(SVsShell));
            await shellService.LoadPackageAsync(Guids.RoslynPackageId);
        }

        /// <summary>
        /// Loads (or reloads) the corresponding Roslyn project and the direct referenced projects in the host environment.
        /// </summary>
        private async Task LoadProjectsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var projectInfos = await _remoteProjectInfoProvider.GetRemoteProjectInfosAsync(cancellationToken).ConfigureAwait(false);
                foreach (var projectInfo in projectInfos)
                {
                    var projectName = projectInfo.Name;
                    if (!_loadedProjects.TryGetValue(projectName, out var projectId))
                    {
                        projectId = projectInfo.Id;

                        // Adds the Roslyn project into the current solution;
                        // and raise WorkspaceChanged event (WorkspaceChangeKind.ProjectAdded)
                        _remoteLanguageServiceWorkspace.OnProjectAdded(projectInfo);

                        _loadedProjects = _loadedProjects.Add(projectName, projectId);
                        _loadedProjectInfo = _loadedProjectInfo.Add(projectName, projectInfo);

                        // TODO : figure out what changes we need to listen to.
                    }
                    else
                    {
                        if (_loadedProjectInfo.TryGetValue(projectName, out var projInfo))
                        {
                            _remoteLanguageServiceWorkspace.OnProjectReloaded(projectInfo);
                        }
                    }
                }

                _projectsLoadedTaskCompletionSource.SetResult(true);
            }
            catch (Exception ex)
            {
                _projectsLoadedTaskCompletionSource.SetException(ex);
            }
        }

        private void CloseAllProjects()
        {
            foreach (var projectId in _loadedProjects.Values)
            {
                _remoteLanguageServiceWorkspace.OnProjectRemoved(projectId);
            }
            _loadedProjects = _loadedProjects.Clear();
            _loadedProjectInfo = _loadedProjectInfo.Clear();
        }

        private class RemoteLanguageServiceSession : ICollaborationService, IDisposable
        {
            public event EventHandler Disposed;

            public void Dispose()
                => Disposed?.Invoke(this, null);
        }
    }
}
