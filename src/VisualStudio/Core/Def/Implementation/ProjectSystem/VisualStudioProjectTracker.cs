// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal sealed partial class VisualStudioProjectTracker : ForegroundThreadAffinitizedObject
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

        /// <summary>
        /// Set to true while we're batching project loads. That is, between
        /// <see cref="IVsSolutionLoadEvents.OnBeforeLoadProjectBatch" /> and
        /// <see cref="IVsSolutionLoadEvents.OnAfterLoadProjectBatch"/>.
        /// </summary>
        private bool _batchingProjectLoads = false;

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
        #endregion

        #region Mutable fields accessed from foreground or background threads - need locking for access.
        /// <summary>
        /// This is a multi-map, only so we don't have any edge cases if people have two projects with
        /// the same output path. It makes state tracking notably easier.
        /// </summary>
        private readonly Dictionary<string, ImmutableArray<AbstractProject>> _projectsByBinPath = new Dictionary<string, ImmutableArray<AbstractProject>>(StringComparer.OrdinalIgnoreCase);

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

        internal HostWorkspaceServices WorkspaceServices { get; }

        internal void NotifyNonDocumentOpenedForProject(AbstractProject project)
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
            WorkspaceServices = workspaceServices;

            _vsSolution = (IVsSolution)serviceProvider.GetService(typeof(SVsSolution));
            _runningDocumentTable = (IVsRunningDocumentTable4)serviceProvider.GetService(typeof(SVsRunningDocumentTable));

            // It's possible that we're loading after the solution has already fully loaded, so see if we missed the event
            var shellMonitorSelection = (IVsMonitorSelection)serviceProvider.GetService(typeof(SVsShellMonitorSelection));
            if (ErrorHandler.Succeeded(shellMonitorSelection.GetCmdUIContextCookie(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_guid, out var fullyLoadedContextCookie)))
            {
                if (ErrorHandler.Succeeded(shellMonitorSelection.IsCmdUIContextActive(fullyLoadedContextCookie, out var fActive)) && fActive != 0)
                {
                    _solutionLoadComplete = true;
                }
            }
        }

        public void RegisterSolutionProperties(SolutionId solutionId)
        {
            AssertIsForeground();

            try
            {
                var solutionWorkingFolder = (IVsSolutionWorkingFolders)_vsSolution;
                solutionWorkingFolder.GetFolder(
                    (uint)__SolutionWorkingFolder.SlnWF_StatePersistence, Guid.Empty, fVersionSpecific: true, fEnsureCreated: true,
                    pfIsTemporary: out var temporary, pszBstrFullPath: out var workingFolderPath);

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
            if (s_workingFolderPathMap.TryGetValue(solution.Id, out var workingFolderPath))
            {
                return workingFolderPath;
            }

            return null;
        }

        public void RegisterWorkspaceHost(IVisualStudioWorkspaceHost host)
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

        internal AbstractProject GetProject(ProjectId id)
        {
            lock (_gate)
            {
                _projectMap.TryGetValue(id, out var project);
                return project;
            }
        }

        internal bool ContainsProject(AbstractProject project)
        {
            lock (_gate)
            {
                return _projectMap.ContainsKey(project.Id);
            }
        }

        /// <summary>
        /// Add a project to the workspace.
        /// </summary>
        /// <remarks>This method must be called on the foreground thread.</remarks>
        internal void AddProject(AbstractProject project)
        {
            AssertIsForeground();

            lock (_gate)
            {
                _projectMap.Add(project.Id, project);
            }

            // UpdateProjectBinPath is defensively executed on the foreground thread as it calls back into referencing projects to perform metadata to P2P reference conversions.
            UpdateProjectBinPath(project, null, project.BinOutputPath);

            if (_solutionLoadComplete)
            {
                StartPushingToWorkspaceAndNotifyOfOpenDocuments(SpecializedCollections.SingletonEnumerable(project));
            }
            else if (_batchingProjectLoads)
            {
                _projectsLoadedThisBatch.Add(project);
            }
        }

        /// <summary>
        /// Starts pushing events from the given projects to the workspace hosts and notifies about open documents.
        /// </summary>
        /// <remarks>This method must be called on the foreground thread.</remarks>
        internal void StartPushingToWorkspaceAndNotifyOfOpenDocuments(IEnumerable<AbstractProject> projects)
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
            AssertIsForeground();

            lock (_gate)
            {
                Contract.ThrowIfFalse(_projectMap.Remove(project.Id));
            }

            UpdateProjectBinPath(project, project.BinOutputPath, null);

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
        /// </summary>
        internal void UpdateProjectBinPath(AbstractProject project, string oldBinPathOpt, string newBinPathOpt)
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

            if (TryGetProjectsByBinPath(path, out var existingProjects))
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
                if (!_projectPathToIdMap.TryGetValue(key, out var id))
                {
                    id = ProjectId.CreateNewId(debugName: projectPath);
                    _projectPathToIdMap[key] = id;
                }

                return id;
            }
        }

        /// <summary>
        /// Notifies the workspace host about the given action.
        /// </summary>
        /// <remarks>This method must be called on the foreground thread.</remarks>
        internal void NotifyWorkspaceHosts(Action<IVisualStudioWorkspaceHost> action)
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
                if (!_projectsByBinPath.TryGetValue(filePath, out var projects))
                {
                    // Workaround https://github.com/dotnet/roslyn/issues/20412 by checking to see if */ref/A.dll can be
                    // adjusted to */A.dll - only handles the default location for reference assemblies during a build.
                    if (!HACK_StripRefDirectoryFromPath(filePath, out string binFilePath)
                        || !_projectsByBinPath.TryGetValue(binFilePath, out projects))
                    {
                        return false;
                    }
                }

                // If for some reason we have more than one referencing project, it's ambiguous so bail
                if (projects.Length == 1)
                {
                    project = projects[0];
                    return true;
                }

                return false;
            }
        }

        private static readonly char[] s_directorySeparatorChars = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        private bool HACK_StripRefDirectoryFromPath(string filePath, out string binFilePath)
        {
            const string refDirectoryName = "ref";

            // looking for "/ref/" where:
            // 1. the first / is a directory separator
            // 2. 'ref' matches in a case-insensitive comparison
            // 3. the second / is the last directory separator
            var lastSeparator = filePath.LastIndexOfAny(s_directorySeparatorChars);
            var secondToLastSeparator = lastSeparator - refDirectoryName.Length - 1;
            if (secondToLastSeparator < 0)
            {
                // Failed condition 3
                binFilePath = null;
                return false;
            }

            if (filePath[secondToLastSeparator] != Path.DirectorySeparatorChar
                && filePath[secondToLastSeparator] != Path.AltDirectorySeparatorChar)
            {
                // Failed condition 1
                binFilePath = null;
                return false;
            }

            if (string.Compare(refDirectoryName, 0, filePath, secondToLastSeparator + 1, lastSeparator - secondToLastSeparator - 1, StringComparison.OrdinalIgnoreCase) != 0)
            {
                // Failed condition 2
                binFilePath = null;
                return false;
            }

            binFilePath = filePath.Remove(secondToLastSeparator, lastSeparator - secondToLastSeparator);
            return true;
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
                if (!_projectsByBinPath.TryGetValue(filePath, out var projects))
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
                if (_projectsByBinPath.TryGetValue(filePath, out var projects) && projects.Contains(project))
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

        public void OnBeforeCloseSolution()
        {
            AssertIsForeground();

            _solutionIsClosing = true;

            foreach (var p in this.ImmutableProjects)
            {
                p.StopPushingToWorkspaceHosts();
            }

            _solutionLoadComplete = false;
        }

        public void OnAfterCloseSolution()
        {
            AssertIsForeground();

            lock (_gate)
            {
                Contract.ThrowIfFalse(_projectMap.Count == 0);
            }

            NotifyWorkspaceHosts(host => host.OnSolutionRemoved());
            NotifyWorkspaceHosts(host => host.ClearSolution());

            lock (_gate)
            {
                _projectPathToIdMap.Clear();
            }

            RuleSetFileProvider.ClearCachedRuleSetFiles();

            foreach (var workspaceHost in _workspaceHosts)
            {
                workspaceHost.SolutionClosed();
            }

            _solutionIsClosing = false;
        }

        private void FinishLoad()
        {
            // We are now completely done, so let's simply ensure all projects are added.
            StartPushingToWorkspaceAndNotifyOfOpenDocuments(this.ImmutableProjects);

            // Also, all remaining project adds need to immediately pushed as well, since we're now "interactive"
            _solutionLoadComplete = true;

            // Check that the set of analyzers is complete and consistent.
            GetAnalyzerDependencyCheckingService()?.CheckForConflictsAsync();
        }

        private AnalyzerDependencyCheckingService GetAnalyzerDependencyCheckingService()
        {
            var componentModel = (IComponentModel)_serviceProvider.GetService(typeof(SComponentModel));

            return componentModel.GetService<AnalyzerDependencyCheckingService>();
        }

        internal void OnBeforeLoadProjectBatch(bool fIsBackgroundIdleBatch)
        {
            AssertIsForeground();

            _batchingProjectLoads = true;
            _projectsLoadedThisBatch.Clear();
        }

        internal void OnAfterLoadProjectBatch(bool fIsBackgroundIdleBatch)
        {
            AssertIsForeground();

            if (!fIsBackgroundIdleBatch)
            {
                // This batch was loaded eagerly. This might be because the user is force expanding the projects in the
                // Solution Explorer, or they had some files open in an .suo we need to push.
                StartPushingToWorkspaceAndNotifyOfOpenDocuments(_projectsLoadedThisBatch);
            }

            _batchingProjectLoads = false;
            _projectsLoadedThisBatch.Clear();
        }

        internal void OnAfterBackgroundSolutionLoadComplete()
        {
            AssertIsForeground();

            // In Non-DPL scenarios, this indicates that ASL is complete, and we should push any
            // remaining information we have to the Workspace.  If DPL is enabled, this is never
            // called.
            FinishLoad();
        }
    }
}
