// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Progress;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor.CodeCleanup;
using Microsoft.VisualStudio.Language.CodeCleanUp;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using __VSHPROPID8 = Microsoft.VisualStudio.Shell.Interop.__VSHPROPID8;
using IVsHierarchyItemManager = Microsoft.VisualStudio.Shell.IVsHierarchyItemManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeCleanup
{
    /// <summary>
    /// Roslyn implementations of <see cref="ICodeCleanUpFixer"/> extend this class. Since other extensions could also
    /// be implementing the <see cref="ICodeCleanUpFixer"/> interface, this abstract base class allows Roslyn to operate
    /// on MEF instances of fixers known to be relevant in the context of Roslyn languages.
    /// </summary>
    internal abstract partial class AbstractCodeCleanUpFixer : ICodeCleanUpFixer
    {
        private readonly IThreadingContext _threadingContext;
        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly IVsHierarchyItemManager _vsHierarchyItemManager;
        private readonly IGlobalOptionService _globalOptions;

        protected AbstractCodeCleanUpFixer(
            IThreadingContext threadingContext,
            VisualStudioWorkspaceImpl workspace,
            IVsHierarchyItemManager vsHierarchyItemManager,
            IGlobalOptionService globalOptions)
        {
            _threadingContext = threadingContext;
            _workspace = workspace;
            _vsHierarchyItemManager = vsHierarchyItemManager;
            _globalOptions = globalOptions;
        }

        public Task<bool> FixAsync(ICodeCleanUpScope scope, ICodeCleanUpExecutionContext context)
            => scope switch
            {
                TextBufferCodeCleanUpScope textBufferScope => FixTextBufferAsync(textBufferScope, context),
                IVsHierarchyCodeCleanupScope hierarchyContentScope => FixHierarchyContentAsync(hierarchyContentScope, context),
                _ => Task.FromResult(false),
            };

        private async Task<bool> FixHierarchyContentAsync(IVsHierarchyCodeCleanupScope hierarchyContent, ICodeCleanUpExecutionContext context)
        {
            var hierarchy = hierarchyContent.Hierarchy;
            if (hierarchy == null)
            {
                return await FixSolutionAsync(_workspace.CurrentSolution, context).ConfigureAwait(true);
            }

            // Map the hierarchy to a ProjectId. For hierarchies mapping to multitargeted projects, we first try to
            // get the project in the most recent active context, but fall back to the first target framework if no
            // active context is available.
            var hierarchyToProjectMap = _workspace.Services.GetRequiredService<IHierarchyItemToProjectIdMap>();

            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(context.OperationContext.UserCancellationToken);

            ProjectId? projectId = null;
            if (ErrorHandler.Succeeded(hierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID8.VSHPROPID_ActiveIntellisenseProjectContext, out var contextProjectNameObject))
                && contextProjectNameObject is string contextProjectName)
            {
                projectId = _workspace.GetProjectWithHierarchyAndName(hierarchy, contextProjectName)?.Id;
            }

            if (projectId is null)
            {
                var projectHierarchyItem = _vsHierarchyItemManager.GetHierarchyItem(hierarchyContent.Hierarchy, (uint)VSConstants.VSITEMID.Root);
                if (!hierarchyToProjectMap.TryGetProjectId(projectHierarchyItem, targetFrameworkMoniker: null, out projectId))
                {
                    return false;
                }
            }

            var itemId = hierarchyContent.ItemId;
            if (itemId == (uint)VSConstants.VSITEMID.Root)
            {
                await TaskScheduler.Default;

                var project = _workspace.CurrentSolution.GetProject(projectId);
                if (project == null)
                {
                    return false;
                }

                return await FixProjectAsync(project, context).ConfigureAwait(true);
            }
            else if (hierarchy.GetCanonicalName(itemId, out var path) == 0)
            {
                var attr = File.GetAttributes(path);
                if (attr.HasFlag(FileAttributes.Directory))
                {
                    // directory
                    // TODO: this one will be implemented later
                    // https://github.com/dotnet/roslyn/issues/30165
                }
                else
                {
                    // Handle code cleanup for a single document
                    await TaskScheduler.Default;

                    var solution = _workspace.CurrentSolution;
                    var documentIds = solution.GetDocumentIdsWithFilePath(path);
                    var documentId = documentIds.FirstOrDefault(id => id.ProjectId == projectId);
                    if (documentId is null)
                    {
                        return false;
                    }

                    var document = solution.GetRequiredDocument(documentId);
                    var options = _globalOptions.GetCodeActionOptions(document.Project.Services);
                    return await FixDocumentAsync(document, options, context).ConfigureAwait(true);
                }
            }

            return false;
        }

        private Task<bool> FixSolutionAsync(Solution solution, ICodeCleanUpExecutionContext context)
        {
            return FixAsync(_workspace, ApplyFixAsync, context);

            // Local function
            Task<Solution> ApplyFixAsync(IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
            {
                return FixSolutionAsync(solution, context.EnabledFixIds, progress, cancellationToken);
            }
        }

        private Task<bool> FixProjectAsync(Project project, ICodeCleanUpExecutionContext context)
        {
            return FixAsync(_workspace, ApplyFixAsync, context);

            // Local function
            async Task<Solution> ApplyFixAsync(IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
            {
                var newProject = await FixProjectAsync(project, context.EnabledFixIds, progress, addProgressItemsForDocuments: true, cancellationToken).ConfigureAwait(true);
                return newProject.Solution;
            }
        }

        private Task<bool> FixDocumentAsync(Document document, CodeActionOptions options, ICodeCleanUpExecutionContext context)
        {
            return FixAsync(document.Project.Solution.Workspace, ApplyFixAsync, context);

            // Local function
            async Task<Solution> ApplyFixAsync(IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
            {
                var newDocument = await FixDocumentAsync(document, context.EnabledFixIds, progress, options, cancellationToken).ConfigureAwait(true);
                return newDocument.Project.Solution;
            }
        }

        private Task<bool> FixTextBufferAsync(TextBufferCodeCleanUpScope textBufferScope, ICodeCleanUpExecutionContext context)
        {
            var buffer = textBufferScope.SubjectBuffer;

            // Let LSP handle code cleanup in the cloud scenario
            if (buffer.IsInLspEditorContext())
            {
                return SpecializedTasks.False;
            }

            var document = buffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return SpecializedTasks.False;
            }

            var workspace = buffer.GetWorkspace();
            Contract.ThrowIfNull(workspace);
            return FixAsync(workspace, ApplyFixAsync, context);

            // Local function
            async Task<Solution> ApplyFixAsync(IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
            {
                var document = buffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                Contract.ThrowIfNull(document);

                var options = _globalOptions.GetCodeActionOptions(document.Project.Services);
                var newDoc = await FixDocumentAsync(document, context.EnabledFixIds, progress, options, cancellationToken).ConfigureAwait(true);
                return newDoc.Project.Solution;
            }
        }

        private async Task<bool> FixAsync(
            Workspace workspace,
            Func<IProgress<CodeAnalysisProgress>, CancellationToken, Task<Solution>> applyFixAsync,
            ICodeCleanUpExecutionContext context)
        {
            using (var scope = context.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Waiting_for_background_work_to_finish))
            {
                var workspaceStatusService = workspace.Services.GetService<IWorkspaceStatusService>();
                if (workspaceStatusService != null)
                {
                    await workspaceStatusService.WaitUntilFullyLoadedAsync(context.OperationContext.UserCancellationToken).ConfigureAwait(true);
                }
            }

            using (var scope = context.OperationContext.AddScope(allowCancellation: true, description: EditorFeaturesResources.Applying_changes))
            {
                var cancellationToken = context.OperationContext.UserCancellationToken;
                var progress = scope.GetCodeAnalysisProgress();

                var solution = await applyFixAsync(progress, cancellationToken).ConfigureAwait(true);

                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                return workspace.TryApplyChanges(solution, progress);
            }
        }

        private async Task<Solution> FixSolutionAsync(
            Solution solution,
            FixIdContainer enabledFixIds,
            IProgress<CodeAnalysisProgress> progressTracker,
            CancellationToken cancellationToken)
        {
            // Prepopulate the solution progress tracker with the total number of documents to process
            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetRequiredProject(projectId);
                if (!CanCleanupProject(project))
                {
                    continue;
                }

                progressTracker.AddItems(project.DocumentIds.Count);
            }

            foreach (var projectId in solution.ProjectIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var project = solution.GetRequiredProject(projectId);
                var newProject = await FixProjectAsync(project, enabledFixIds, progressTracker, addProgressItemsForDocuments: false, cancellationToken).ConfigureAwait(false);
                solution = newProject.Solution;
            }

            return solution;
        }

        private async Task<Project> FixProjectAsync(
            Project project,
            FixIdContainer enabledFixIds,
            IProgress<CodeAnalysisProgress> progressTracker,
            bool addProgressItemsForDocuments,
            CancellationToken cancellationToken)
        {
            if (!CanCleanupProject(project))
            {
                return project;
            }

            if (addProgressItemsForDocuments)
            {
                progressTracker.AddItems(project.DocumentIds.Count);
            }

            var ideOptions = _globalOptions.GetCodeActionOptions(project.Services);

            foreach (var documentId in project.DocumentIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var document = project.GetRequiredDocument(documentId);
                progressTracker.Report(CodeAnalysisProgress.Description(document.Name));

                // FixDocumentAsync reports progress within a document, but we only want to report progress at the
                // project granularity.  So we pass CodeAnalysisProgress.None here so that inner progress updates don't
                // affect us.
                var fixedDocument = await FixDocumentAsync(document, enabledFixIds, CodeAnalysisProgress.None, ideOptions, cancellationToken).ConfigureAwait(false);
                project = fixedDocument.Project;
                progressTracker.ItemCompleted();
            }

            return project;
        }

        private static bool CanCleanupProject(Project project)
            => project.Services.GetService<ICodeCleanupService>() != null;

        private static async Task<Document> FixDocumentAsync(
            Document document,
            FixIdContainer enabledFixIds,
            IProgress<CodeAnalysisProgress> progressTracker,
            CodeActionOptions ideOptions,
            CancellationToken cancellationToken)
        {
            if (document.IsGeneratedCode(cancellationToken))
            {
                return document;
            }

            var codeCleanupService = document.GetRequiredLanguageService<ICodeCleanupService>();

            var enabledDiagnostics = codeCleanupService.GetAllDiagnostics();
            enabledDiagnostics = AdjustDiagnosticOptions(enabledDiagnostics, enabledFixIds.IsFixIdEnabled);

            return await codeCleanupService.CleanupAsync(
                document, enabledDiagnostics, progressTracker, ideOptions.CreateProvider(), cancellationToken).ConfigureAwait(false);
        }
    }
}
