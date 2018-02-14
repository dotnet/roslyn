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
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;
using Microsoft.VisualStudio.LanguageServices.Storage;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal sealed partial class VisualStudioProjectTracker : ForegroundThreadAffinitizedObject
    {
        #region Readonly fields

        /// <summary>
        /// The underlying workspace. This is an instance of <see cref="VisualStudioWorkspaceImpl"/> in Visual Studio, but
        /// of TestWorkpace if we're in tests. Type checking should be avoided for the most part.
        /// </summary>
        private readonly Workspace _workspace;
        private readonly IServiceProvider _serviceProvider;
        private readonly IVsSolution _vsSolution;
        private readonly IVsRunningDocumentTable4 _runningDocumentTable;
        private readonly object _gate = new object();
        #endregion

        #region Mutable fields accessed only from foreground thread - don't need locking for access (all accessing methods must have AssertIsForeground).

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
        /// should be pushed immediately to the workspace hosts.
        /// </summary>
        private bool _solutionLoadComplete = false;

        /// <summary>
        /// Set to true if we've already called <see cref="Workspace.OnSolutionAdded(Microsoft.CodeAnalysis.SolutionInfo)"/>. Set to false after the solution has closed.
        /// </summary>
        private bool _solutionAdded;

        /// <summary>
        /// The projects that have already been added to the workspace.
        /// </summary>
        private readonly HashSet<AbstractProject> _pushedProjects = new HashSet<AbstractProject>();

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

        public VisualStudioProjectTracker(IServiceProvider serviceProvider, Workspace workspace)
            : base(assertIsForeground: true)
        {
            _projectMap = new Dictionary<ProjectId, AbstractProject>();
            _projectPathToIdMap = new Dictionary<string, ProjectId>(StringComparer.OrdinalIgnoreCase);

            _serviceProvider = serviceProvider;
            _workspace = workspace;
            WorkspaceServices = workspace.Services;

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

            // If the solution is closing we shouldn't do anything, because all of our state is
            // in the process of going away. This can happen if we receive notification that a document has
            // opened in the middle of the solution close operation.
            if (_solutionIsClosing)
            {
                return;
            }

            // We need to push these projects and any project dependencies we already know about. Therefore, compute the
            // transitive closure of the projects that haven't already been pushed, keeping them in appropriate order.
            var visited = new HashSet<AbstractProject>();
            var inOrderToPush = new List<AbstractProject>();

            void addToInOrderToPush(AbstractProject project)
            {
                Contract.ThrowIfFalse(ContainsProject(project));

                // Bail out if any of the following is true:
                //  1. We have already started pushing changes for this project OR
                //  2. We have already visited this project in a prior recursive call                
                if (_pushedProjects.Contains(project) || !visited.Add(project))
                {
                    return;
                }

                foreach (var projectReference in project.GetCurrentProjectReferences())
                {
                    addToInOrderToPush(GetProject(projectReference.ProjectId));
                }

                inOrderToPush.Add(project);
            }

            foreach (var project in projects)
            {
                addToInOrderToPush(project);
            }

            var projectInfos = inOrderToPush.Select(p => p.CreateProjectInfoForCurrentState()).ToImmutableArray();

            // We need to enable projects to start pushing changes to the workspace even before we add the solution/project to the host.
            // This is required because between the point we capture the project info for current state and the point where we start pushing to the workspace,
            // project system may send new events on the AbstractProject on a background thread, and these won't get pushed over to the workspace hosts as we didn't set the _pushingChangesToWorkspaceHost flag on the AbstractProject.
            // By invoking StartPushingToWorkspaceHosts upfront, any project state changes on the background thread will enqueue notifications to workspace hosts on foreground scheduled tasks.
            foreach (var project in inOrderToPush)
            {
                project.PushingChangesToWorkspace = true;
            }

            using (WorkspaceServices.GetService<IGlobalOperationNotificationService>()?.Start("Add Project to Workspace"))
            {
                if (!_solutionAdded)
                {
                    string solutionFilePath = null;
                    VersionStamp? version = default;
                    // Figure out the solution version
                    if (ErrorHandler.Succeeded(_vsSolution.GetSolutionInfo(out var solutionDirectory, out var solutionFileName, out var userOptsFile)) && solutionFileName != null)
                    {
                        solutionFilePath = Path.Combine(solutionDirectory, solutionFileName);
                        if (File.Exists(solutionFilePath))
                        {
                            version = VersionStamp.Create(File.GetLastWriteTimeUtc(solutionFilePath));
                        }
                    }

                    if (version == null)
                    {
                        version = VersionStamp.Create();
                    }

                    var id = SolutionId.CreateNewId(string.IsNullOrWhiteSpace(solutionFileName) ? null : solutionFileName);

                    var solutionInfo = SolutionInfo.Create(id, version.Value, solutionFilePath, projects: projectInfos);

                    NotifyWorkspace(workspace => workspace.OnSolutionAdded(solutionInfo));

                    _solutionAdded = true;

                    var persistenceService = WorkspaceServices.GetRequiredService<IPersistentStorageLocationService>() as VisualStudioPersistentStorageLocationService;
                    persistenceService?.UpdateForVisualStudioWorkspace(_workspace);

                }
                else
                {
                    // The solution is already added, so we'll just do project added notifications from here
                    foreach (var projectInfo in projectInfos)
                    {
                        NotifyWorkspace(workspace => workspace.OnProjectAdded(projectInfo));
                    }
                }

                foreach (var project in inOrderToPush)
                {
                    _pushedProjects.Add(project);

                    foreach (var document in project.GetCurrentDocuments())
                    {
                        if (document.IsOpen)
                        {
                            NotifyWorkspace(workspace =>
                                workspace.OnDocumentOpened(
                                    document.Id,
                                    document.GetOpenTextBuffer().AsTextContainer(),
                                    isCurrentContext: LinkedFileUtilities.IsCurrentContextHierarchy(document, _runningDocumentTable)));
                        }
                    }
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

            if (_pushedProjects.Contains(project))
            {
                NotifyWorkspace(workspace => workspace.OnProjectRemoved(project.Id));

                _pushedProjects.Remove(project);
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
        /// Notifies the <see cref="Workspace"/> of the change with the appropriate threading handling.
        /// </summary>
        /// <remarks>This method must be called on the foreground thread.</remarks>
        internal void NotifyWorkspace(Action<Workspace> action)
        {
            AssertIsForeground();

            // We do not want to allow message pumping/reentrancy when processing project system changes.
            using (Dispatcher.CurrentDispatcher.DisableProcessing())
            {
                action(_workspace);
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

        private static bool HACK_StripRefDirectoryFromPath(string filePath, out string binFilePath)
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
                p.PushingChangesToWorkspace = false;
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

            NotifyWorkspace(host => host.OnSolutionRemoved());
            NotifyWorkspace(host => (host as VisualStudioWorkspaceImpl)?.ClearReferenceCache());

            lock (_gate)
            {
                _projectPathToIdMap.Clear();
            }

            RuleSetFileProvider.ClearCachedRuleSetFiles();

            _solutionAdded = false;
            _pushedProjects.Clear();

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
