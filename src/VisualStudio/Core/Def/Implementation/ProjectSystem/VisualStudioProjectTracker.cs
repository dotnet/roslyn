// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Legacy;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal sealed partial class VisualStudioProjectTracker : ForegroundThreadAffinitizedObject, IDisposable, IVisualStudioHostProjectContainer
    {
        #region Readonly fields
        private static readonly ConditionalWeakTable<SolutionId, string> s_workingFolderPathMap = new ConditionalWeakTable<SolutionId, string>();

        private readonly IServiceProvider _serviceProvider;
        private readonly IVsSolution _vsSolution;
        private readonly IVsRunningDocumentTable4 _runningDocumentTable;
        private readonly object _gate = new object();
        #endregion

        #region Mutable fields accessed only from foreground thread - don't need locking for access (all accessing methods must have AssertIsForeground).
        private readonly List<WorkspaceHostState> _workspaceHosts;

        private readonly HostWorkspaceServices _workspaceServices;

        /// <summary>
        /// The list of projects loaded in this batch between <see cref="IVsSolutionLoadEvents.OnBeforeLoadProjectBatch" /> and
        /// <see cref="IVsSolutionLoadEvents.OnAfterLoadProjectBatch(bool)"/>.
        /// </summary>
        private readonly List<AbstractProject> _projectsLoadedThisBatch = new List<AbstractProject>();

        /// <summary>
        /// Set to true while the solution is in the process of closing. That is, between
        /// <see cref="IVsSolutionEvents.OnBeforeCloseSolution"/> and <see cref="IVsSolutionEvents.OnAfterCloseSolution"/>.
        /// </summary>
        private bool _solutionIsClosing = false;

        /// <summary>
        /// Set to true once the solution has already been completely loaded and all future changes
        /// should be pushed immediately to the workspace hosts. This may not actually result in changes
        /// being pushed to a particular host if <see cref="WorkspaceHostState.HostReadyForEvents"/> isn't true yet.
        /// </summary>
        private bool _solutionLoadComplete = false;

        private uint? _solutionEventsCookie;
        #endregion

        #region Mutable fields accessed from foreground or background threads - need locking for access.
        /// <summary>
        /// This is a multi-map, only so we don't have any edge cases if people have two projects with
        /// the same output path. It makes state tracking notably easier.
        /// </summary>
        private readonly Dictionary<string, ImmutableArray<AbstractProject>> _projectsByBinPath = new Dictionary<string, ImmutableArray<AbstractProject>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Holds the task with continuations to sequentially execute all the foreground affinitized actions on the foreground task scheduler.
        /// More specifically, all the notifications to workspace hosts are executed on the foreground thread. However, the project system might make project state changes
        /// and request notifications to workspace hosts on background thread. So we queue up all the notifications for project state changes onto this task and execute them on the foreground thread.
        /// </summary>
        private Task _taskForForegroundAffinitizedActions = Task.CompletedTask;

        private readonly Dictionary<ProjectId, AbstractProject> _projectMap;
        private readonly Dictionary<string, ProjectId> _projectPathToIdMap;
        #endregion

        /// <summary>
        /// Provided to not break CodeLens which has a dependency on this API until there is a
        /// public release which calls <see cref="ImmutableProjects"/>.  Once there is, we should
        /// change this back to returning <see cref="ImmutableArray{AbstractProject}"/>, and 
        /// Obsolete <see cref="ImmutableProjects"/> instead, and then remove that after a
        /// second public release.
        /// </summary>
        [Obsolete("Use '" + nameof(ImmutableProjects) + "' instead.", true)]
        internal IEnumerable<AbstractProject> Projects => ImmutableProjects;

        internal ImmutableArray<AbstractProject> ImmutableProjects
        {
            get
            {
                lock (_gate)
                {
                    return _projectMap.Values.ToImmutableArray();
                }
            }
        }

        IReadOnlyList<IVisualStudioHostProject> IVisualStudioHostProjectContainer.GetProjects() => this.ImmutableProjects;

        void IVisualStudioHostProjectContainer.NotifyNonDocumentOpenedForProject(IVisualStudioHostProject project)
        {
            AssertIsForeground();

            var abstractProject = (AbstractProject)project;
            StartPushingToWorkspaceAndNotifyOfOpenDocuments(SpecializedCollections.SingletonEnumerable(abstractProject));
        }

        public VisualStudioProjectTracker(IServiceProvider serviceProvider, HostWorkspaceServices workspaceServices)
            : base(assertIsForeground: true)
        {
            _projectMap = new Dictionary<ProjectId, AbstractProject>();
            _projectPathToIdMap = new Dictionary<string, ProjectId>(StringComparer.OrdinalIgnoreCase);

            _serviceProvider = serviceProvider;
            _workspaceHosts = new List<WorkspaceHostState>(capacity: 1);
            _workspaceServices = workspaceServices;

            _vsSolution = (IVsSolution)serviceProvider.GetService(typeof(SVsSolution));
            _runningDocumentTable = (IVsRunningDocumentTable4)serviceProvider.GetService(typeof(SVsRunningDocumentTable));

            uint solutionEventsCookie;
            _vsSolution.AdviseSolutionEvents(this, out solutionEventsCookie);
            _solutionEventsCookie = solutionEventsCookie;

            // It's possible that we're loading after the solution has already fully loaded, so see if we missed the event
            var shellMonitorSelection = (IVsMonitorSelection)serviceProvider.GetService(typeof(SVsShellMonitorSelection));

            uint fullyLoadedContextCookie;
            if (ErrorHandler.Succeeded(shellMonitorSelection.GetCmdUIContextCookie(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_guid, out fullyLoadedContextCookie)))
            {
                int fActive;
                if (ErrorHandler.Succeeded(shellMonitorSelection.IsCmdUIContextActive(fullyLoadedContextCookie, out fActive)) && fActive != 0)
                {
                    _solutionLoadComplete = true;
                }
            }
        }

        private void ScheduleForegroundAffinitizedAction(Action action)
        {
            AssertIsBackground();

            lock (_gate)
            {
                _taskForForegroundAffinitizedActions = _taskForForegroundAffinitizedActions.SafeContinueWith(_ => action(), ForegroundTaskScheduler);
            }
        }

        /// <summary>
        /// If invoked on the foreground thread, the action is executed right away.
        /// Otherwise, the action is scheduled on foreground task scheduler.
        /// </summary>
        /// <param name="action">Action that needs to be executed on a foreground thread.</param>
        private void ExecuteOrScheduleForegroundAffinitizedAction(Action action)
        {
            if (IsForeground())
            {
                // We are already on the foreground thread, execute the given action.
                action();
            }
            else
            {
                // Schedule the update on the foreground task scheduler.
                ScheduleForegroundAffinitizedAction(action);
            }
        }

        public void RegisterSolutionProperties(SolutionId solutionId)
        {
            AssertIsForeground();

            try
            {
                var solutionWorkingFolder = (IVsSolutionWorkingFolders)_vsSolution;

                bool temporary;
                string workingFolderPath;
                solutionWorkingFolder.GetFolder(
                    (uint)__SolutionWorkingFolder.SlnWF_StatePersistence, Guid.Empty, fVersionSpecific: true, fEnsureCreated: true,
                    pfIsTemporary: out temporary, pszBstrFullPath: out workingFolderPath);

                if (!temporary && !string.IsNullOrWhiteSpace(workingFolderPath))
                {
                    s_workingFolderPathMap.Add(solutionId, workingFolderPath);
                }
            }
            catch
            {
                // don't crash just because solution having problem getting working folder information
            }
        }

        public void UpdateSolutionProperties(SolutionId solutionId)
        {
            AssertIsForeground();

            s_workingFolderPathMap.Remove(solutionId);

            RegisterSolutionProperties(solutionId);
        }

        public string GetWorkingFolderPath(Solution solution)
        {
            string workingFolderPath;
            if (s_workingFolderPathMap.TryGetValue(solution.Id, out workingFolderPath))
            {
                return workingFolderPath;
            }

            return null;
        }

        public void RegisterWorkspaceHost(IVisualStudioWorkspaceHost host)
        {
            ExecuteOrScheduleForegroundAffinitizedAction(
                () => RegisterWorkspaceHostOnForeground(host));
        }

        private void RegisterWorkspaceHostOnForeground(IVisualStudioWorkspaceHost host)
        {
            this.AssertIsForeground();

            if (_workspaceHosts.Any(hostState => hostState.Host == host))
            {
                throw new ArgumentException("The workspace host is already registered.", nameof(host));
            }

            _workspaceHosts.Add(new WorkspaceHostState(this, host));
        }

        public void StartSendingEventsToWorkspaceHost(IVisualStudioWorkspaceHost host)
        {
            AssertIsForeground();

            var hostData = _workspaceHosts.FirstOrDefault(s => s.Host == host);
            if (hostData == null)
            {
                throw new ArgumentException("The workspace host not registered", nameof(host));
            }

            // This method is idempotent.
            if (hostData.HostReadyForEvents)
            {
                return;
            }

            hostData.HostReadyForEvents = true;

            // If any of the projects are already interactive, then we better catch up the host.
            var interactiveProjects = this.ImmutableProjects.Where(p => p.PushingChangesToWorkspaceHosts);

            if (interactiveProjects.Any())
            {
                hostData.StartPushingToWorkspaceAndNotifyOfOpenDocuments(interactiveProjects);
            }
        }

        public void InitializeProviders(DocumentProvider documentProvider, VisualStudioMetadataReferenceManager metadataReferenceProvider, VisualStudioRuleSetManager ruleSetFileProvider)
        {
            AssertIsForeground();

            Contract.ThrowIfFalse(DocumentProvider == null);
            Contract.ThrowIfFalse(MetadataReferenceProvider == null);
            Contract.ThrowIfFalse(RuleSetFileProvider == null);

            DocumentProvider = documentProvider;
            MetadataReferenceProvider = metadataReferenceProvider;
            RuleSetFileProvider = ruleSetFileProvider;
        }

        public DocumentProvider DocumentProvider { get; private set; }
        public VisualStudioMetadataReferenceManager MetadataReferenceProvider { get; private set; }
        public VisualStudioRuleSetManager RuleSetFileProvider { get; private set; }

        public void Dispose()
        {
            if (_solutionEventsCookie.HasValue)
            {
                _vsSolution.UnadviseSolutionEvents(_solutionEventsCookie.Value);
                _solutionEventsCookie = null;
            }

            if (this.RuleSetFileProvider != null)
            {
                this.RuleSetFileProvider.Dispose();
            }
        }

        internal AbstractProject GetProject(ProjectId id)
        {
            lock (_gate)
            {
                AbstractProject project;
                _projectMap.TryGetValue(id, out project);
                return project;
            }
        }

        /// <summary>
        /// Add a project to the workspace.
        /// If invoked on the foreground thread, the add is executed right away.
        /// Otherwise, the add is scheduled on foreground task scheduler.
        /// </summary>
        /// <remarks>This method may be called on a background thread.</remarks>
        internal void AddProject(AbstractProject project)
        {
            ExecuteOrScheduleForegroundAffinitizedAction(() => AddProject_Foreground(project));
        }

        private void AddProject_Foreground(AbstractProject project)
        {
            AssertIsForeground();

            lock (_gate)
            {
                _projectMap.Add(project.Id, project);
            }

            // UpdateProjectBinPath is defensively executed on the foreground thread as it calls back into referencing projects to perform metadata to P2P reference conversions.
            UpdateProjectBinPath_Foreground(project, null, project.BinOutputPath);

            if (_solutionLoadComplete)
            {
                StartPushingToWorkspaceAndNotifyOfOpenDocuments_Foreground(SpecializedCollections.SingletonEnumerable(project));
            }
            else
            {
                _projectsLoadedThisBatch.Add(project);
            }
        }

        /// <summary>
        /// Starts pushing events from the given projects to the workspace hosts and notifies about open documents.
        /// If invoked on the foreground thread, it is executed right away.
        /// Otherwise, it is scheduled on foreground task scheduler.
        /// </summary>
        /// <remarks>This method may be called on a background thread.</remarks>
        internal void StartPushingToWorkspaceAndNotifyOfOpenDocuments(IEnumerable<AbstractProject> projects)
        {
            ExecuteOrScheduleForegroundAffinitizedAction(() => StartPushingToWorkspaceAndNotifyOfOpenDocuments_Foreground(projects));
        }

        private void StartPushingToWorkspaceAndNotifyOfOpenDocuments_Foreground(IEnumerable<AbstractProject> projects)
        {
            AssertIsForeground();

            using (Dispatcher.CurrentDispatcher.DisableProcessing())
            {
                foreach (var hostState in _workspaceHosts)
                {
                    hostState.StartPushingToWorkspaceAndNotifyOfOpenDocuments(projects);
                }
            }
        }

        /// <summary>
        /// Remove a project from the workspace.
        /// </summary>
        internal void RemoveProject(AbstractProject project)
        {
            ExecuteOrScheduleForegroundAffinitizedAction(() => RemoveProject_Foreground(project));
        }

        /// <summary>
        /// Remove a project from the workspace.
        /// </summary>
        private void RemoveProject_Foreground(AbstractProject project)
        {
            AssertIsForeground();

            lock (_gate)
            {
                Contract.ThrowIfFalse(_projectMap.Remove(project.Id));
            }

            UpdateProjectBinPath_Foreground(project, project.BinOutputPath, null);

            using (Dispatcher.CurrentDispatcher.DisableProcessing())
            {
                foreach (var hostState in _workspaceHosts)
                {
                    hostState.RemoveProject(project);
                }
            }
        }

        /// <summary>
        /// Updates the project tracker and referencing projects for binary output path change for the given project.
        /// If invoked on the foreground thread, the update is executed right away.
        /// Otherwise, update is scheduled on foreground task scheduler.
        /// </summary>
        /// <remarks>This method may be called on a background thread.</remarks>
        internal void UpdateProjectBinPath(AbstractProject project, string oldBinPathOpt, string newBinPathOpt)
        {
            ExecuteOrScheduleForegroundAffinitizedAction(() => UpdateProjectBinPath_Foreground(project, oldBinPathOpt, newBinPathOpt));
        }

        internal void UpdateProjectBinPath_Foreground(AbstractProject project, string oldBinPathOpt, string newBinPathOpt)
        {
            // UpdateProjectBinPath is defensively executed on the foreground thread as it calls back into referencing projects to perform metadata to P2P reference conversions.
            AssertIsForeground();

            if (oldBinPathOpt != null)
            {
                UpdateReferencesForBinPathChange(oldBinPathOpt, () => RemoveProjectByBinPath(oldBinPathOpt, project));
            }

            if (newBinPathOpt != null)
            {
                UpdateReferencesForBinPathChange(newBinPathOpt, () => AddProjectByBinPath(newBinPathOpt, project));
            }
        }

        private void UpdateReferencesForBinPathChange(string path, Action updateProjects)
        {
            AssertIsForeground();

            // If we already have a single project that points to this path, we'll either be:
            // 
            // (1) removing it, where it no longer exists, or
            // (2) adding another path, where it's now ambiguous
            //
            // in either case, we want to undo file-to-P2P reference conversion
            ImmutableArray<AbstractProject> existingProjects;

            if (TryGetProjectsByBinPath(path, out existingProjects))
            {
                if (existingProjects.Length == 1)
                {
                    foreach (var projectToUpdate in ImmutableProjects)
                    {
                        projectToUpdate.UndoProjectReferenceConversionForDisappearingOutputPath(path);
                    }
                }
            }

            updateProjects();

            if (TryGetProjectsByBinPath(path, out existingProjects))
            {
                if (existingProjects.Length == 1)
                {
                    foreach (var projectToUpdate in ImmutableProjects)
                    {
                        projectToUpdate.TryProjectConversionForIntroducedOutputPath(path, existingProjects[0]);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or creates a project ID for the given project file path and display name.
        /// </summary>
        /// <remarks>This method may be called on a background thread.</remarks>
        internal ProjectId GetOrCreateProjectIdForPath(string projectPath, string projectSystemName)
        {
            lock (_gate)
            {
                string key = projectPath + projectSystemName;
                ProjectId id;
                if (!_projectPathToIdMap.TryGetValue(key, out id))
                {
                    id = ProjectId.CreateNewId(debugName: projectPath);
                    _projectPathToIdMap[key] = id;
                }

                return id;
            }
        }

        /// <summary>
        /// Notifies the workspace host about the given action.
        /// If invoked on the foreground thread, the action is executed right away.
        /// Otherwise, the action is scheduled on foreground task scheduler.
        /// </summary>
        /// <remarks>This method may be called on a background thread.</remarks>
        internal void NotifyWorkspaceHosts(Action<IVisualStudioWorkspaceHost> action)
        {
            ExecuteOrScheduleForegroundAffinitizedAction(() => NotifyWorkspaceHosts_Foreground(action));
        }

        internal void NotifyWorkspaceHosts_Foreground(Action<IVisualStudioWorkspaceHost> action)
        {
            AssertIsForeground();

            // We do not want to allow message pumping/reentrancy when processing project system changes.
            using (Dispatcher.CurrentDispatcher.DisableProcessing())
            {
                foreach (var workspaceHost in _workspaceHosts)
                {
                    if (workspaceHost.HostReadyForEvents)
                    {
                        action(workspaceHost.Host);
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to get single project by given output binary filePath.
        /// </summary>
        /// <remarks>This method may be called on a background thread.</remarks>
        internal bool TryGetProjectByBinPath(string filePath, out AbstractProject project)
        {
            lock (_gate)
            {
                project = null;

                ImmutableArray<AbstractProject> projects;
                if (_projectsByBinPath.TryGetValue(filePath, out projects))
                {
                    // If for some reason we have more than one referencing project, it's ambiguous so bail
                    if (projects.Length == 1)
                    {
                        project = projects[0];
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Attempts to get the projects by given output binary filePath.
        /// </summary>
        /// <remarks>This method may be called on a background thread.</remarks>
        internal bool TryGetProjectsByBinPath(string filePath, out ImmutableArray<AbstractProject> projects)
        {
            lock (_gate)
            {
                if (_projectsByBinPath.TryGetValue(filePath, out projects))
                {
                    return true;
                }

                projects = ImmutableArray<AbstractProject>.Empty;
                return false;
            }
        }

        internal void AddProjectByBinPath(string filePath, AbstractProject project)
        {
            lock (_gate)
            {
                ImmutableArray<AbstractProject> projects;
                if (!_projectsByBinPath.TryGetValue(filePath, out projects))
                {
                    projects = ImmutableArray<AbstractProject>.Empty;
                }

                _projectsByBinPath[filePath] = projects.Add(project);
            }
        }

        internal void RemoveProjectByBinPath(string filePath, AbstractProject project)
        {
            lock (_gate)
            {
                ImmutableArray<AbstractProject> projects;
                if (_projectsByBinPath.TryGetValue(filePath, out projects) && projects.Contains(project))
                {
                    if (projects.Length == 1)
                    {
                        _projectsByBinPath.Remove(filePath);
                    }
                    else
                    {
                        _projectsByBinPath[filePath] = projects.Remove(project);
                    }
                }
            }
        }

        internal void TryDisconnectExistingDeferredProject(IVsHierarchy hierarchy, string projectName)
        {
            var projectPath = AbstractLegacyProject.GetProjectFilePath(hierarchy);
            var projectId = GetOrCreateProjectIdForPath(projectPath, projectName);

            // If we created a project for this while in deferred project load mode, let's close it
            // now that we're being asked to make a "real" project for it, so that we'll prefer the
            // "real" project
            if (IsDeferredSolutionLoadEnabled())
            {
                var existingProject = GetProject(projectId);
                Debug.Assert(existingProject is IWorkspaceProjectContext);
                existingProject?.Disconnect();
            }
        }

        private bool IsDeferredSolutionLoadEnabled()
        {
            // NOTE: It is expected that the "as" will fail on Dev14, as IVsSolution7 was
            // introduced in Dev15.  Be sure to handle the null result here.
            var solution7 = _serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution7;
            return solution7?.IsSolutionLoadDeferred() == true;
        }
    }
}
