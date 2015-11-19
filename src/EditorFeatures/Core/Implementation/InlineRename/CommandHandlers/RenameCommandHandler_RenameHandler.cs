// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler : ICommandHandler<RenameCommandArgs>
    {
        public CommandState GetCommandState(RenameCommandArgs args, Func<CommandState> nextHandler)
        {
            var caretPoint = args.TextView.GetCaretPoint(args.SubjectBuffer);
            if (!caretPoint.HasValue)
            {
                return nextHandler();
            }

            Workspace workspace;
            var textContainer = args.SubjectBuffer.AsTextContainer();
            if (!Workspace.TryGetWorkspace(textContainer, out workspace))
            {
                return nextHandler();
            }

            if (!workspace.CanApplyChange(ApplyChangesKind.ChangeDocument))
            {
                return nextHandler();
            }

            var documents = textContainer.GetRelatedDocuments();
            var supportsFeatureService = workspace.Services.GetService<IDocumentSupportsFeatureService>();
            if (!documents.All(d => supportsFeatureService.SupportsRename(d)))
            {
                return nextHandler();
            }

            return CommandState.Available;
        }

        public void ExecuteCommand(RenameCommandArgs args, Action nextHandler)
        {
            _waitIndicator.Wait(
                title: EditorFeaturesResources.Rename,
                message: EditorFeaturesResources.FindingTokenToRename,
                allowCancel: true,
                action: waitContext =>
            {
                ExecuteRenameWorker(args, waitContext.CancellationToken);
            });
        }

        private void ExecuteRenameWorker(RenameCommandArgs args, CancellationToken cancellationToken)
        {
            var snapshot = args.SubjectBuffer.CurrentSnapshot;
            Workspace workspace;
            if (!Workspace.TryGetWorkspace(snapshot.AsText().Container, out workspace))
            {
                return;
            }

            var caretPoint = args.TextView.GetCaretPoint(args.SubjectBuffer);
            if (!caretPoint.HasValue)
            {
                ShowErrorDialog(workspace, EditorFeaturesResources.YouMustRenameAnIdentifier);
                return;
            }

            // If there is already an active session, commit it first
            if (_renameService.ActiveSession != null)
            {
                // Is the caret within any of the rename fields in this buffer?
                // If so, focus the dashboard
                SnapshotSpan editableSpan;
                if (_renameService.ActiveSession.TryGetContainingEditableSpan(caretPoint.Value, out editableSpan))
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

            var position = caretPoint.Value;
            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                ShowErrorDialog(workspace, EditorFeaturesResources.YouMustRenameAnIdentifier);
                return;
            }

            var selectedSpans = args.TextView.Selection.GetSnapshotSpansOnBuffer(args.SubjectBuffer);

            // Now make sure the entire selection is contained within that token.
            // There can be zero selectedSpans in projection scenarios.
            if (selectedSpans.Count != 1)
            {
                ShowErrorDialog(workspace, EditorFeaturesResources.YouMustRenameAnIdentifier);
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
