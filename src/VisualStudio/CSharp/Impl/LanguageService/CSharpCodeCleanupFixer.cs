// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor.CodeCleanup;
using Microsoft.VisualStudio.Language.CodeCleanUp;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeCleanup;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using __VSHPROPID8 = Microsoft.VisualStudio.Shell.Interop.__VSHPROPID8;
using IVsHierarchyItemManager = Microsoft.VisualStudio.Shell.IVsHierarchyItemManager;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService
{
    [Export(typeof(CodeCleanUpFixer))]
    [VisualStudio.Utilities.ContentType(ContentTypeNames.CSharpContentType)]
    internal partial class CSharpCodeCleanUpFixer : CodeCleanUpFixer
    {
        private const string RemoveUnusedImportsFixId = "RemoveUnusedImportsFixId";
        private const string SortImportsFixId = "SortImportsFixId";

        private readonly IThreadingContext _threadingContext;
        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly IVsHierarchyItemManager _vsHierarchyItemManager;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpCodeCleanUpFixer(IThreadingContext threadingContext, VisualStudioWorkspaceImpl workspace, IVsHierarchyItemManager vsHierarchyItemManager)
        {
            _threadingContext = threadingContext;
            _workspace = workspace;
            _vsHierarchyItemManager = vsHierarchyItemManager;
        }

        public override Task<bool> FixAsync(ICodeCleanUpScope scope, ICodeCleanUpExecutionContext context)
        {
            switch (scope)
            {
                case TextBufferCodeCleanUpScope textBufferScope:
                    return FixTextBufferAsync(textBufferScope, context);
                case IVsHierarchyCodeCleanupScope hierarchyContentScope:
                    return FixHierarchyContentAsync(hierarchyContentScope, context);
                default:
                    return Task.FromResult(false);
            }
        }

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
            context.OperationContext.UserCancellationToken.ThrowIfCancellationRequested();

            ProjectId projectId = null;
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

                    return await FixDocumentAsync(solution.GetDocument(documentId), context).ConfigureAwait(true);
                }
            }

            return false;
        }

        private Task<bool> FixSolutionAsync(Solution solution, ICodeCleanUpExecutionContext context)
        {
            var solutionName = Path.GetFileName(solution.FilePath);
            return FixAsync(solution.Workspace, ApplyFixAsync, context, solutionName);

            // Local function
            Task<Solution> ApplyFixAsync(ProgressTracker progressTracker, CancellationToken cancellationToken)
            {
                return FixSolutionAsync(solution, context.EnabledFixIds, progressTracker, cancellationToken);
            }
        }

        private Task<bool> FixProjectAsync(Project project, ICodeCleanUpExecutionContext context)
        {
            return FixAsync(project.Solution.Workspace, ApplyFixAsync, context, project.Name);

            // Local function
            async Task<Solution> ApplyFixAsync(ProgressTracker progressTracker, CancellationToken cancellationToken)
            {
                var newProject = await FixProjectAsync(project, context.EnabledFixIds, progressTracker, addProgressItemsForDocuments: true, cancellationToken).ConfigureAwait(true);
                return newProject.Solution;
            }
        }

        private Task<bool> FixDocumentAsync(Document document, ICodeCleanUpExecutionContext context)
        {
            return FixAsync(document.Project.Solution.Workspace, ApplyFixAsync, context, document.Name);

            // Local function
            async Task<Solution> ApplyFixAsync(ProgressTracker progressTracker, CancellationToken cancellationToken)
            {
                var newDocument = await FixDocumentAsync(document, context.EnabledFixIds, progressTracker, cancellationToken).ConfigureAwait(true);
                return newDocument.Project.Solution;
            }
        }

        private Task<bool> FixTextBufferAsync(TextBufferCodeCleanUpScope textBufferScope, ICodeCleanUpExecutionContext context)
        {
            var buffer = textBufferScope.SubjectBuffer;
            var document = buffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return SpecializedTasks.False;
            }

            return FixAsync(buffer.GetWorkspace(), ApplyFixAsync, context, document.Name);

            // Local function
            async Task<Solution> ApplyFixAsync(ProgressTracker progressTracker, CancellationToken cancellationToken)
            {
                var document = buffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                var newDoc = await FixDocumentAsync(document, context.EnabledFixIds, progressTracker, cancellationToken).ConfigureAwait(true);
                return newDoc.Project.Solution;
            }
        }

        private async Task<bool> FixAsync(
            Workspace workspace,
            Func<ProgressTracker, CancellationToken, Task<Solution>> applyFixAsync,
            ICodeCleanUpExecutionContext context,
            string contextName)
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
                var progressTracker = new ProgressTracker((description, completed, total) =>
                {
                    if (scope != null)
                    {
                        scope.Description = description;
                        scope.Progress.Report(new VisualStudio.Utilities.ProgressInfo(completed, total));
                    }
                });

                var solution = await applyFixAsync(progressTracker, cancellationToken).ConfigureAwait(true);

                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                return workspace.TryApplyChanges(solution, progressTracker);
            }
        }

        private async Task<Solution> FixSolutionAsync(
            Solution solution,
            FixIdContainer enabledFixIds,
            ProgressTracker progressTracker,
            CancellationToken cancellationToken)
        {
            // Prepopulate the solution progress tracker with the total number of documents to process
            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetProject(projectId);
                if (!CanCleanupProject(project))
                {
                    continue;
                }

                progressTracker.AddItems(project.DocumentIds.Count);
            }

            foreach (var projectId in solution.ProjectIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var project = solution.GetProject(projectId);
                var newProject = await FixProjectAsync(project, enabledFixIds, progressTracker, addProgressItemsForDocuments: false, cancellationToken).ConfigureAwait(false);
                solution = newProject.Solution;
            }

            return solution;
        }

        private async Task<Project> FixProjectAsync(
            Project project,
            FixIdContainer enabledFixIds,
            ProgressTracker progressTracker,
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

            foreach (var documentId in project.DocumentIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var document = project.GetDocument(documentId);
                progressTracker.Description = document.Name;

                // FixDocumentAsync reports progress within a document, but we limit progress reporting for a project
                // to the current document.
                var documentProgressTracker = new ProgressTracker();

                var fixedDocument = await FixDocumentAsync(document, enabledFixIds, documentProgressTracker, cancellationToken).ConfigureAwait(false);
                project = fixedDocument.Project;
                progressTracker.ItemCompleted();
            }

            return project;
        }

        private static bool CanCleanupProject(Project project)
            => project.LanguageServices.GetService<ICodeCleanupService>() != null;

        private async Task<Document> FixDocumentAsync(
            Document document,
            FixIdContainer enabledFixIds,
            ProgressTracker progressTracker,
            CancellationToken cancellationToken)
        {
            if (document.IsGeneratedCode(cancellationToken))
            {
                return document;
            }

            var codeCleanupService = document.GetLanguageService<ICodeCleanupService>();

            var allDiagnostics = codeCleanupService.GetAllDiagnostics();

            var enabedDiagnosticSets = ArrayBuilder<DiagnosticSet>.GetInstance();

            foreach (var diagnostic in allDiagnostics.Diagnostics)
            {
                foreach (var diagnosticId in diagnostic.DiagnosticIds)
                {
                    if (enabledFixIds.IsFixIdEnabled(diagnosticId))
                    {
                        enabedDiagnosticSets.Add(diagnostic);
                        break;
                    }
                }
            }

            var isRemoveUnusedUsingsEnabled = enabledFixIds.IsFixIdEnabled(RemoveUnusedImportsFixId);
            var isSortUsingsEnabled = enabledFixIds.IsFixIdEnabled(SortImportsFixId);
            var enabledDiagnostics = new EnabledDiagnosticOptions(enabedDiagnosticSets.ToImmutableArray(),
                new OrganizeUsingsSet(isRemoveUnusedUsingsEnabled, isSortUsingsEnabled));

            return await codeCleanupService.CleanupAsync(
                document, enabledDiagnostics, progressTracker, cancellationToken).ConfigureAwait(false);
        }
    }
}
