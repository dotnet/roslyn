// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeActions
{
#pragma warning disable RS0030 // Do not used banned APIs
    /// <summary>
    /// A <see cref="CodeActionOperation"/> for applying solution changes to a workspace.
    /// <see cref="CodeAction.GetOperationsAsync(CancellationToken)"/> may return at most one
    /// <see cref="ApplyChangesOperation"/>. Hosts may provide custom handling for 
    /// <see cref="ApplyChangesOperation"/>s, but if a <see cref="CodeAction"/> requires custom
    /// host behavior not supported by a single <see cref="ApplyChangesOperation"/>, then instead:
    /// <list type="bullet">
    /// <description><text>Implement a custom <see cref="CodeAction"/> and <see cref="CodeActionOperation"/>s</text></description>
    /// <description><text>Do not return any <see cref="ApplyChangesOperation"/> from <see cref="CodeAction.GetOperationsAsync(CancellationToken)"/></text></description>
    /// <description><text>Directly apply any workspace edits</text></description>
    /// <description><text>Handle any custom host behavior</text></description>
    /// <description><text>Produce a preview for <see cref="CodeAction.GetPreviewOperationsAsync(CancellationToken)"/> 
    ///   by creating a custom <see cref="PreviewOperation"/> or returning a single <see cref="ApplyChangesOperation"/>
    ///   to use the built-in preview mechanism</text></description>
    /// </list>
    /// </summary>
#pragma warning restore RS0030 // Do not used banned APIs
    public sealed class ApplyChangesOperation(Solution changedSolution) : CodeActionOperation
    {
        public Solution ChangedSolution { get; } = changedSolution ?? throw new ArgumentNullException(nameof(changedSolution));

        internal override bool ApplyDuringTests => true;

        public override void Apply(Workspace workspace, CancellationToken cancellationToken)
            => workspace.TryApplyChanges(ChangedSolution, CodeAnalysisProgress.None);

        internal sealed override Task<bool> TryApplyAsync(Workspace workspace, Solution originalSolution, IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken)
            => Task.FromResult(ApplyOrMergeChanges(workspace, originalSolution, ChangedSolution, progressTracker, cancellationToken));

        internal static bool ApplyOrMergeChanges(
            Workspace workspace,
            Solution originalSolution,
            Solution changedSolution,
            IProgress<CodeAnalysisProgress> progressTracker,
            CancellationToken cancellationToken)
        {
            var currentSolution = workspace.CurrentSolution;

            // if there was no intermediary edit, just apply the change fully.
            if (changedSolution.WorkspaceVersion == currentSolution.WorkspaceVersion)
            {
                var result = workspace.TryApplyChanges(changedSolution, progressTracker);

                Logger.Log(
                    result ? FunctionId.ApplyChangesOperation_WorkspaceVersionMatch_ApplicationSucceeded : FunctionId.ApplyChangesOperation_WorkspaceVersionMatch_ApplicationFailed,
                    logLevel: LogLevel.Information);

                return result;
            }

            // Otherwise, we need to see what changes were actually made and see if we can apply them.  The general rules are:
            //
            // 1. we only support text changes when doing merges.  Any other changes to projects/documents are not
            //    supported because it's very unclear what impact they may have wrt other workspace updates that have
            //    already happened.
            //
            // 2. For text changes, we only support it if the current text of the document we're changing itself has not
            //    changed. This means we can merge in edits if there were changes to unrelated files, but not if there
            //    are changes to the current file.  We could consider relaxing this in the future, esp. if we make use
            //    of some sort of text-merging-library to handle this.  However, the user would then have to handle diff
            //    markers being inserted into their code that they then have to handle.

            var solutionChanges = changedSolution.GetChanges(originalSolution);

            if (solutionChanges.GetAddedProjects().Any() ||
                solutionChanges.GetAddedAnalyzerReferences().Any() ||
                solutionChanges.GetRemovedProjects().Any() ||
                solutionChanges.GetRemovedAnalyzerReferences().Any())
            {
                Logger.Log(FunctionId.ApplyChangesOperation_WorkspaceVersionMismatch_ApplicationFailed_IncompatibleSolutionChange, logLevel: LogLevel.Information);
                return false;
            }

            // Take the actual current solution the workspace is pointing to and fork it with just the text changes the
            // code action wanted to make.  Then apply that fork back into the workspace.
            var forkedSolution = currentSolution;

            foreach (var changedProject in solutionChanges.GetProjectChanges())
            {
                // We only support text changes.  If we see any other changes to this project, bail out immediately.
                if (changedProject.GetAddedAdditionalDocuments().Any() ||
                    changedProject.GetAddedAnalyzerConfigDocuments().Any() ||
                    changedProject.GetAddedAnalyzerReferences().Any() ||
                    changedProject.GetAddedDocuments().Any() ||
                    changedProject.GetAddedMetadataReferences().Any() ||
                    changedProject.GetAddedProjectReferences().Any() ||
                    changedProject.GetRemovedAdditionalDocuments().Any() ||
                    changedProject.GetRemovedAnalyzerConfigDocuments().Any() ||
                    changedProject.GetRemovedAnalyzerReferences().Any() ||
                    changedProject.GetRemovedDocuments().Any() ||
                    changedProject.GetRemovedMetadataReferences().Any() ||
                    changedProject.GetRemovedProjectReferences().Any())
                {
                    Logger.Log(FunctionId.ApplyChangesOperation_WorkspaceVersionMismatch_ApplicationFailed_IncompatibleProjectChange, logLevel: LogLevel.Information);
                    return false;
                }

                // We have to at least have some changed document
                var changedDocuments = changedProject.GetChangedDocuments()
                    .Concat(changedProject.GetChangedAdditionalDocuments())
                    .Concat(changedProject.GetChangedAnalyzerConfigDocuments()).ToImmutableArray();

                if (changedDocuments.Length == 0)
                {
                    Logger.Log(FunctionId.ApplyChangesOperation_WorkspaceVersionMismatch_ApplicationFailed_NoChangedDocument, logLevel: LogLevel.Information);
                    return false;
                }

                foreach (var documentId in changedDocuments)
                {
                    var originalDocument = changedProject.OldProject.Solution.GetRequiredTextDocument(documentId);
                    var changedDocument = changedProject.NewProject.Solution.GetRequiredTextDocument(documentId);

                    // it has to be a text change the operation wants to make.  If the operation is making some other
                    // sort of change, we can't merge this operation in.
                    if (!changedDocument.HasTextChanged(originalDocument, ignoreUnchangeableDocument: false))
                    {
                        Logger.Log(FunctionId.ApplyChangesOperation_WorkspaceVersionMismatch_ApplicationFailed_NoTextChange, logLevel: LogLevel.Information);
                        return false;
                    }

                    // If the document has gone away, we definitely cannot apply a text change to it.
                    var currentDocument = currentSolution.GetTextDocument(documentId);
                    if (currentDocument is null)
                    {
                        Logger.Log(FunctionId.ApplyChangesOperation_WorkspaceVersionMismatch_ApplicationFailed_DocumentRemoved, logLevel: LogLevel.Information);
                        return false;
                    }

                    // If the file contents changed in the current workspace, then we can't apply this change to it.
                    // Note: we could potentially try to do a 3-way merge in the future, including handling conflicts
                    // with that.  For now though, we'll leave that out of scope.
                    if (originalDocument.HasTextChanged(currentDocument, ignoreUnchangeableDocument: false))
                    {
                        Logger.Log(FunctionId.ApplyChangesOperation_WorkspaceVersionMismatch_ApplicationFailed_TextChangeConflict, logLevel: LogLevel.Information);
                        return false;
                    }

                    forkedSolution = forkedSolution.WithTextDocumentText(documentId, changedDocument.GetTextSynchronously(cancellationToken));
                }
            }

            Logger.Log(FunctionId.ApplyChangesOperation_WorkspaceVersionMismatch_ApplicationSucceeded, logLevel: LogLevel.Information);
            return workspace.TryApplyChanges(forkedSolution, progressTracker);
        }
    }
}
