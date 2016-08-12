// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class VisualStudioProjectTracker : IVsSolutionLoadEvents
    {
        private CancellationTokenSource _solutionParsingCancellationTokenSource = new CancellationTokenSource();
        private IVsOutputWindowPane _pane;

        int IVsSolutionLoadEvents.OnBeforeOpenSolution(string pszSolutionFilename)
        {
            LoadSolutionFromMSBuild(pszSolutionFilename, _solutionParsingCancellationTokenSource.Token).FireAndForget();

            return VSConstants.S_OK;
        }

        private async Task LoadSolutionFromMSBuild(string pszSolutionFilename, CancellationToken cancellationToken)
        {
            AssertIsForeground();
            var outputWindow = (IVsOutputWindow)_serviceProvider.GetService(typeof(SVsOutputWindow));
            var paneGuid = new Guid("07aaa8e9-d776-47d6-a1be-5ce00332d74d");
            if (ErrorHandler.Succeeded(outputWindow.CreatePane(ref paneGuid, "Roslyn DPL Status", fInitVisible: 1, fClearWithSolution: 1)) &&
                ErrorHandler.Succeeded(outputWindow.GetPane(ref paneGuid, out _pane)) && _pane != null)
            {
                _pane.Activate();
                OutputToOutputWindow("OnBeforeOpenSolution - waiting 3 seconds to load solution in background");
            }

            // Continue on the UI thread for these operations, since we are touching the VisualStudioWorkspace, etc.
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken).ConfigureAwait(true);
            await LoadSolutionInBackground(pszSolutionFilename, cancellationToken).ConfigureAwait(true);
        }

        int IVsSolutionLoadEvents.OnBeforeBackgroundSolutionLoadBegins()
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionLoadEvents.OnQueryBackgroundLoadProjectBatch(out bool pfShouldDelayLoadToNextIdle)
        {
            pfShouldDelayLoadToNextIdle = false;
            return VSConstants.S_OK;
        }

        int IVsSolutionLoadEvents.OnBeforeLoadProjectBatch(bool fIsBackgroundIdleBatch)
        {
            AssertIsForeground();

            _projectsLoadedThisBatch.Clear();
            return VSConstants.S_OK;
        }

        int IVsSolutionLoadEvents.OnAfterLoadProjectBatch(bool fIsBackgroundIdleBatch)
        {
            AssertIsForeground();

            if (!fIsBackgroundIdleBatch)
            {
                // This batch was loaded eagerly. This might be because the user is force expanding the projects in the
                // Solution Explorer, or they had some files open in an .suo we need to push.
                StartPushingToWorkspaceAndNotifyOfOpenDocuments(_projectsLoadedThisBatch, s_getProjectInfoForProject);
            }

            _projectsLoadedThisBatch.Clear();

            return VSConstants.S_OK;
        }

        int IVsSolutionLoadEvents.OnAfterBackgroundSolutionLoadComplete()
        {
            AssertIsForeground();
            // TODO: Not called in DPL scenarios?
            //if (_inDeferredProjectLoad)
            //{
            //    LoadSolutionInBackground();
            //}
            //else
            //{
                PushCompleteSolutionToWorkspace(s_getProjectInfoForProject);
            //}

            return VSConstants.S_OK;
        }

        private async Task LoadSolutionInBackground(string solutionFilename, CancellationToken cancellationToken)
        {
            AssertIsForeground();

            var componentModel = _serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel;
            var workspaceProjectContextFactory = componentModel.GetService<IWorkspaceProjectContextFactory>();

            OutputToOutputWindow("Beginning solution parsing");
            var start = DateTimeOffset.UtcNow;

            // Load the solution on a threadpool thread, but *do* capture the context so that we continue on the UI thread, since we're
            // going to create projects/etc.
            var solutionInfo = await Task.Run(() => LoadSolutionInfoAsync(solutionFilename, cancellationToken), cancellationToken).ConfigureAwait(true);
            AssertIsForeground();

            OutputToOutputWindow($"Done solution parsing - creating {solutionInfo.Projects.Count} projects.  Took {DateTimeOffset.UtcNow - start}");
            start = DateTimeOffset.UtcNow;
            var projectToProjectInfo = new Dictionary<AbstractProject, ProjectInfo>();
            foreach (var projectInfo in solutionInfo.Projects)
            {
                CreateProjectFromProjectInfo(workspaceProjectContextFactory, projectToProjectInfo, projectInfo, solutionInfo);
            }

            OutputToOutputWindow($"Done creating projects - pushing to workspace. Took {DateTimeOffset.UtcNow - start}");
            start = DateTimeOffset.UtcNow;
            PushCompleteSolutionToWorkspace(p => projectToProjectInfo[p]);
            OutputToOutputWindow($"Done pushing to workspace. Took {DateTimeOffset.UtcNow - start}");
        }

        private void OutputToOutputWindow(string message)
        {
            if (_pane != null)
            {
                _pane.OutputString(message + Environment.NewLine);
            }
        }

        private IWorkspaceProjectContext CreateProjectFromProjectInfo(IWorkspaceProjectContextFactory workspaceProjectContextFactory, Dictionary<AbstractProject, ProjectInfo> projectToProjectInfo, ProjectInfo projectInfo, SolutionInfo solutionInfo)
        {
            // Pre-prime our list of project Ids, so that the ones in solutionInfo will get re-used
            // by the Project's we create 
            AddProjectIdForPath(projectInfo.FilePath, projectInfo.Name, projectInfo.Id);

            // If this project already exists, either because it was loaded by VS, or because
            // we demand loaded it to satisfy a project reference from another project, then
            // there is nothing to do.
            var existingProject = Projects.SingleOrDefault(p => p.Id == projectInfo.Id);
            if (existingProject != null)
            {
                return existingProject as IWorkspaceProjectContext;
            }

            var projectContext = workspaceProjectContextFactory.CreateProjectContext(
                projectInfo.Language,
                projectInfo.Name,
                projectInfo.FilePath,
                Guid.Empty,
                hierarchy: null,
                commandLineForOptions: projectInfo.CommandLineOpt);

            foreach (var documentInfo in projectInfo.Documents)
            {
                using (DocumentProvider.ProvideDocumentIdHint(documentInfo.FilePath, documentInfo.Id))
                {
                    projectContext.AddSourceFile(documentInfo.FilePath, folderNames: documentInfo.Folders);
                }
            }

            foreach (var documentInfo in projectInfo.AdditionalDocuments)
            {
                using (DocumentProvider.ProvideDocumentIdHint(documentInfo.FilePath, documentInfo.Id))
                {
                    projectContext.AddAdditionalFile(documentInfo.FilePath);
                }
            }

            foreach (var reference in projectInfo.MetadataReferences)
            {
                projectContext.AddMetadataReference(reference.Display, reference.Properties);
            }

            foreach (var reference in projectInfo.ProjectReferences)
            {
                var referencedProject = (IWorkspaceProjectContext)projectToProjectInfo.SingleOrDefault(kvp => kvp.Value.Id == reference.ProjectId).Key;

                if (referencedProject == null)
                {
                    var referencedProjectInfo = solutionInfo.Projects.Single(pi => pi.Id == reference.ProjectId);
                    referencedProject = CreateProjectFromProjectInfo(workspaceProjectContextFactory, projectToProjectInfo, referencedProjectInfo, solutionInfo);
                }

                // TODO: If VS loaded a full project for this already, we will get null back from CreateProjectFromProjectInfo.

                projectContext.AddProjectReference(
                    referencedProject,
                    new MetadataReferenceProperties(aliases: reference.Aliases, embedInteropTypes: reference.EmbedInteropTypes));
            }

            foreach (var reference in projectInfo.AnalyzerReferences)
            {
                projectContext.AddAnalyzerReference(reference.FullPath);
            }

            projectToProjectInfo[(AbstractProject)projectContext] = projectInfo;
            return projectContext;
        }

        private Task<SolutionInfo> LoadSolutionInfoAsync(string solutionFilename, CancellationToken cancellationToken)
        {
            AssertIsBackground();
            var loader = new MSBuildProjectLoader(_workspace);
            return loader.LoadSolutionInfoAsync(solutionFilename, cancellationToken);
        }

        private void PushCompleteSolutionToWorkspace(Func<AbstractProject, ProjectInfo> getProjectInfo)
        {
            // We are now completely done, so let's simply ensure all projects are added.
            StartPushingToWorkspaceAndNotifyOfOpenDocuments_Foreground(this.ImmutableProjects);

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
    }
}
