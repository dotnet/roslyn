// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler : VSCommanding.ICommandHandler<RenameCommandArgs>
    {
        public VSCommanding.CommandState GetCommandState(RenameCommandArgs args)
        {
            var caretPoint = args.TextView.GetCaretPoint(args.SubjectBuffer);
            if (!caretPoint.HasValue)
            {
                return VSCommanding.CommandState.Unspecified;
            }

            var textContainer = args.SubjectBuffer.AsTextContainer();
            if (!Workspace.TryGetWorkspace(textContainer, out var workspace))
            {
                return VSCommanding.CommandState.Unspecified;
            }

            if (!workspace.CanApplyChange(ApplyChangesKind.ChangeDocument))
            {
                return VSCommanding.CommandState.Unspecified;
            }

            _ = textContainer.GetRelatedDocuments();
            var supportsFeatureService = workspace.Services.GetService<ITextBufferSupportsFeatureService>();
            if (!supportsFeatureService.SupportsRename(args.SubjectBuffer))
            {
                return VSCommanding.CommandState.Unspecified;
            }

            return VSCommanding.CommandState.Available;
        }

        public bool ExecuteCommand(RenameCommandArgs args, CommandExecutionContext context)
        {
            using (context.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Finding_token_to_rename))
            {
                ExecuteRenameWorker(args, context.OperationContext.UserCancellationToken);
            }

            return true;
        }

        private void ExecuteRenameWorker(RenameCommandArgs args, CancellationToken cancellationToken)
        {
            var snapshot = args.SubjectBuffer.CurrentSnapshot;
            if (!Workspace.TryGetWorkspace(snapshot.AsText().Container, out var workspace))
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
                if (_renameService.ActiveSession.TryGetContainingEditableSpan(caretPoint.Value, out var _))
                {
                    //var dashboard = GetDashboard(args.TextView);
                    //dashboard.Focus();
                    return;
                }
                else
                {
                    // Otherwise, commit the existing session and start a new one.
                    _renameService.ActiveSession.Commit();
                }
            }

            _ = caretPoint.Value;
            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
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
