// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Notification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler : ICommandHandler<RenameCommandArgs>
    {
        public CommandState GetCommandState(RenameCommandArgs args)
        {
            var caretPoint = args.TextView.GetCaretPoint(args.SubjectBuffer);
            if (!caretPoint.HasValue)
            {
                return CommandState.Unspecified;
            }

            if (!args.SubjectBuffer.TryGetWorkspace(out var workspace) ||
                !workspace.CanApplyChange(ApplyChangesKind.ChangeDocument) ||
                !args.SubjectBuffer.SupportsRename())
            {
                return CommandState.Unspecified;
            }

            return CommandState.Available;
        }

        public bool ExecuteCommand(RenameCommandArgs args, CommandExecutionContext context)
        {
            using (context.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Finding_token_to_rename))
            {
                ExecuteRenameWorker(args, context);
            }

            return true;
        }

        private void ExecuteRenameWorker(RenameCommandArgs args, CommandExecutionContext context)
        {
            if (!args.SubjectBuffer.TryGetWorkspace(out var workspace))
            {
                return;
            }

            var caretPoint = args.TextView.GetCaretPoint(args.SubjectBuffer);
            if (!caretPoint.HasValue)
            {
                ShowErrorDialog(workspace, EditorFeaturesResources.You_must_rename_an_identifier);
                return;
            }

            // If there is already an active session, commit it first
            if (_renameService.ActiveSession != null)
            {
                // Is the caret within any of the rename fields in this buffer?
                // If so, focus the dashboard
                if (_renameService.ActiveSession.TryGetContainingEditableSpan(caretPoint.Value, out var editableSpan))
                {
                    var dashboard = GetDashboard(args.TextView);
                    dashboard.Focus();
                    return;
                }
                else
                {
                    // Otherwise, commit the existing session and start a new one.
                    _renameService.ActiveSession.Commit();
                }
            }

            var cancellationToken = context.OperationContext.UserCancellationToken;
            var document = args.SubjectBuffer.CurrentSnapshot.GetFullyLoadedOpenDocumentInCurrentContextWithChanges(
                context.OperationContext, _threadingContext);
            if (document == null)
            {
                ShowErrorDialog(workspace, EditorFeaturesResources.You_must_rename_an_identifier);
                return;
            }

            var selectedSpans = args.TextView.Selection.GetSnapshotSpansOnBuffer(args.SubjectBuffer);

            // Now make sure the entire selection is contained within that token.
            // There can be zero selectedSpans in projection scenarios.
            if (selectedSpans.Count != 1)
            {
                ShowErrorDialog(workspace, EditorFeaturesResources.You_must_rename_an_identifier);
                return;
            }

            var sessionInfo = _renameService.StartInlineSession(document, selectedSpans.Single().Span.ToTextSpan(), cancellationToken);
            if (!sessionInfo.CanRename)
            {
                ShowErrorDialog(workspace, sessionInfo.LocalizedErrorMessage);
            }
        }

        private static void ShowErrorDialog(Workspace workspace, string message)
        {
            var notificationService = workspace.Services.GetService<INotificationService>();
            notificationService.SendNotification(message, title: EditorFeaturesResources.Rename, severity: NotificationSeverity.Error);
        }
    }
}
