// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal abstract partial class AbstractRenameCommandHandler : ICommandHandler<RenameCommandArgs>
    {
        public CommandState GetCommandState(RenameCommandArgs args)
        {
            var caretPoint = args.TextView.GetCaretPoint(args.SubjectBuffer);
            if (!caretPoint.HasValue)
            {
                return CommandState.Unspecified;
            }

            if (!CanRename(args))
            {
                return CommandState.Unspecified;
            }

            return CommandState.Available;
        }

        public bool ExecuteCommand(RenameCommandArgs args, CommandExecutionContext context)
        {
            if (!CanRename(args))
            {
                return false;
            }

            var token = _listener.BeginAsyncOperation(nameof(ExecuteCommand));
            _ = ExecuteCommandAsync(args, context).CompletesAsyncOperation(token);
            return true;
        }

        private async Task ExecuteCommandAsync(RenameCommandArgs args, CommandExecutionContext commandExecutionContext)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            if (!args.SubjectBuffer.TryGetWorkspace(out var workspace))
            {
                return;
            }

            var caretPoint = args.TextView.GetCaretPoint(args.SubjectBuffer);
            if (!caretPoint.HasValue)
            {
                await ShowErrorDialogAsync(workspace, EditorFeaturesResources.You_must_rename_an_identifier, _threadingContext).ConfigureAwait(false);
                return;
            }

            using var context = GetContext();

            // If there is already an active session, commit it first
            if (_renameService.ActiveSession != null)
            {
                // Is the caret within any of the rename fields in this buffer?
                // If so, focus the dashboard
                if (_renameService.ActiveSession.TryGetContainingEditableSpan(caretPoint.Value, out _))
                {
                    SetFocusToAdornment(args.TextView);
                    return;
                }
                else
                {
                    // Otherwise, commit the existing session and start a new one.
                    _renameService.ActiveSession.Commit();
                }
            }

            var cancellationToken = context.UserCancellationToken;

            var document = await args
                .SubjectBuffer
                .CurrentSnapshot
                .GetFullyLoadedOpenDocumentInCurrentContextWithChangesAsync(context)
                .ConfigureAwait(false);

            if (document == null)
            {
                await ShowErrorDialogAsync(workspace, EditorFeaturesResources.You_must_rename_an_identifier, _threadingContext).ConfigureAwait(false);
                return;
            }

            var selectedSpans = args.TextView.Selection.GetSnapshotSpansOnBuffer(args.SubjectBuffer);

            // Now make sure the entire selection is contained within that token.
            // There can be zero selectedSpans in projection scenarios.
            if (selectedSpans.Count != 1)
            {
                await ShowErrorDialogAsync(workspace, EditorFeaturesResources.You_must_rename_an_identifier, _threadingContext).ConfigureAwait(false);
                return;
            }

            var sessionInfo = await _renameService.StartInlineSessionAsync(document, selectedSpans.Single().Span.ToTextSpan(), cancellationToken).ConfigureAwait(false);
            if (!sessionInfo.CanRename)
            {
                await ShowErrorDialogAsync(workspace, sessionInfo.LocalizedErrorMessage, _threadingContext).ConfigureAwait(false);
            }

            //
            // Local Functions
            //
            IUIThreadOperationContext GetContext()
            {
                var backgroundWorkIndicatorFactory = workspace.Services.GetService<IBackgroundWorkIndicatorFactory>();

                // Rename command handler builds for hosts that don't necessarily
                // have an implementation for IBackgroundWorkIndicatorFactory
                if (backgroundWorkIndicatorFactory is null)
                {
                    // If using the traditional OperationContext then 
                    // add a new scope, but it will be disposed with the OperationContext
                    // instead of disposed directly
                    _ = commandExecutionContext.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Finding_token_to_rename);
                    return commandExecutionContext.OperationContext;
                }

                return backgroundWorkIndicatorFactory.Create(
                    args.TextView,
                    args.TextView.GetTextElementSpan(caretPoint.Value),
                    EditorFeaturesResources.Finding_token_to_rename);
            }
        }

        private static bool CanRename(RenameCommandArgs args)
        {
            return args.SubjectBuffer.TryGetWorkspace(out var workspace) &&
                workspace.CanApplyChange(ApplyChangesKind.ChangeDocument) &&
                args.SubjectBuffer.SupportsRename() && !args.SubjectBuffer.IsInLspEditorContext();
        }

        private static async Task ShowErrorDialogAsync(Workspace workspace, string message, IThreadingContext threadingContext)
        {
            await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
            var notificationService = workspace.Services.GetService<INotificationService>();
            notificationService.SendNotification(message, title: EditorFeaturesResources.Rename, severity: NotificationSeverity.Error);
        }
    }
}
