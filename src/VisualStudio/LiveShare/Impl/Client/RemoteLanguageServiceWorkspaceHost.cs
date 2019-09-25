// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Projects;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.LiveShare;
using Microsoft.VisualStudio.Shell;
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

        // TODO: remove this project language to extension map with the switch to LSP
        private readonly ImmutableDictionary<string, string[]> _projectLanguageToExtensionMap;
        private readonly SVsServiceProvider _serviceProvider;
        private readonly IThreadingContext _threadingContext;

        public Workspace Workspace => _remoteLanguageServiceWorkspace;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteLanguageServiceWorkspaceHost"/> class.
        /// </summary>
        /// <param name="remoteLanguageServiceWorkspace">The workspace</param>
        [ImportingConstructor]
        public RemoteLanguageServiceWorkspaceHost(RemoteLanguageServiceWorkspace remoteLanguageServiceWorkspace,
                                                  RemoteProjectInfoProvider remoteProjectInfoProvider,
                                                  SVsServiceProvider serviceProvider,
                                                  IThreadingContext threadingContext)
        {
            _remoteLanguageServiceWorkspace = Requires.NotNull(remoteLanguageServiceWorkspace, nameof(remoteLanguageServiceWorkspace));
            _remoteProjectInfoProvider = Requires.NotNull(remoteProjectInfoProvider, nameof(remoteProjectInfoProvider));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _threadingContext = Requires.NotNull(threadingContext, nameof(threadingContext));

            var builder = ImmutableDictionary.CreateBuilder<string, string[]>(StringComparer.OrdinalIgnoreCase);
            builder.Add("TypeScript", new string[] { ".js", ".jsx", ".ts", ".tsx" });
            builder.Add("C#_Remote", new string[] { ".cs" });
            _projectLanguageToExtensionMap = builder.ToImmutable();
        }

        public async Task<ICollaborationService> CreateServiceAsync(CollaborationSession collaborationSession, CancellationToken cancellationToken)
        {
            await _remoteLanguageServiceWorkspace.SetSession(collaborationSession).ConfigureAwait(false);

            await InitOptionsAsync(cancellationToken).ConfigureAwait(false);
            _remoteLanguageServiceWorkspace.Init();

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

        public VisualStudioProjectTracker ProjectTracker { get; private set; }

        public async Task InitializeProjectTrackerAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

#pragma warning disable CS0618 // Type or member is obsolete.  This is the new liveshare layer.
            ProjectTracker = new VisualStudioProjectTracker(_serviceProvider, _remoteLanguageServiceWorkspace);


            var documentProvider = (DocumentProvider)Activator.CreateInstance(typeof(DocumentProvider));
            var metadataReferenceProvider = _remoteLanguageServiceWorkspace.Services.GetService<VisualStudioMetadataReferenceManager>();
            var ruleSetFileProvider = _remoteLanguageServiceWorkspace.Services.GetService<VisualStudioRuleSetManager>();
            ProjectTracker.InitializeProviders(documentProvider, metadataReferenceProvider, ruleSetFileProvider);
            ProjectTracker.StartPushingToWorkspaceAndNotifyOfOpenDocuments(Enumerable.Empty<AbstractProject>());
#pragma warning restore CS0618 // Type or member is obsolete.  This is the new liveshare layer.
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

        /// <summary>
        /// Initialize the options.  This must be done on the UI thread because
        /// multiple <see cref="IOptionPersister"/> are required to be initialized on the UI thread.
        /// Typically this is done by <see cref="RoslynPackage"/> but this is not to guaranteed
        /// to procede liveshare initialization.
        /// TODO - https://github.com/dotnet/roslyn/issues/37377
        /// </summary>
        private async Task InitOptionsAsync(CancellationToken cancellationToken)
        {
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var componentModel = (IComponentModel)_serviceProvider.GetService(typeof(SComponentModel));
            // Ensure the options persisters are loaded since we have to fetch options from the shell
            componentModel.GetExtensions<IOptionPersister>();
        }

        /// <summary>
        /// Loads (or reloads) the corresponding Roslyn project and the direct referenced projects in the host environment.
        /// </summary>
        private async Task LoadProjectsAsync(CancellationToken cancellationToken)
        {
            try
            {
                await InitializeProjectTrackerAsync().ConfigureAwait(false);
                var projectInfos = await _remoteProjectInfoProvider.GetRemoteProjectInfosAsync(cancellationToken).ConfigureAwait(false);
                foreach (var projectInfo in projectInfos)
                {
                    var projectName = projectInfo.Name;
                    if (!_loadedProjects.TryGetValue(projectName, out ProjectId projectId))
                    {
                        projectId = projectInfo.Id;

                        // Adds the Roslyn project into the current solution;
                        // and raise WorkspaceChanged event (WorkspaceChangeKind.ProjectAdded)
                        _remoteLanguageServiceWorkspace.OnManagedProjectAdded(projectInfo);

                        _loadedProjects = _loadedProjects.Add(projectName, projectId);
                        _loadedProjectInfo = _loadedProjectInfo.Add(projectName, projectInfo);

                        // TODO : figure out what changes we need to listen to.
                    }
                    else
                    {
                        if (_loadedProjectInfo.TryGetValue(projectName, out ProjectInfo projInfo))
                        {
                            _remoteLanguageServiceWorkspace.OnManagedProjectReloaded(projInfo);
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
            {
                Disposed?.Invoke(this, null);
            }
        }
    }
}
