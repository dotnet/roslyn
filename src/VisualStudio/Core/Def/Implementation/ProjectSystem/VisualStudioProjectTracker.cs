// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal sealed partial class VisualStudioProjectTracker : IDisposable, IVisualStudioHostProjectContainer
    {
        private static readonly ConditionalWeakTable<SolutionId, string> s_workingFolderPathMap = new ConditionalWeakTable<SolutionId, string>();

        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<ProjectId, AbstractProject> _projectMap;

        /// <summary>
        /// This is a multi-map, only so we don't have any edge cases if people have two projects with
        /// the same output path. It makes state tracking notably easier.
        /// </summary>
        private readonly Dictionary<string, List<AbstractProject>> _projectsByBinPath = new Dictionary<string, List<AbstractProject>>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, ProjectId> _projectPathToIdMap;
        private readonly List<WorkspaceHostState> _workspaceHosts;
        private readonly IVsSolution _vsSolution;
        private readonly IVsRunningDocumentTable4 _runningDocumentTable;

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

        internal IEnumerable<AbstractProject> Projects { get { return _projectMap.Values; } }

        IEnumerable<IVisualStudioHostProject> IVisualStudioHostProjectContainer.GetProjects()
        {
            return this.Projects;
        }

        void IVisualStudioHostProjectContainer.NotifyNonDocumentOpenedForProject(IVisualStudioHostProject project)
        {
            var abstractProject = (AbstractProject)project;
            StartPushingToWorkspaceAndNotifyOfOpenDocuments(SpecializedCollections.SingletonEnumerable(abstractProject));
        }

        private uint? _solutionEventsCookie;

        public VisualStudioProjectTracker(IServiceProvider serviceProvider)
        {
            _projectMap = new Dictionary<ProjectId, AbstractProject>();
            _projectPathToIdMap = new Dictionary<string, ProjectId>(StringComparer.OrdinalIgnoreCase);

            _serviceProvider = serviceProvider;
            _workspaceHosts = new List<WorkspaceHostState>(capacity: 1);

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

        public void RegisterSolutionProperties(SolutionId solutionId)
        {
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
            if (_workspaceHosts.Any(hostState => hostState.Host == host))
            {
                throw new ArgumentException("The workspace host is already registered.", "host");
            }

            _workspaceHosts.Add(new WorkspaceHostState(this, host));
        }

        public void StartSendingEventsToWorkspaceHost(IVisualStudioWorkspaceHost host)
        {
            var hostData = _workspaceHosts.Find(s => s.Host == host);
            if (hostData == null)
            {
                throw new ArgumentException("The workspace host not registered", "host");
            }

            // This method is idempotent.
            if (hostData.HostReadyForEvents)
            {
                return;
            }

            hostData.HostReadyForEvents = true;

            // If any of the projects are already interactive, then we better catch up the host.
            var interactiveProjects = _projectMap.Values.Where(p => p.PushingChangesToWorkspaceHosts);
            if (interactiveProjects.Any())
            {
                hostData.StartPushingToWorkspaceAndNotifyOfOpenDocuments(interactiveProjects);
            }
        }

        public DocumentProvider DocumentProvider { get; set; }
        public VisualStudioMetadataReferenceManager MetadataReferenceProvider { get; set; }
        public VisualStudioRuleSetManager RuleSetFileProvider { get; set; }

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
            AbstractProject project;
            _projectMap.TryGetValue(id, out project);
            return project;
        }

        /// <summary>
        /// Add a project to the workspace.
        /// </summary>
        internal void AddProject(AbstractProject project)
        {
            _projectMap.Add(project.Id, project);

            UpdateProjectBinPath(project, null, project.TryGetBinOutputPath());

            if (_solutionLoadComplete)
            {
                StartPushingToWorkspaceAndNotifyOfOpenDocuments(SpecializedCollections.SingletonEnumerable(project));
            }
            else
            {
                _projectsLoadedThisBatch.Add(project);
            }
        }

        internal void StartPushingToWorkspaceAndNotifyOfOpenDocuments(IEnumerable<AbstractProject> projects)
        {
            foreach (var hostState in _workspaceHosts)
            {
                hostState.StartPushingToWorkspaceAndNotifyOfOpenDocuments(projects);
            }
        }

        /// <summary>
        /// Remove a project from the workspace.
        /// </summary>
        internal void RemoveProject(AbstractProject project)
        {
            Contract.ThrowIfFalse(_projectMap.Remove(project.Id));

            UpdateProjectBinPath(project, project.TryGetBinOutputPath(), null);

            if (project.PushingChangesToWorkspaceHosts)
            {
                NotifyWorkspaceHosts(host => host.OnProjectRemoved(project.Id));
            }
        }

        internal void UpdateProjectBinPath(AbstractProject project, string oldBinPathOpt, string newBinPathOpt)
        {
            if (oldBinPathOpt != null)
            {
                UpdateReferencesForBinPathChange(oldBinPathOpt, () => _projectsByBinPath.MultiRemove(oldBinPathOpt, project));
            }

            if (newBinPathOpt != null)
            {
                UpdateReferencesForBinPathChange(newBinPathOpt, () => _projectsByBinPath.MultiAdd(newBinPathOpt, project));
            }
        }

        private void UpdateReferencesForBinPathChange(string path, Action updateProjects)
        {
            // If we already have a single project that points to this path, we'll either be:
            // 
            // (1) removing it, where it no longer exists, or
            // (2) adding another path, where it's now ambiguous
            //
            // in either case, we want to undo file-to-P2P reference conversion
            List<AbstractProject> existingProjects;

            if (_projectsByBinPath.TryGetValue(path, out existingProjects))
            {
                if (existingProjects.Count == 1)
                {
                    foreach (var projectToUpdate in _projectMap.Values)
                    {
                        projectToUpdate.UndoProjectReferenceConversionForDisappearingOutputPath(path);
                    }
                }
            }

            updateProjects();

            if (_projectsByBinPath.TryGetValue(path, out existingProjects))
            {
                if (existingProjects.Count == 1)
                {
                    foreach (var projectToUpdate in _projectMap.Values)
                    {
                        projectToUpdate.TryProjectConversionForIntroducedOutputPath(path, existingProjects[0]);
                    }
                }
            }
        }

        internal ProjectId GetOrCreateProjectIdForPath(string projectPath, string projectSystemName)
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

        internal void NotifyWorkspaceHosts(Action<IVisualStudioWorkspaceHost> action)
        {
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

        internal bool TryGetProjectByBinPath(string filePath, out AbstractProject project)
        {
            project = null;

            List<AbstractProject> projects;
            if (_projectsByBinPath.TryGetValue(filePath, out projects))
            {
                // If for some reason we have more than one referencing project, it's ambiguous so bail
                if (projects.Count == 1)
                {
                    project = projects[0];
                    return true;
                }
            }

            return false;
        }
    }
}
