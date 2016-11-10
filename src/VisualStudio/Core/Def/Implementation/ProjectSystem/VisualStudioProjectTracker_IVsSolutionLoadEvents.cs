// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
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

        private async Task LoadSolutionFromMSBuildAsync(
            CancellationToken cancellationToken)
        {
            AssertIsForeground();
            InitializeOutputPane();

            // Continue on the UI thread for these operations, since we are touching the VisualStudioWorkspace, etc.
            await PopulateWorkspaceFromDeferredProjectInfoAsync(cancellationToken).ConfigureAwait(true);
        }

        [Conditional("DEBUG")]
        private void InitializeOutputPane()
        {
            var outputWindow = (IVsOutputWindow)_serviceProvider.GetService(typeof(SVsOutputWindow));
            var paneGuid = new Guid("07aaa8e9-d776-47d6-a1be-5ce00332d74d");
            if (ErrorHandler.Succeeded(outputWindow.CreatePane(ref paneGuid, "Roslyn DPL Status", fInitVisible: 1, fClearWithSolution: 1)) &&
                ErrorHandler.Succeeded(outputWindow.GetPane(ref paneGuid, out _pane)) && _pane != null)
            {
                _pane.Activate();
            }
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

            // In Non-DPL scenarios, this indicates that ASL is complete, and we should push any
            // remaining information we have to the Workspace.  If DPL is enabled, this is never
            // called.
            FinishLoad();

            return VSConstants.S_OK;
        }

        private async Task PopulateWorkspaceFromDeferredProjectInfoAsync(
            CancellationToken cancellationToken)
        {
            // NOTE: We need to check cancellationToken after each await, in case the user has
            // already closed the solution.
            AssertIsForeground();

            var componentModel = _serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel;
            var workspaceProjectContextFactory = componentModel.GetService<IWorkspaceProjectContextFactory>();

            var dte = _serviceProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            var solutionConfig = (EnvDTE80.SolutionConfiguration2)dte.Solution.SolutionBuild.ActiveConfiguration;

            OutputToOutputWindow($"Getting project information - start");
            var start = DateTimeOffset.UtcNow;

            var projectInfos = SpecializedCollections.EmptyReadOnlyDictionary<string, DeferredProjectInformation>();

            // Note that `solutionConfig` may be null. For example: if the solution doesn't actually
            // contain any projects.
            if (solutionConfig != null)
            {
                // Capture the context so that we come back on the UI thread, and do the actual project creation there.
                var deferredProjectWorkspaceService = _workspaceServices.GetService<IDeferredProjectWorkspaceService>();
                projectInfos = await deferredProjectWorkspaceService.GetDeferredProjectInfoForConfigurationAsync(
                    $"{solutionConfig.Name}|{solutionConfig.PlatformName}",
                    cancellationToken).ConfigureAwait(true);
            }

            AssertIsForeground();
            cancellationToken.ThrowIfCancellationRequested();
            OutputToOutputWindow($"Getting project information - done (took {DateTimeOffset.UtcNow - start})");

            OutputToOutputWindow($"Creating projects - start");
            start = DateTimeOffset.UtcNow;
            var targetPathsToProjectPaths = BuildTargetPathMap(projectInfos);
            var analyzerAssemblyLoader = _workspaceServices.GetRequiredService<IAnalyzerService>().GetLoader();
            foreach (var projectFilename in projectInfos.Keys)
            {
                cancellationToken.ThrowIfCancellationRequested();
                GetOrCreateProjectFromArgumentsAndReferences(
                    workspaceProjectContextFactory,
                    analyzerAssemblyLoader,
                    projectFilename,
                    projectInfos,
                    targetPathsToProjectPaths);
            }
            OutputToOutputWindow($"Creating projects - done (took {DateTimeOffset.UtcNow - start})");

            OutputToOutputWindow($"Pushing to workspace - start");
            start = DateTimeOffset.UtcNow;
            FinishLoad();
            OutputToOutputWindow($"Pushing to workspace - done (took {DateTimeOffset.UtcNow - start})");
        }

        private static ImmutableDictionary<string, string> BuildTargetPathMap(IReadOnlyDictionary<string, DeferredProjectInformation> projectInfos)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in projectInfos)
            {
                var targetPath = item.Value.TargetPath;
                if (!string.IsNullOrEmpty(targetPath))
                {
                    if (!builder.ContainsKey(targetPath))
                    {
                        builder[targetPath] = item.Key;
                    }
                    else
                    {
                        Debug.Fail($"Already have a target path of '{item.Value.TargetPath}', with value '{builder[item.Value.TargetPath]}'.");
                    }
                }
            }
            return builder.ToImmutable();
        }

        [Conditional("DEBUG")]
        private void OutputToOutputWindow(string message)
        {
            _pane?.OutputString(message + Environment.NewLine);
        }

        private AbstractProject GetOrCreateProjectFromArgumentsAndReferences(
            IWorkspaceProjectContextFactory workspaceProjectContextFactory,
            IAnalyzerAssemblyLoader analyzerAssemblyLoader,
            string projectFilename,
            IReadOnlyDictionary<string, DeferredProjectInformation> allProjectInfos,
            IReadOnlyDictionary<string, string> targetPathsToProjectPaths)
        {
            var languageName = GetLanguageOfProject(projectFilename);
            if (languageName == null)
            {
                return null;
            }

            DeferredProjectInformation projectInfo;
            if (!allProjectInfos.TryGetValue(projectFilename, out projectInfo))
            {
                // This could happen if we were called recursively about a dangling P2P reference
                // that isn't actually in the solution.
                return null;
            }

            var commandLineParser = _workspaceServices.GetLanguageServices(languageName).GetService<ICommandLineParserService>();
            var projectDirectory = PathUtilities.GetDirectoryName(projectFilename);
            var commandLineArguments = commandLineParser.Parse(
                projectInfo.CommandLineArguments,
                projectDirectory,
                isInteractive: false,
                sdkDirectory: RuntimeEnvironment.GetRuntimeDirectory());

            // TODO: Should come from sln file?
            var projectName = PathUtilities.GetFileName(projectFilename, includeExtension: false);

            // `AbstractProject` only sets the filename if it actually exists.  Since we want 
            // our ids to match, mimic that behavior here.
            var projectId = File.Exists(projectFilename)
                ? GetOrCreateProjectIdForPath(projectFilename, projectName)
                : GetOrCreateProjectIdForPath(projectName, projectName);

            // See if we've already created this project and we're now in a recursive call to
            // hook up a P2P ref.
            AbstractProject project;
            if (_projectMap.TryGetValue(projectId, out project))
            {
                return project;
            }

            OutputToOutputWindow($"\tCreating '{projectName}':\t{commandLineArguments.SourceFiles.Length} source files,\t{commandLineArguments.MetadataReferences.Length} references.");
            var solution5 = _serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution5;

            // If the index is stale, it might give us a path that doesn't exist anymore that the 
            // solution doesn't know about - be resilient to that case.
            Guid projectGuid;
            try
            {
                projectGuid = solution5.GetGuidOfProjectFile(projectFilename);
            }
            catch (ArgumentException)
            {
                var message = $"Failed to get the project guid for '{projectFilename}' from the solution, using  random guid instead.";
                Debug.Fail(message);
                OutputToOutputWindow(message);
                projectGuid = Guid.NewGuid();
            }

            // NOTE: If the indexing service fails for a project, it will give us an *empty*
            // target path, which we aren't prepared to handle.  Instead, convert it to a *null*
            // value, which we do handle.
            var outputPath = projectInfo.TargetPath;
            if (outputPath == string.Empty)
            {
                outputPath = null;
            }

            var projectContext = workspaceProjectContextFactory.CreateProjectContext(
                languageName,
                projectName,
                projectFilename,
                projectGuid: projectGuid,
                hierarchy: null,
                binOutputPath: outputPath);

            project = (AbstractProject)projectContext;
            projectContext.SetOptions(projectInfo.CommandLineArguments.Join(" "));

            foreach (var sourceFile in commandLineArguments.SourceFiles)
            {
                projectContext.AddSourceFile(sourceFile.Path);
            }

            foreach (var sourceFile in commandLineArguments.AdditionalFiles)
            {
                projectContext.AddAdditionalFile(sourceFile.Path);
            }

            var addedProjectReferences = new HashSet<string>();
            foreach (var projectReferencePath in projectInfo.ReferencedProjectFilePaths)
            {
                // NOTE: ImmutableProjects might contain projects for other languages like
                // Xaml, or Typescript where the project file ends up being identical.
                var referencedProject = ImmutableProjects.SingleOrDefault(
                    p => (p.Language == LanguageNames.CSharp || p.Language == LanguageNames.VisualBasic)
                         && StringComparer.OrdinalIgnoreCase.Equals(p.ProjectFilePath, projectReferencePath));
                if (referencedProject == null)
                {
                    referencedProject = GetOrCreateProjectFromArgumentsAndReferences(
                        workspaceProjectContextFactory,
                        analyzerAssemblyLoader,
                        projectReferencePath,
                        allProjectInfos,
                        targetPathsToProjectPaths);
                }

                var referencedProjectContext = referencedProject as IWorkspaceProjectContext;
                if (referencedProjectContext != null)
                {
                    // TODO: Can we get the properties from corresponding metadata reference in
                    // commandLineArguments?
                    addedProjectReferences.Add(projectReferencePath);
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
                        addedProjectReferences.Add(projectReferencePath);
                        projectContext.AddMetadataReference(
                            existingReferenceOutputPath,
                            new MetadataReferenceProperties());
                    }
                }
                else
                {
                    // We don't know how to create this project.  Another language or something?
                    OutputToOutputWindow($"Failed to create a project for '{projectReferencePath}'.");
                }
            }

            foreach (var reference in commandLineArguments.ResolveMetadataReferences(project.CurrentCompilationOptions.MetadataReferenceResolver))
            {
                // Some references may fail to be resolved - if they are, we'll still pass them
                // through, in case they come into existence later (they may be built by other 
                // parts of the build system).
                var unresolvedReference = reference as UnresolvedMetadataReference;
                var path = unresolvedReference == null
                    ? ((PortableExecutableReference)reference).FilePath
                    : unresolvedReference.Reference;

                string possibleProjectReference;
                if (targetPathsToProjectPaths.TryGetValue(path, out possibleProjectReference) &&
                    addedProjectReferences.Contains(possibleProjectReference))
                {
                    // We already added a P2P reference for this, we don't need to add the file reference too.
                    continue;
                }

                projectContext.AddMetadataReference(path, reference.Properties);
            }

            foreach (var reference in commandLineArguments.ResolveAnalyzerReferences(analyzerAssemblyLoader))
            {
                var path = reference.FullPath;
                if (!PathUtilities.IsAbsolute(path))
                {
                    path = PathUtilities.CombineAbsoluteAndRelativePaths(
                        projectDirectory,
                        path);
                }

                projectContext.AddAnalyzerReference(path);
            }

            return (AbstractProject)projectContext;
        }

        private static string GetLanguageOfProject(string projectFilename)
        {
            switch (PathUtilities.GetExtension(projectFilename))
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
