// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.ExtractMethod
{
    internal abstract class AbstractExtractMethodCommandHandler : VSCommanding.ICommandHandler<ExtractMethodCommandArgs>
    {
        private readonly ITextBufferUndoManagerProvider _undoManager;
        private readonly IInlineRenameService _renameService;

        public AbstractExtractMethodCommandHandler(
            ITextBufferUndoManagerProvider undoManager,
            IInlineRenameService renameService)
        {
            Contract.ThrowIfNull(undoManager);
            Contract.ThrowIfNull(renameService);

            _undoManager = undoManager;
            _renameService = renameService;
        }
        public string DisplayName => EditorFeaturesResources.Extract_Method;

        public VSCommanding.CommandState GetCommandState(ExtractMethodCommandArgs args)
        {
            var spans = args.TextView.Selection.GetSnapshotSpansOnBuffer(args.SubjectBuffer);
            if (spans.Count(s => s.Length > 0) != 1)
            {
                return VSCommanding.CommandState.Unspecified;
            }

            if (!args.SubjectBuffer.TryGetWorkspace(out var workspace) ||
                !workspace.CanApplyChange(ApplyChangesKind.ChangeDocument) ||
                !args.SubjectBuffer.SupportsRefactorings())
            {
                return VSCommanding.CommandState.Unspecified;
            }

            return VSCommanding.CommandState.Available;
        }

        public bool ExecuteCommand(ExtractMethodCommandArgs args, CommandExecutionContext context)
        {
            // Finish any rename that had been started. We'll do this here before we enter the
            // wait indicator for Extract Method
            if (_renameService.ActiveSession != null)
            {
                _renameService.ActiveSession.Commit();
            }

            if (!args.SubjectBuffer.SupportsRefactorings())
            {
                return false;
            }

            using (context.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Applying_Extract_Method_refactoring))
            {
                return Execute(args.SubjectBuffer, args.TextView, context.OperationContext);
            }
        }

        private bool Execute(
            ITextBuffer textBuffer,
            ITextView view,
            IUIThreadOperationContext waitContext)
        {
            var cancellationToken = waitContext.UserCancellationToken;

            var spans = view.Selection.GetSnapshotSpansOnBuffer(textBuffer);
            if (spans.Count(s => s.Length > 0) != 1)
            {
                return false;
            }

            var document = textBuffer.CurrentSnapshot.GetFullyLoadedOpenDocumentInCurrentContextWithChangesAsync(waitContext).WaitAndGetResult(cancellationToken);
            if (document == null)
            {
                return false;
            }

            var result = ExtractMethodService.ExtractMethodAsync(
                document, spans.Single().Span.ToTextSpan(), cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken);
            Contract.ThrowIfNull(result);

            if (!result.Succeeded && !result.SucceededWithSuggestion)
            {
                // if it failed due to out/ref parameter in async method, try it with different option
                var newResult = TryWithoutMakingValueTypesRef(document, spans, result, cancellationToken);
                if (newResult != null)
                {
                    var notificationService = document.Project.Solution.Workspace.Services.GetService<INotificationService>();
                    if (notificationService != null)
                    {
                        // We are about to show a modal UI dialog so we should take over the command execution
                        // wait context. That means the command system won't attempt to show its own wait dialog 
                        // and also will take it into consideration when measuring command handling duration.
                        waitContext.TakeOwnership();
                        if (!notificationService.ConfirmMessageBox(
                                EditorFeaturesResources.Extract_method_encountered_the_following_issues + Environment.NewLine + Environment.NewLine +
                                string.Join(Environment.NewLine, result.Reasons) + Environment.NewLine + Environment.NewLine +
                                EditorFeaturesResources.We_can_fix_the_error_by_not_making_struct_out_ref_parameter_s_Do_you_want_to_proceed,
                                title: EditorFeaturesResources.Extract_Method,
                                severity: NotificationSeverity.Error))
                        {
                            // We handled the command, displayed a notification and did not produce code.
                            return true;
                        }
                    }

                    // reset result
                    result = newResult;
                }
                else if (TryNotifyFailureToUser(document, result, waitContext))
                {
                    // We handled the command, displayed a notification and did not produce code.
                    return true;
                }
            }

            // apply the change to buffer
            // get method name token
            ApplyChangesToBuffer(result, textBuffer, cancellationToken);

            // start inline rename
            var methodNameAtInvocation = result.InvocationNameToken;
            var snapshotAfterFormatting = textBuffer.CurrentSnapshot;
            var documentAfterFormatting = snapshotAfterFormatting.GetOpenDocumentInCurrentContextWithChanges();
            _renameService.StartInlineSession(documentAfterFormatting, methodNameAtInvocation.Span, cancellationToken);

            // select invocation span
            view.TryMoveCaretToAndEnsureVisible(new SnapshotPoint(snapshotAfterFormatting, methodNameAtInvocation.Span.End));
            view.SetSelection(
                methodNameAtInvocation.Span.ToSnapshotSpan(snapshotAfterFormatting));

            return true;
        }

        /// <returns>
        /// True: if a failure notification was displayed or the user did not want to proceed in a best effort scenario. 
        ///       Extract Method does not proceed further and is done.
        /// False: the user proceeded to a best effort scenario.
        /// </returns>
        private bool TryNotifyFailureToUser(Document document, ExtractMethodResult result, IUIThreadOperationContext waitContext)
        {
            // We are about to show a modal UI dialog so we should take over the command execution
            // wait context. That means the command system won't attempt to show its own wait dialog 
            // and also will take it into consideration when measuring command handling duration.
            waitContext.TakeOwnership();
            var project = document.Project;
            var solution = project.Solution;
            var notificationService = solution.Workspace.Services.GetService<INotificationService>();

            // see whether we will allow best effort extraction and if it is possible.
            if (!solution.Options.GetOption(ExtractMethodOptions.AllowBestEffort, project.Language) ||
                !result.Status.HasBestEffort() ||
                result.Document == null)
            {
                if (notificationService != null)
                {
                    notificationService.SendNotification(
                        EditorFeaturesResources.Extract_method_encountered_the_following_issues + Environment.NewLine +
                        string.Join("", result.Reasons.Select(r => Environment.NewLine + "  " + r)),
                        title: EditorFeaturesResources.Extract_Method,
                        severity: NotificationSeverity.Error);
                }

                return true;
            }

            // okay, best effort is turned on, let user know it is an best effort
            if (notificationService != null)
            {
                if (!notificationService.ConfirmMessageBox(
                        EditorFeaturesResources.Extract_method_encountered_the_following_issues + Environment.NewLine +
                        string.Join("", result.Reasons.Select(r => Environment.NewLine + "  " + r)) + Environment.NewLine + Environment.NewLine +
                        EditorFeaturesResources.Do_you_still_want_to_proceed_This_may_produce_broken_code,
                        title: EditorFeaturesResources.Extract_Method,
                        severity: NotificationSeverity.Warning))
                {
                    return true;
                }
            }

            return false;
        }

        private static ExtractMethodResult TryWithoutMakingValueTypesRef(
            Document document, NormalizedSnapshotSpanCollection spans, ExtractMethodResult result, CancellationToken cancellationToken)
        {
            var options = document.Project.Solution.Options;

            if (options.GetOption(ExtractMethodOptions.DontPutOutOrRefOnStruct, document.Project.Language) || !result.Reasons.IsSingle())
            {
                return null;
            }

            var reason = result.Reasons.FirstOrDefault();
            var length = FeaturesResources.Asynchronous_method_cannot_have_ref_out_parameters_colon_bracket_0_bracket.IndexOf(':');
            if (reason != null && length > 0 && reason.IndexOf(FeaturesResources.Asynchronous_method_cannot_have_ref_out_parameters_colon_bracket_0_bracket.Substring(0, length), 0, length, StringComparison.Ordinal) >= 0)
            {
                options = options.WithChangedOption(ExtractMethodOptions.DontPutOutOrRefOnStruct, document.Project.Language, true);
                var newResult = ExtractMethodService.ExtractMethodAsync(
                    document, spans.Single().Span.ToTextSpan(), options, cancellationToken).WaitAndGetResult(cancellationToken);

                // retry succeeded, return new result
                if (newResult.Succeeded || newResult.SucceededWithSuggestion)
                {
                    return newResult;
                }
            }

            return null;
        }

        /// <summary>
        /// Applies an ExtractMethodResult to the editor.
        /// </summary>
        private void ApplyChangesToBuffer(ExtractMethodResult extractMethodResult, ITextBuffer subjectBuffer, CancellationToken cancellationToken)
        {
            using var undoTransaction = _undoManager.GetTextBufferUndoManager(subjectBuffer).TextBufferUndoHistory.CreateTransaction("Extract Method");

            // apply extract method code to buffer
            var document = extractMethodResult.Document;
            document.Project.Solution.Workspace.ApplyDocumentChanges(document, cancellationToken);

            // apply changes
            undoTransaction.Complete();
        }
    }
}
