// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Undo;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal abstract class AbstractChangeSignatureCommandHandler : ICommandHandler<ReorderParametersCommandArgs>,
        ICommandHandler<RemoveParametersCommandArgs>
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IGlobalOptionService _globalOptions;

        protected AbstractChangeSignatureCommandHandler(IThreadingContext threadingContext, IGlobalOptionService globalOptions)
        {
            _threadingContext = threadingContext;
            _globalOptions = globalOptions;
        }

        public string DisplayName => EditorFeaturesResources.Change_Signature;

        public CommandState GetCommandState(ReorderParametersCommandArgs args)
            => GetCommandState(args.SubjectBuffer);

        public CommandState GetCommandState(RemoveParametersCommandArgs args)
            => GetCommandState(args.SubjectBuffer);

        private static CommandState GetCommandState(ITextBuffer subjectBuffer)
            => IsAvailable(subjectBuffer, out _) ? CommandState.Available : CommandState.Unspecified;

        public bool ExecuteCommand(RemoveParametersCommandArgs args, CommandExecutionContext context)
            => ExecuteCommand(args.TextView, args.SubjectBuffer, context);

        public bool ExecuteCommand(ReorderParametersCommandArgs args, CommandExecutionContext context)
            => ExecuteCommand(args.TextView, args.SubjectBuffer, context);

        private static bool IsAvailable(ITextBuffer subjectBuffer, [NotNullWhen(true)] out Workspace? workspace)
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

                var changeSignatureService = document.GetRequiredLanguageService<AbstractChangeSignatureService>();

                var cancellationToken = context.OperationContext.UserCancellationToken;

                // Async operation to determine the change signature
                var changeSignatureContext = changeSignatureService.GetChangeSignatureContextAsync(
                    document,
                    caretPoint.Value.Position,
                    restrictToDeclarations: false,
                    _globalOptions.CreateProvider(),
                    cancellationToken).WaitAndGetResult(context.OperationContext.UserCancellationToken);

                // UI thread bound operation to show the change signature dialog.
                var changeSignatureOptions = AbstractChangeSignatureService.GetChangeSignatureOptions(changeSignatureContext);

                // Async operation to compute the new solution created from the specified options.
                var result = changeSignatureService.ChangeSignatureWithContextAsync(changeSignatureContext, changeSignatureOptions, cancellationToken).WaitAndGetResult(cancellationToken);

                // UI thread bound operation to show preview changes dialog / show error message, then apply the solution changes (if applicable).
                HandleResult(result, document.Project.Solution, workspace, context);

                return true;
            }
        }

        private static void HandleResult(ChangeSignatureResult result, Solution oldSolution, Workspace workspace, CommandExecutionContext context)
        {
            var notificationService = workspace.Services.GetRequiredService<INotificationService>();
            if (!result.Succeeded)
            {
                if (result.ChangeSignatureFailureKind != null)
                {
                    ShowError(result.ChangeSignatureFailureKind.Value, context.OperationContext, notificationService);
                }

                return;
            }

            if (result.ConfirmationMessage != null && !notificationService.ConfirmMessageBox(result.ConfirmationMessage, severity: NotificationSeverity.Warning))
            {
                return;
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
                    oldSolution);
            }

            if (finalSolution == null)
            {
                // User clicked cancel.
                return;
            }

            using var workspaceUndoTransaction = workspace.OpenGlobalUndoTransaction(FeaturesResources.Change_signature);
            if (workspace.TryApplyChanges(finalSolution))
            {
                workspaceUndoTransaction.Commit();
            }

            // TODO: handle failure
        }

        private static void ShowError(ChangeSignatureFailureKind reason, IUIThreadOperationContext operationContext, INotificationService notificationService)
        {
            switch (reason)
            {
                case ChangeSignatureFailureKind.DefinedInMetadata:
                    ShowMessage(FeaturesResources.The_member_is_defined_in_metadata, NotificationSeverity.Error, operationContext, notificationService);
                    break;
                case ChangeSignatureFailureKind.IncorrectKind:
                    ShowMessage(FeaturesResources.You_can_only_change_the_signature_of_a_constructor_indexer_method_or_delegate, NotificationSeverity.Error, operationContext, notificationService);
                    break;
            }

            static void ShowMessage(string errorMessage, NotificationSeverity severity, IUIThreadOperationContext operationContext, INotificationService notificationService)
            {
                operationContext.TakeOwnership();
                notificationService.SendNotification(errorMessage, severity: severity);
            }
        }
    }
}
