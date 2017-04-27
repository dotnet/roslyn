// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Undo;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.UI.Commanding.Commands;
using VSC = Microsoft.VisualStudio.Text.UI.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.ChangeSignature
{
    internal abstract class AbstractChangeSignatureCommandHandler : VSC.ICommandHandler<ReorderParametersCommandArgs>, VSC.ICommandHandler<RemoveParametersCommandArgs>
    {
        private readonly IWaitIndicator _waitIndicator;

        public bool InterestedInReadOnlyBuffer => false;

        protected AbstractChangeSignatureCommandHandler(
            IWaitIndicator waitIndicator)
        {
            _waitIndicator = waitIndicator;
        }

        public VSC.CommandState GetCommandState(ReorderParametersCommandArgs args)
            => GetCommandState(args.SubjectBuffer);

        public VSC.CommandState GetCommandState(RemoveParametersCommandArgs args)
            => GetCommandState(args.SubjectBuffer);

        private static VSC.CommandState GetCommandState(ITextBuffer subjectBuffer)
        {
            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null ||
                !document.Project.Solution.Workspace.CanApplyChange(ApplyChangesKind.ChangeDocument))
            {
                return VSC.CommandState.CommandIsUnavailable;
            }

            var supportsFeatureService = document.Project.Solution.Workspace.Services.GetService<IDocumentSupportsFeatureService>();
            if (!supportsFeatureService.SupportsRefactorings(document))
            {
                return VSC.CommandState.CommandIsUnavailable;
            }

            return VSC.CommandState.CommandIsAvailable;
        }

        public bool ExecuteCommand(RemoveParametersCommandArgs args)
            => ExecuteCommand(args.TextView, args.SubjectBuffer);

        public bool ExecuteCommand(ReorderParametersCommandArgs args)
            => ExecuteCommand(args.TextView, args.SubjectBuffer);

        private bool ExecuteCommand(ITextView textView, ITextBuffer subjectBuffer)
        {
            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return false;
            }

            // TODO: reuse GetCommandState instead
            var workspace = document.Project.Solution.Workspace;
            if (!workspace.CanApplyChange(ApplyChangesKind.ChangeDocument))
            {
                return false;
            }

            var supportsFeatureService = document.Project.Solution.Workspace.Services.GetService<IDocumentSupportsFeatureService>();
            if (!supportsFeatureService.SupportsRefactorings(document))
            {
                return false;
            }

            var caretPoint = textView.GetCaretPoint(subjectBuffer);
            if (!caretPoint.HasValue)
            {
                return false;
            }

            ChangeSignatureResult result = null;
            var waitResult = _waitIndicator.Wait(
                FeaturesResources.Change_signature,
                allowCancel: true,
                action: w =>
                {
                    var reorderParametersService = document.GetLanguageService<AbstractChangeSignatureService>();
                    result = reorderParametersService.ChangeSignature(
                        document,
                        caretPoint.Value.Position,
                        (errorMessage, severity) => workspace.Services.GetService<INotificationService>().SendNotification(errorMessage, severity: severity),
                        w.CancellationToken);
                });

            if (waitResult == WaitIndicatorResult.Canceled)
            {
                return true;
            }

            if (result == null || !result.Succeeded)
            {
                return true;
            }

            var finalSolution = result.UpdatedSolution;

            var previewService = workspace.Services.GetService<IPreviewDialogService>();
            if (previewService != null && result.PreviewChanges)
            {
                finalSolution = previewService.PreviewChanges(
                    string.Format(EditorFeaturesResources.Preview_Changes_0, EditorFeaturesResources.Change_Signature),
                    "vs.csharp.refactoring.preview",
                    EditorFeaturesResources.Change_Signature_colon,
                    result.Name,
                    result.Glyph.GetValueOrDefault(),
                    result.UpdatedSolution,
                    document.Project.Solution);
            }

            if (finalSolution == null)
            {
                // User clicked cancel.
                return true;
            }

            using (var workspaceUndoTransaction = workspace.OpenGlobalUndoTransaction(FeaturesResources.Change_signature))
            {
                if (!workspace.TryApplyChanges(finalSolution))
                {
                    // TODO: handle failure
                    return true;
                }

                workspaceUndoTransaction.Commit();
            }

            return true;
        }
    }
}
