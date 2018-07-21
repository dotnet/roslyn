// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.EncapsulateField;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EncapsulateField
{
    internal abstract class AbstractEncapsulateFieldCommandHandler : VSCommanding.ICommandHandler<EncapsulateFieldCommandArgs>
    {
        private readonly ITextBufferUndoManagerProvider _undoManager;
        private readonly IAsynchronousOperationListener _listener;

        public string DisplayName => EditorFeaturesResources.Encapsulate_Field;

        public AbstractEncapsulateFieldCommandHandler(
            ITextBufferUndoManagerProvider undoManager,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _undoManager = undoManager;
            _listener = listenerProvider.GetListener(FeatureAttribute.EncapsulateField);
        }

        public bool ExecuteCommand(EncapsulateFieldCommandArgs args, CommandExecutionContext context)
        {
            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return false;
            }

            var workspace = document.Project.Solution.Workspace;
            var supportsFeatureService = workspace.Services.GetService<IDocumentSupportsFeatureService>();
            if (!supportsFeatureService.SupportsRefactorings(document))
            {
                return false;
            }

            using (var waitScope = context.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Applying_Encapsulate_Field_refactoring))
            {
                return Execute(args, waitScope);
            }
        }

        private bool Execute(EncapsulateFieldCommandArgs args, IUIThreadOperationScope waitScope)
        {
            using (var token = _listener.BeginAsyncOperation("EncapsulateField"))
            {
                var text = args.TextView.TextBuffer.CurrentSnapshot.AsText();
                var cancellationToken = waitScope.Context.UserCancellationToken;
                if (!Workspace.TryGetWorkspace(text.Container, out var workspace))
                {
                    return false;
                }

                var documentId = workspace.GetDocumentIdInCurrentContext(text.Container);
                if (documentId == null)
                {
                    return false;
                }

                var document = workspace.CurrentSolution.GetDocument(documentId);
                if (document == null)
                {
                    return false;
                }

                var spans = args.TextView.Selection.GetSnapshotSpansOnBuffer(args.SubjectBuffer);

                var service = document.GetLanguageService<AbstractEncapsulateFieldService>();

                var result = service.EncapsulateFieldAsync(document, spans.First().Span.ToTextSpan(), true, cancellationToken).WaitAndGetResult(cancellationToken);
                var finalSolution = result?.GetSolutionAsync(cancellationToken).WaitAndGetResult(cancellationToken);

                // This is the last point where the operation can be canceled by the operation context
                // managed by the command system. Make sure to not ignore a cancellation request which
                // occurred prior to this line before proceeding with the Roslyn-managed dialogs.
                cancellationToken.ThrowIfCancellationRequested();

                // We are about to show a modal UI dialog so we should take over the command execution
                // wait context. That means the command system won't attempt to show its own wait dialog 
                // and also will take it into consideration when measuring command handling duration.
                waitScope.Context.TakeOwnership();

                // The cancellation token associated with the operation scope may or may not have been
                // canceled when ownership of the context was taken. Treat the token as invalid and use
                // a different one for future operations in this command handler.
                cancellationToken = CancellationToken.None;

                if (result == null)
                {
                    var notificationService = workspace.Services.GetService<INotificationService>();
                    notificationService.SendNotification(EditorFeaturesResources.Please_select_the_definition_of_the_field_to_encapsulate, severity: NotificationSeverity.Error);
                    return false;
                }

                var previewService = workspace.Services.GetService<IPreviewDialogService>();
                if (previewService != null)
                {
                    finalSolution = previewService.PreviewChanges(
                        string.Format(EditorFeaturesResources.Preview_Changes_0, EditorFeaturesResources.Encapsulate_Field),
                         "vs.csharp.refactoring.preview",
                        EditorFeaturesResources.Encapsulate_Field_colon,
                        result.GetNameAsync(cancellationToken).WaitAndGetResult(cancellationToken),
                        result.GetGlyphAsync(cancellationToken).WaitAndGetResult(cancellationToken),
                        finalSolution,
                        document.Project.Solution);
                }

                if (finalSolution == null)
                {
                    // User clicked cancel.
                    return true;
                }

                using (var undoTransaction = _undoManager.GetTextBufferUndoManager(args.SubjectBuffer).TextBufferUndoHistory.CreateTransaction(EditorFeaturesResources.Encapsulate_Field))
                {
                    if (!workspace.TryApplyChanges(finalSolution))
                    {
                        undoTransaction.Cancel();
                        return false;
                    }

                    undoTransaction.Complete();
                }

                return true;
            }
        }

        public VSCommanding.CommandState GetCommandState(EncapsulateFieldCommandArgs args)
        {
            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return VSCommanding.CommandState.Unspecified;
            }

            var supportsFeatureService = document.Project.Solution.Workspace.Services.GetService<IDocumentSupportsFeatureService>();
            if (!supportsFeatureService.SupportsRefactorings(document))
            {
                return VSCommanding.CommandState.Unspecified;
            }

            return VSCommanding.CommandState.Available;
        }
    }
}
