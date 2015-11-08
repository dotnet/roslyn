// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.EncapsulateField;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EncapsulateField
{
    internal abstract class AbstractEncapsulateFieldCommandHandler : ICommandHandler<EncapsulateFieldCommandArgs>
    {
        private readonly IWaitIndicator _waitIndicator;
        private readonly ITextBufferUndoManagerProvider _undoManager;

        public AbstractEncapsulateFieldCommandHandler(
            IWaitIndicator waitIndicator,
            ITextBufferUndoManagerProvider undoManager)
        {
            _waitIndicator = waitIndicator;
            _undoManager = undoManager;
        }

        public void ExecuteCommand(EncapsulateFieldCommandArgs args, Action nextHandler)
        {
            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                nextHandler();
                return;
            }

            var workspace = document.Project.Solution.Workspace;
            var supportsFeatureService = workspace.Services.GetService<IDocumentSupportsFeatureService>();
            if (!supportsFeatureService.SupportsRefactorings(document))
            {
                nextHandler();
                return;
            }

            bool executed = false;
            _waitIndicator.Wait(
                title: EditorFeaturesResources.Encapsulatefield,
                message: EditorFeaturesResources.ApplyingEncapsulateField,
                allowCancel: true,
                action: waitContext =>
            {
                executed = Execute(args, waitContext);
            });

            if (!executed)
            {
                nextHandler();
            }
        }

        private bool Execute(EncapsulateFieldCommandArgs args, IWaitContext waitContext)
        {
            var text = args.TextView.TextBuffer.CurrentSnapshot.AsText();
            var cancellationToken = waitContext.CancellationToken;

            Workspace workspace;
            if (!Workspace.TryGetWorkspace(text.Container, out workspace))
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

            if (result == null)
            {
                var notificationService = workspace.Services.GetService<INotificationService>();
                notificationService.SendNotification(EditorFeaturesResources.PleaseSelectTheDefinitionOf, severity: NotificationSeverity.Error);
                return false;
            }

            waitContext.AllowCancel = false;

            var finalSolution = result.GetSolutionAsync(cancellationToken).WaitAndGetResult(cancellationToken);

            var previewService = workspace.Services.GetService<IPreviewDialogService>();
            if (previewService != null)
            {
                finalSolution = previewService.PreviewChanges(
                    string.Format(EditorFeaturesResources.PreviewChangesOf, EditorFeaturesResources.Encapsulatefield),
                     "vs.csharp.refactoring.preview",
                    EditorFeaturesResources.EncapsulateFieldTitle,
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

            using (var undoTransaction = _undoManager.GetTextBufferUndoManager(args.SubjectBuffer).TextBufferUndoHistory.CreateTransaction(EditorFeaturesResources.Encapsulatefield))
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

        public CommandState GetCommandState(EncapsulateFieldCommandArgs args, Func<CommandState> nextHandler)
        {
            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return nextHandler();
            }

            var supportsFeatureService = document.Project.Solution.Workspace.Services.GetService<IDocumentSupportsFeatureService>();
            if (!supportsFeatureService.SupportsRefactorings(document))
            {
                return nextHandler();
            }

            return CommandState.Available;
        }
    }
}
