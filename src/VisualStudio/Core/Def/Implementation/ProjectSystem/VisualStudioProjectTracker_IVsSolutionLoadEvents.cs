// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.CodeAnalysis.MSBuild;
using System.Collections.Generic;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class VisualStudioProjectTracker : IVsSolutionLoadEvents
    {
        private string _solutionFilename;
        private CancellationTokenSource _solutionParsingCancellationTokenSource = new CancellationTokenSource();
        private bool _inDeferredProjectLoad = true;

        int IVsSolutionLoadEvents.OnBeforeOpenSolution(string pszSolutionFilename)
        {
            this._solutionFilename = pszSolutionFilename;
            return VSConstants.S_OK;
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

            if (_inDeferredProjectLoad)
            {
                var componentModel = _serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel;
                var workspaceProjectFactory = componentModel.GetService<IWorkspaceProjectContextFactory>();
                LoadSolutionInBackground(workspaceProjectFactory);
            }
            else
            {
                PushCompleteSolutionToWorkspace(s_getProjectInfoForProject);
            }

            return VSConstants.S_OK;
        }

        private async void LoadSolutionInBackground(IWorkspaceProjectContextFactory workspaceProjectContextFactory)
        {
            var solutionInfo = await Task.Run(LoadSolutionInfoAsync, _solutionParsingCancellationTokenSource.Token).ConfigureAwait(true);

            var projectToProjectInfo = new Dictionary<AbstractProject, ProjectInfo>();

            // TODO: Sort this topologically somehow?
            foreach (var projectInfo in solutionInfo.Projects)
            {
                if (_projectPathToIdMap.ContainsKey(projectInfo.FilePath))
                {
                    continue;
                }

                var projectContext = workspaceProjectContextFactory.CreateProjectContext(
                    projectInfo.Language,
                    projectInfo.Name,
                    projectInfo.FilePath,
                    Guid.Empty,
                    null,
                    projectInfo.CommandLineOpt);

                foreach (var documentInfo in projectInfo.Documents)
                {
                    projectContext.AddSourceFile(documentInfo.FilePath, folderNames: documentInfo.Folders);
                }

                foreach (var documentInfo in projectInfo.AdditionalDocuments)
                {
                    projectContext.AddAdditionalFile(documentInfo.FilePath);
                }

                foreach (var reference in projectInfo.MetadataReferences)
                {
                    projectContext.AddMetadataReference(reference.Display, reference.Properties);
                }

                foreach (var reference in projectInfo.ProjectReferences)
                {
                    // TODO: If this was eagerly loaded already, this cast will fail?
                    projectContext.AddProjectReference(
                        (IWorkspaceProjectContext)_projectMap[reference.ProjectId],
                        new MetadataReferenceProperties(aliases: reference.Aliases, embedInteropTypes: reference.EmbedInteropTypes));
                }

                foreach (var reference in projectInfo.AnalyzerReferences)
                {
                    projectContext.AddAnalyzerReference(reference.FullPath);
                }

                projectToProjectInfo[(AbstractProject)projectContext] = projectInfo;
            }

            PushCompleteSolutionToWorkspace(p => projectToProjectInfo[p]);
        }

        private IVisualStudioHostDocument CreateHostDocument(AbstractProject project, DocumentInfo documentInfo)
        {
            return DocumentProvider.TryGetDocumentForFile(
                                        project,
                                        documentInfo.FilePath,
                                        SourceCodeKind.Regular,
                                        tb => true,
                                        itemid => SpecializedCollections.EmptyReadOnlyList<string>());
        }

        private Task<SolutionInfo> LoadSolutionInfoAsync()
        {
            var loader = new MSBuildProjectLoader(_workspace);
            return loader.LoadSolutionInfoAsync(_solutionFilename, _solutionParsingCancellationTokenSource.Token);
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
