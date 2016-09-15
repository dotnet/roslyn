// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class VisualStudioProjectTracker : IVsSolutionLoadEvents
    {
        // Temporary for prototyping purposes
        private IVsOutputWindowPane _pane;

        /// <summary>
        /// Used to cancel our background solution parse if we get a solution close event from VS.
        /// </summary>
        private CancellationTokenSource _solutionParsingCancellationTokenSource = new CancellationTokenSource();

        int IVsSolutionLoadEvents.OnBeforeOpenSolution(string pszSolutionFilename)
        {
            return VSConstants.S_OK;
        }

        private async Task LoadSolutionFromMSBuild(
            IDeferredProjectWorkspaceService deferredProjectWorkspaceService,
            CancellationToken cancellationToken)
        {
            AssertIsForeground();
            var outputWindow = (IVsOutputWindow)_serviceProvider.GetService(typeof(SVsOutputWindow));
            var paneGuid = new Guid("07aaa8e9-d776-47d6-a1be-5ce00332d74d");
            if (ErrorHandler.Succeeded(outputWindow.CreatePane(ref paneGuid, "Roslyn DPL Status", fInitVisible: 1, fClearWithSolution: 1)) &&
                ErrorHandler.Succeeded(outputWindow.GetPane(ref paneGuid, out _pane)) && _pane != null)
            {
                _pane.Activate();
            }

            // Continue on the UI thread for these operations, since we are touching the VisualStudioWorkspace, etc.
            await LoadSolutionInBackground(deferredProjectWorkspaceService, cancellationToken).ConfigureAwait(true);
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
                StartPushingToWorkspaceAndNotifyOfOpenDocuments(_projectsLoadedThisBatch);
            }

            _projectsLoadedThisBatch.Clear();

            return VSConstants.S_OK;
        }

        int IVsSolutionLoadEvents.OnAfterBackgroundSolutionLoadComplete()
        {
            AssertIsForeground();
            // TODO: Not called in DPL scenarios?
            //if (_deferredProjectLoadIsEnabled )
            //{
            //    LoadSolutionInBackground();
            //}
            //else
            //{
                FinishLoad();
            //}

            return VSConstants.S_OK;
        }

        private async Task LoadSolutionInBackground(
            IDeferredProjectWorkspaceService deferredProjectWorkspaceService,
            CancellationToken cancellationToken)
        {
            AssertIsForeground();

            var componentModel = _serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel;
            var workspaceProjectContextFactory = componentModel.GetService<IWorkspaceProjectContextFactory>();

            var dte = _serviceProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            var solutionConfig = dte.Solution.SolutionBuild.ActiveConfiguration;

            var start = DateTimeOffset.UtcNow;
            OutputToOutputWindow($"Getting project information - start");
            // Capture the context so that we come back on the UI thread, and do the actual project creation there.
            var projectInfos = await deferredProjectWorkspaceService.GetDeferredProjectInfoForConfigurationAsync(
                $"{solutionConfig.Name}|Any CPU",
                cancellationToken).ConfigureAwait(true);
            AssertIsForeground();
            OutputToOutputWindow($"Getting project information - done (took {DateTimeOffset.UtcNow - start})");

            OutputToOutputWindow($"Creating projects - start");
            foreach (var projectFilename in projectInfos.Keys)
            {
                CreateProjectFromArgumentsAndReferences(
                    workspaceProjectContextFactory,
                    projectFilename,
                    projectInfos);
            }
            OutputToOutputWindow($"Creating projects - done (took {DateTimeOffset.UtcNow - start})");

            OutputToOutputWindow($"Pushing to workspace - start");
            start = DateTimeOffset.UtcNow;
            FinishLoad();
            OutputToOutputWindow($"Pushing to workspace - done (took {DateTimeOffset.UtcNow - start})");
        }

        private void OutputToOutputWindow(string message)
        {
            if (_pane != null)
            {
                _pane.OutputString(message + Environment.NewLine);
            }
        }

        private IWorkspaceProjectContext CreateProjectFromArgumentsAndReferences(
            IWorkspaceProjectContextFactory workspaceProjectContextFactory,
            string projectFilename,
            IReadOnlyDictionary<string, DeferredProjectInformation> allProjectInfos)
        {
            var languageName = GetLanguageOfProject(projectFilename);
            if (languageName == null)
            {
                return null;
            }

            DeferredProjectInformation projectInfo;
            if (!allProjectInfos.TryGetValue(projectFilename, out projectInfo))
            {
                return null;
            }

            var commandLineParser = _workspace.Services.GetLanguageServices(languageName).GetService<ICommandLineParserService>();
            var projectDirectory = Path.GetDirectoryName(projectFilename);
            var commandLineArguments = commandLineParser.Parse(
                projectInfo.CommandLineArguments,
                projectDirectory,
                isInteractive: false,
                sdkDirectory: RuntimeEnvironment.GetRuntimeDirectory());

            // TODO: Should come from sln file?
            var projectName = Path.GetFileNameWithoutExtension(projectFilename);
            var projectId = GetOrCreateProjectIdForPath(projectFilename, projectName);

            AbstractProject project;
            if (_projectMap.TryGetValue(projectId, out project))
            {
                return project as IWorkspaceProjectContext;
            }

            // TODO: We need to actually get an output path somehow.
            OutputToOutputWindow($"\tCreating '{projectName}':\t{commandLineArguments.SourceFiles.Length} source files,\t{commandLineArguments.MetadataReferences.Length} references.");
            var solution5 = _serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution5;
            var projectGuid = solution5.GetGuidOfProjectFile(projectFilename);
            var projectContext = workspaceProjectContextFactory.CreateProjectContext(
                languageName,
                projectName,
                projectFilename,
                projectGuid: projectGuid,
                hierarchy: null,
                binOutputPath: null);

            projectContext.SetOptions(projectInfo.CommandLineArguments.Join(" "));

            foreach (var sourceFile in commandLineArguments.SourceFiles)
            {
                projectContext.AddSourceFile(sourceFile.Path);
            }

            foreach (var sourceFile in commandLineArguments.AdditionalFiles)
            {
                projectContext.AddAdditionalFile(sourceFile.Path);
            }

            foreach (var reference in commandLineArguments.MetadataReferences)
            {
                projectContext.AddMetadataReference(reference.Reference, reference.Properties);
            }

            foreach (var projectReference in projectInfo.ProjectReferences)
            {
                var projectReferencePath = projectReference.Substring("/ProjectReference:".Length);
                var referencedProject = ImmutableProjects.SingleOrDefault(p => StringComparer.OrdinalIgnoreCase.Equals(p.ProjectFilePath, projectReferencePath));
                if (referencedProject == null)
                {
                    referencedProject = (AbstractProject)CreateProjectFromArgumentsAndReferences(
                        workspaceProjectContextFactory,
                        projectReferencePath,
                        allProjectInfos);
                }

                var referencedProjectContext = referencedProject as IWorkspaceProjectContext;
                if (referencedProjectContext != null)
                {
                    projectContext.AddProjectReference(
                        referencedProjectContext,
                        new MetadataReferenceProperties());
                }
                else if (referencedProject != null)
                {
                    // This project was already created by the regular project system. See if we
                    // can find the matching project somehow.
                    var existingReferenceOutputPath = referencedProject?.BinOutputPath;
                    if (existingReferenceOutputPath != null)
                    {
                        projectContext.AddMetadataReference(
                            existingReferenceOutputPath,
                            new MetadataReferenceProperties());
                    }
                }
                else
                {
                    // We don't know how to create this project.  Another language or something?
                }
            }

            foreach (var reference in commandLineArguments.AnalyzerReferences)
            {
                projectContext.AddAnalyzerReference(reference.FilePath);
            }

            return projectContext;
        }

        private static string GetLanguageOfProject(string projectFilename)
        {
            switch (Path.GetExtension(projectFilename))
            {
                case ".csproj":
                    return LanguageNames.CSharp;
                case ".vbproj":
                    return LanguageNames.VisualBasic;
                default:
                    return null;
            };
        }

        private void FinishLoad()
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
