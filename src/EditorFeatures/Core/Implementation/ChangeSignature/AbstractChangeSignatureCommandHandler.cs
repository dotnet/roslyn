// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Undo;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.ChangeSignature
{
    internal abstract class AbstractChangeSignatureCommandHandler : VSCommanding.ICommandHandler<ReorderParametersCommandArgs>,
        VSCommanding.ICommandHandler<RemoveParametersCommandArgs>
    {
        private readonly IThreadingContext _threadingContext;

        protected AbstractChangeSignatureCommandHandler(IThreadingContext threadingContext)
        {
            _threadingContext = threadingContext;
        }

        public string DisplayName => EditorFeaturesResources.Change_Signature;

        public VSCommanding.CommandState GetCommandState(ReorderParametersCommandArgs args)
            => GetCommandState(args.SubjectBuffer);

        public VSCommanding.CommandState GetCommandState(RemoveParametersCommandArgs args)
            => GetCommandState(args.SubjectBuffer);

        private static VSCommanding.CommandState GetCommandState(ITextBuffer subjectBuffer)
            => IsAvailable(subjectBuffer, out _) ? VSCommanding.CommandState.Available : VSCommanding.CommandState.Unspecified;

        public bool ExecuteCommand(RemoveParametersCommandArgs args, CommandExecutionContext context)
            => ExecuteCommand(args.TextView, args.SubjectBuffer, context);

        public bool ExecuteCommand(ReorderParametersCommandArgs args, CommandExecutionContext context)
            => ExecuteCommand(args.TextView, args.SubjectBuffer, context);

        private static bool IsAvailable(ITextBuffer subjectBuffer, out Workspace workspace)
            => subjectBuffer.TryGetWorkspace(out workspace) &&
               workspace.CanApplyChange(ApplyChangesKind.ChangeDocument) &&
               subjectBuffer.SupportsRefactorings();

        private bool ExecuteCommand(ITextView textView, ITextBuffer subjectBuffer, CommandExecutionContext context)
        {
            using (context.OperationContext.AddScope(allowCancellation: true, FeaturesResources.Change_signature))
            {
                if (!IsAvailable(subjectBuffer, out var workspace))
                {
                    return false;
                }

                var caretPoint = textView.GetCaretPoint(subjectBuffer);
                if (!caretPoint.HasValue)
                {
                    return false;
                }

                var document = subjectBuffer.CurrentSnapshot.GetFullyLoadedOpenDocumentInCurrentContextWithChanges(
                    context.OperationContext, _threadingContext);
                if (document == null)
                {
                    return false;
                }

                var reorderParametersService = document.GetLanguageService<AbstractChangeSignatureService>();
                var result = reorderParametersService.ChangeSignature(
                    document,
                    caretPoint.Value.Position,
                    (errorMessage, severity) =>
                    {
                        // We are about to show a modal UI dialog so we should take over the command execution
                        // wait context. That means the command system won't attempt to show its own wait dialog 
                        // and also will take it into consideration when measuring command handling duration.
                        context.OperationContext.TakeOwnership();
                        workspace.Services.GetService<INotificationService>().SendNotification(errorMessage, severity: severity);
                    },
                context.OperationContext.UserCancellationToken);

                if (result == null || !result.Succeeded)
                {
                    return true;
                }

                var finalSolution = result.UpdatedSolution;

                var previewService = workspace.Services.GetService<IPreviewDialogService>();
                if (previewService != null && result.PreviewChanges)
                {
                    // We are about to show a modal UI dialog so we should take over the command execution
                    // wait context. That means the command system won't attempt to show its own wait dialog 
                    // and also will take it into consideration when measuring command handling duration.
                    context.OperationContext.TakeOwnership();
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
}
