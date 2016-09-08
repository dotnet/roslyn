// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal sealed partial class VisualStudioProjectTracker
    {
        internal sealed class WorkspaceHostState : ForegroundThreadAffinitizedObject
        {
            private readonly IVisualStudioWorkspaceHost _workspaceHost;
            private readonly VisualStudioProjectTracker _tracker;
            private readonly HashSet<AbstractProject> _pushedProjects;

            /// <summary>
            /// Set to true if we've already called <see cref="IVisualStudioWorkspaceHost.OnSolutionAdded(Microsoft.CodeAnalysis.SolutionInfo)"/>
            /// for this host. Set to false after the solution has closed.
            /// </summary>
            private bool _solutionAdded;

            public WorkspaceHostState(VisualStudioProjectTracker tracker, IVisualStudioWorkspaceHost workspaceHost)
                : base(assertIsForeground: true)
            {
                _tracker = tracker;
                _workspaceHost = workspaceHost;
                _pushedProjects = new HashSet<AbstractProject>();

                this.HostReadyForEvents = false;
                _solutionAdded = false;
            }

            public IVisualStudioWorkspaceHost Host
            {
                get
                {
                    AssertIsForeground();
                    return _workspaceHost;
                }
            }

            /// <summary>
            /// Whether or not the project tracker has been notified that it should start to push state
            /// to the <see cref="IVisualStudioWorkspaceHost"/> or not.
            /// </summary>
            public bool HostReadyForEvents { get; set; }

            private void AddToPushListIfNeeded(AbstractProject project, List<AbstractProject> inOrderToPush, HashSet<AbstractProject> visited)
            {
                AssertIsForeground();

                if (_pushedProjects.Contains(project))
                {
                    return;
                }

                if (!visited.Add(project))
                {
                    return;
                }

                foreach (var projectReference in project.GetCurrentProjectReferences())
                {
                    AddToPushListIfNeeded(_tracker.GetProject(projectReference.ProjectId), inOrderToPush, visited);
                }

                inOrderToPush.Add(project);
            }

            internal void SolutionClosed()
            {
                AssertIsForeground();

                _solutionAdded = false;
                _pushedProjects.Clear();
            }

            internal void StartPushingToWorkspaceAndNotifyOfOpenDocuments(
                IEnumerable<AbstractProject> projects)
            {
                AssertIsForeground();

                // If the workspace host isn't actually ready yet, we shouldn't do anything.
                // Also, if the solution is closing we shouldn't do anything either, because all of our state is
                // in the process of going away. This can happen if we receive notification that a document has
                // opened in the middle of the solution close operation.
                if (!this.HostReadyForEvents || _tracker._solutionIsClosing)
                {
                    return;
                }

                // We need to push these projects and any project dependencies we already know about. Therefore, compute the
                // transitive closure of the projects that haven't already been pushed, keeping them in appropriate order.
                var visited = new HashSet<AbstractProject>();
                var inOrderToPush = new List<AbstractProject>();

                foreach (var project in projects)
                {
                    AddToPushListIfNeeded(project, inOrderToPush, visited);
                }

                var projectInfos = inOrderToPush.Select(p => p.CreateProjectInfoForCurrentState()).ToImmutableArray();

                // We need to enable projects to start pushing changes to workspace hosts even before we add the solution/project to the host.
                // This is required because between the point we capture the project info for current state and the point where we start pushing to workspace hosts,
                // project system may send new events on the AbstractProject on a background thread, and these won't get pushed over to the workspace hosts as we didn't set the _pushingChangesToWorkspaceHost flag on the AbstractProject.
                // By invoking StartPushingToWorkspaceHosts upfront, any project state changes on the background thread will enqueue notifications to workspace hosts on foreground scheduled tasks.
                foreach (var project in inOrderToPush)
                {
                    project.StartPushingToWorkspaceHosts();
                }

                if (!_solutionAdded)
                {
                    string solutionFilePath = null;
                    VersionStamp? version = default(VersionStamp?);

                    // Figure out the solution version
                    string solutionDirectory;
                    string solutionFileName;
                    string userOptsFile;
                    if (ErrorHandler.Succeeded(_tracker._vsSolution.GetSolutionInfo(out solutionDirectory, out solutionFileName, out userOptsFile)) && solutionFileName != null)
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
                    _tracker.RegisterSolutionProperties(id);

                    var solutionInfo = SolutionInfo.Create(id, version.Value, solutionFilePath, projects: projectInfos);

                    this.Host.OnSolutionAdded(solutionInfo);

                    _solutionAdded = true;
                }
                else
                {
                    // The solution is already added, so we'll just do project added notifications from here
                    foreach (var projectInfo in projectInfos)
                    {
                        this.Host.OnProjectAdded(projectInfo);
                    }
                }

                foreach (var project in inOrderToPush)
                {
                    _pushedProjects.Add(project);

                    foreach (var document in project.GetCurrentDocuments())
                    {
                        if (document.IsOpen)
                        {
                            this.Host.OnDocumentOpened(
                                document.Id,
                                document.GetOpenTextBuffer(),
                                isCurrentContext: LinkedFileUtilities.IsCurrentContextHierarchy(document, _tracker._runningDocumentTable));
                        }
                    }
                }
            }

            internal void RemoveProject(AbstractProject project)
            {
                // If we've already told this host about it, then we need to remove it. Otherwise, this host has no
                // further work to do.
                if (_pushedProjects.Remove(project))
                {
                    this.Host.OnProjectRemoved(project.Id);
                }
            }
        }
    }
}
