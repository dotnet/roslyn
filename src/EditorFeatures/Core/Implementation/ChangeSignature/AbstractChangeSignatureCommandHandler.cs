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

namespace Microsoft.CodeAnalysis.Editor.Implementation.ChangeSignature
{
    internal abstract class AbstractChangeSignatureCommandHandler : ICommandHandler<ReorderParametersCommandArgs>, ICommandHandler<RemoveParametersCommandArgs>
    {
        public CommandState GetCommandState(ReorderParametersCommandArgs args, Func<CommandState> nextHandler)
        {
            return GetCommandState(args.SubjectBuffer, nextHandler);
        }

        public CommandState GetCommandState(RemoveParametersCommandArgs args, Func<CommandState> nextHandler)
        {
            return GetCommandState(args.SubjectBuffer, nextHandler);
        }

        private static CommandState GetCommandState(ITextBuffer subjectBuffer, Func<CommandState> nextHandler)
        {
            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null ||
                !document.Project.Solution.Workspace.CanApplyChange(ApplyChangesKind.ChangeDocument))
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

        public void ExecuteCommand(RemoveParametersCommandArgs args, Action nextHandler)
        {
            ExecuteCommand(args.TextView, args.SubjectBuffer, nextHandler);
        }

        public void ExecuteCommand(ReorderParametersCommandArgs args, Action nextHandler)
        {
            ExecuteCommand(args.TextView, args.SubjectBuffer, nextHandler);
        }

        private static void ExecuteCommand(ITextView textView, ITextBuffer subjectBuffer, Action nextHandler)
        {
            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                nextHandler();
                return;
            }

            // TODO: reuse GetCommandState instead
            var workspace = document.Project.Solution.Workspace;
            if (!workspace.CanApplyChange(ApplyChangesKind.ChangeDocument))
            {
                nextHandler();
                return;
            }

            var supportsFeatureService = document.Project.Solution.Workspace.Services.GetService<IDocumentSupportsFeatureService>();
            if (!supportsFeatureService.SupportsRefactorings(document))
            {
                nextHandler();
                return;
            }

            var caretPoint = textView.GetCaretPoint(subjectBuffer);
            if (!caretPoint.HasValue)
            {
                nextHandler();
                return;
            }

            // Reorder parameters is not cancellable.
            var reorderParametersService = document.GetLanguageService<AbstractChangeSignatureService>();
            var result = reorderParametersService.ChangeSignature(
                document,
                caretPoint.Value.Position,
                (errorMessage, severity) => workspace.Services.GetService<INotificationService>().SendNotification(errorMessage, severity: severity),
                CancellationToken.None);

            if (result == null || !result.Succeeded)
            {
                return;
            }

            var finalSolution = result.UpdatedSolution;

            var previewService = workspace.Services.GetService<IPreviewDialogService>();
            if (previewService != null && result.PreviewChanges)
            {
                finalSolution = previewService.PreviewChanges(
                    string.Format(EditorFeaturesResources.PreviewChangesOf, EditorFeaturesResources.ChangeSignature),
                    "vs.csharp.refactoring.preview",
                    EditorFeaturesResources.ChangeSignatureTitle,
                    result.Name,
                    result.Glyph.GetValueOrDefault(),
                    result.UpdatedSolution,
                    document.Project.Solution);
            }

            if (finalSolution == null)
            {
                // User clicked cancel.
                return;
            }

            using (var workspaceUndoTransaction = workspace.OpenGlobalUndoTransaction(FeaturesResources.ChangeSignature))
            {
                if (!workspace.TryApplyChanges(finalSolution))
                {
                    // TODO: handle failure
                    return;
                }

                workspaceUndoTransaction.Commit();
            }
        }
    }
}
