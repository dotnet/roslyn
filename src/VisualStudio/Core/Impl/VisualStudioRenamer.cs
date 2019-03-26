// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Undo;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices
{
    [Export(typeof(VisualStudioSymbolRenamer))]
    public sealed class VisualStudioSymbolRenamer
    {
        private readonly IEnumerable<IRefactorNotifyService> _refactorNotifyServices;

        [ImportingConstructor]
        private VisualStudioSymbolRenamer([ImportMany] IEnumerable<IRefactorNotifyService> refactorNotifyServices)
        {
            _refactorNotifyServices = refactorNotifyServices;
        }

        public Task<bool> TryRenameSymbolAsync(VisualStudioWorkspace visualStudioWorkspace,
                                               ISymbol symbol,
                                               string newName,
                                               CancellationToken cancellationToken)
        {
            return TryRenameSymbolImplAsync(visualStudioWorkspace, symbol, newName, throwOnFailure: false, cancellationToken);
        }

        public async Task RenameSymbolAsync(VisualStudioWorkspace visualStudioWorkspace,
                                            ISymbol symbol,
                                            string newName,
                                            CancellationToken cancellationToken)
        {
            _ = await TryRenameSymbolImplAsync(visualStudioWorkspace, symbol, newName, throwOnFailure: true, cancellationToken).ConfigureAwait(false);
        }

        private async Task<bool> TryRenameSymbolImplAsync(VisualStudioWorkspace visualStudioWorkspace,
                                                         ISymbol symbol,
                                                         string newName,
                                                         bool throwOnFailure,
                                                         CancellationToken cancellationToken)
        {
            var oldSolution = visualStudioWorkspace.CurrentSolution;
            var newSolution = await Renamer.RenameSymbolAsync(oldSolution, symbol, newName, oldSolution.Options, cancellationToken).ConfigureAwait(false);
            var changedDocumentIDs = newSolution.GetChangedDocuments(oldSolution);

            // Notify third parties of the coming rename operation
            if (!_refactorNotifyServices.TryOnBeforeGlobalSymbolRenamed(visualStudioWorkspace, changedDocumentIDs, symbol, newName, throwOnFailure: throwOnFailure))
            {
                var notificationService = visualStudioWorkspace.Services.GetService<INotificationService>();
                notificationService.SendNotification(
                    EditorFeaturesResources.Rename_operation_was_cancelled_or_is_not_valid,
                    EditorFeaturesResources.Rename_Symbol,
                    NotificationSeverity.Error);
                return false;
            }

            // Begin undo transaction
            using (var undoTransaction = visualStudioWorkspace.OpenGlobalUndoTransaction(EditorFeaturesResources.Rename_Symbol, useFallback: !throwOnFailure))
            {
                var finalSolution = newSolution.Workspace.CurrentSolution;
                foreach (var id in changedDocumentIDs)
                {
                    // If the document supports syntax tree, then create the new solution from the
                    // updated syntax root.  This should ensure that annotations are preserved, and
                    // prevents the solution from having to re-parse documents when we already have
                    // the trees for them.  If we don't support syntax, then just use the text of
                    // the document.
                    var newDocument = newSolution.GetDocument(id);
                    if (newDocument.SupportsSyntaxTree)
                    {
                        var root = await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                        finalSolution = finalSolution.WithDocumentSyntaxRoot(id, root);
                    }
                    else
                    {
                        var newText = await newDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                        finalSolution = finalSolution.WithDocumentText(id, newText);
                    }
                }

                // Update the workspace.
                if (visualStudioWorkspace.TryApplyChanges(finalSolution))
                {
                    // Notify third parties of the completed rename operation
                    if (!_refactorNotifyServices.TryOnAfterGlobalSymbolRenamed(visualStudioWorkspace, changedDocumentIDs, symbol, newName, throwOnFailure: throwOnFailure))
                    {
                        var notificationService = visualStudioWorkspace.Services.GetService<INotificationService>();
                        notificationService.SendNotification(
                            EditorFeaturesResources.Rename_operation_was_not_properly_completed_Some_file_might_not_have_been_updated,
                            EditorFeaturesResources.Rename_Symbol,
                            NotificationSeverity.Information);
                        return false;
                    }
                }
                else if (throwOnFailure)
                {
                    throw Exceptions.ThrowEFail();
                }

                undoTransaction.Commit();
            }

            RenameTrackingDismisser.DismissRenameTracking(visualStudioWorkspace, changedDocumentIDs);
            return true;
        }
    }
}
