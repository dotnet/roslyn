// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal sealed partial class VisualStudioProjectTracker
    {
        private readonly Workspace _workspace;
        private readonly IThreadingContext _threadingContext;

        internal ImmutableArray<AbstractProject> ImmutableProjects => ImmutableArray<AbstractProject>.Empty;

        internal HostWorkspaceServices WorkspaceServices => _workspace.Services;

        public VisualStudioProjectTracker(Workspace workspace, IThreadingContext threadingContext)
        {
            _workspace = workspace;
            _threadingContext = threadingContext;
        }

        /*
          
        private void FinishLoad()
        {
            // Check that the set of analyzers is complete and consistent.
            GetAnalyzerDependencyCheckingService()?.ReanalyzeSolutionForConflicts();
        }

        private AnalyzerDependencyCheckingService GetAnalyzerDependencyCheckingService()
        {
            var componentModel = (IComponentModel)_serviceProvider.GetService(typeof(SComponentModel));

            return componentModel.GetService<AnalyzerDependencyCheckingService>();
        }

        */

        public ProjectId GetOrCreateProjectIdForPath(string filePath, string projectDisplayName)
        {
            // HACK: to keep F# working, we will ensure we return the ProjectId if there is a project that matches this path. Otherwise, we'll just return
            // a random ProjectId, which is sufficient for their needs. They'll simply observe there is no project with that ID, and then go and create a
            // new project. Then they call this function again, and fetch the real ID.
            return _workspace.CurrentSolution.Projects.FirstOrDefault(p => p.FilePath == filePath)?.Id ?? ProjectId.CreateNewId("ProjectNotFound");
        }

        public AbstractProject GetProject(ProjectId projectId)
        {
            // HACK: to keep F# working, we will ensure that if there is a project with that ID, we will return a non-null value, otherwise we'll return null.
            // It doesn't actually matter *what* the project is, so we'll just return something silly
            var project = _workspace.CurrentSolution.GetProject(projectId);

            if (project != null)
            {
                return new StubProject(project, _threadingContext);
            }
            else
            {
                return null;
            }
        }

        private sealed class StubProject : AbstractProject
        {
            public StubProject(Project project, IThreadingContext threadingContext)
                : base(_ => null, project.Name + "_Stub", project.FilePath, null, project.Language, Guid.Empty, null, threadingContext, null)
            {
            }
        }
    }
}
