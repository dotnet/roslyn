// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal sealed partial class VisualStudioProjectTracker
    {
        internal sealed class WorkspaceHostState
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
            {
                _tracker = tracker;
                _workspaceHost = workspaceHost;
                _pushedProjects = new HashSet<AbstractProject>();

                this.HostReadyForEvents = false;
                _solutionAdded = false;
            }

            public IVisualStudioWorkspaceHost Host { get { return _workspaceHost; } }

            /// <summary>
            /// Whether or not the project tracker has been notified that it should start to push state
            /// to the <see cref="IVisualStudioWorkspaceHost"/> or not.
            /// </summary>
            public bool HostReadyForEvents { get; set; }

            private void AddToPushListIfNeeded(AbstractProject project, List<AbstractProject> inOrderToPush, HashSet<AbstractProject> visited)
            {
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
                    AddToPushListIfNeeded(_tracker._projectMap[projectReference.ProjectId], inOrderToPush, visited);
                }

                inOrderToPush.Add(project);
            }

            internal void SolutionClosed()
            {
                _solutionAdded = false;
                _pushedProjects.Clear();
            }

            internal void StartPushingToWorkspaceAndNotifyOfOpenDocuments(IEnumerable<AbstractProject> projects)
            {
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
                    project.StartPushingToWorkspaceHosts();
                    project.UpdateGeneratedFiles();
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
        }
    }
}
