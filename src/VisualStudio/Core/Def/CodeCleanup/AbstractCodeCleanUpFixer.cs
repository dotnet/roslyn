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
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Progress;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor.CodeCleanup;
using Microsoft.VisualStudio.Language.CodeCleanUp;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using __VSHPROPID8 = Microsoft.VisualStudio.Shell.Interop.__VSHPROPID8;
using IVsHierarchyItemManager = Microsoft.VisualStudio.Shell.IVsHierarchyItemManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeCleanup;

/// <summary>
/// Roslyn implementations of <see cref="ICodeCleanUpFixer"/> extend this class. Since other extensions could also
/// be implementing the <see cref="ICodeCleanUpFixer"/> interface, this abstract base class allows Roslyn to operate
/// on MEF instances of fixers known to be relevant in the context of Roslyn languages.
/// </summary>
internal abstract partial class AbstractCodeCleanUpFixer(
    IThreadingContext threadingContext,
    VisualStudioWorkspaceImpl workspace,
    IVsHierarchyItemManager vsHierarchyItemManager) : ICodeCleanUpFixer
{
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly VisualStudioWorkspaceImpl _workspace = workspace;
    private readonly IVsHierarchyItemManager _vsHierarchyItemManager = vsHierarchyItemManager;

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
            var solution = _workspace.CurrentSolution;
            return await FixAsync(
                _workspace,
                // Just defer to FixProjectsAsync, passing in all fixable projects in the solution.
                (progress, cancellationToken) => FixProjectsAsync(
                    solution, [.. solution.Projects.Where(p => p.SupportsCompilation)], context.EnabledFixIds, progress, cancellationToken),
                context).ConfigureAwait(false);
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
                return false;
        }

        var itemId = hierarchyContent.ItemId;
        if (itemId == (uint)VSConstants.VSITEMID.Root)
        {
            await TaskScheduler.Default;

            var project = _workspace.CurrentSolution.GetProject(projectId);
            if (project == null || !project.SupportsCompilation)
                return false;

            return await FixAsync(
                _workspace,
                // Just defer to FixProjectsAsync, passing in this single project to fix.
                (progress, cancellationToken) => FixProjectsAsync(
                    project.Solution, [project], context.EnabledFixIds, progress, cancellationToken),
                context).ConfigureAwait(false);
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
                    return false;

                var document = solution.GetRequiredDocument(documentId);

                return await FixAsync(
                    _workspace,
                    async (progress, cancellationToken) =>
                    {
                        var newDocument = await FixDocumentAsync(document, context.EnabledFixIds, progress, cancellationToken).ConfigureAwait(true);
                        return newDocument.Project.Solution;
                    },
                    context).ConfigureAwait(false);
            }
        }

        return false;
    }

    private Task<bool> FixTextBufferAsync(TextBufferCodeCleanUpScope textBufferScope, ICodeCleanUpExecutionContext context)
    {
        var buffer = textBufferScope.SubjectBuffer;

        // Let LSP handle code cleanup in the cloud scenario
        if (buffer.IsInLspEditorContext())
            return SpecializedTasks.False;

        var document = buffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (document == null)
            return SpecializedTasks.False;

        var workspace = buffer.GetWorkspace();
        if (workspace is not VisualStudioWorkspace visualStudioWorkspace)
            return SpecializedTasks.False;

        return FixAsync(visualStudioWorkspace, ApplyFixAsync, context);

        // Local function
        async Task<Solution> ApplyFixAsync(IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
        {
            var document = buffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            Contract.ThrowIfNull(document);

            var newDoc = await FixDocumentAsync(document, context.EnabledFixIds, progress, cancellationToken).ConfigureAwait(true);
            return newDoc.Project.Solution;
        }
    }

    private async Task<bool> FixAsync(
        VisualStudioWorkspace workspace,
        Func<IProgress<CodeAnalysisProgress>, CancellationToken, Task<Solution>> applyFixAsync,
        ICodeCleanUpExecutionContext context)
    {
        using (var scope = context.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Waiting_for_background_work_to_finish))
        {
            var workspaceStatusService = workspace.Services.GetService<IWorkspaceStatusService>();
            if (workspaceStatusService != null)
                await workspaceStatusService.WaitUntilFullyLoadedAsync(context.OperationContext.UserCancellationToken).ConfigureAwait(true);
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

    private static async Task<Solution> FixProjectsAsync(
        Solution solution,
        ImmutableArray<Project> projects,
        FixIdContainer enabledFixIds,
        IProgress<CodeAnalysisProgress> progressTracker,
        CancellationToken cancellationToken)
    {
        // Add an item for each document in all the projects we're processing.
        progressTracker.AddItems(projects.Sum(static p => p.DocumentIds.Count));

        // Run in parallel across all projects.
        var changedRoots = await ProducerConsumer<(DocumentId documentId, SyntaxNode newRoot)>.RunParallelAsync(
            source: projects,
            produceItems: static async (project, callback, args, cancellationToken) =>
            {
                Contract.ThrowIfFalse(project.SupportsCompilation);

                var (solution, enabledFixIds, progressTracker) = args;
                cancellationToken.ThrowIfCancellationRequested();

                // And for each project, process all the documents in parallel.
                await Parallel.ForEachAsync(
                    source: project.Documents,
                    cancellationToken,
                    async (document, cancellationToken) =>
                    {
                        using var _ = progressTracker.ItemCompletedScope();

                        // FixDocumentAsync reports progress within a document, but we only want to report progress at
                        // the document granularity.  So we pass CodeAnalysisProgress.None here so that inner progress
                        // updates don't affect us.
                        var fixedDocument = await FixDocumentAsync(document, enabledFixIds, CodeAnalysisProgress.None, cancellationToken).ConfigureAwait(false);
                        if (fixedDocument == document)
                            return;

                        callback((document.Id, await fixedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false)));
                    }).ConfigureAwait(false);
            },
            args: (solution, enabledFixIds, progressTracker),
            cancellationToken).ConfigureAwait(false);

        return solution.WithDocumentSyntaxRoots(changedRoots);
    }

    private static async Task<Document> FixDocumentAsync(
        Document document,
        FixIdContainer enabledFixIds,
        IProgress<CodeAnalysisProgress> progressTracker,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (document.IsGeneratedCode(cancellationToken))
            return document;

        var codeCleanupService = document.GetRequiredLanguageService<ICodeCleanupService>();

        var enabledDiagnostics = codeCleanupService.GetAllDiagnostics();
        enabledDiagnostics = AdjustDiagnosticOptions(enabledDiagnostics, enabledFixIds.IsFixIdEnabled);

        return await codeCleanupService.CleanupAsync(
            document, enabledDiagnostics, progressTracker, cancellationToken).ConfigureAwait(false);
    }
}
