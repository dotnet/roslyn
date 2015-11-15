// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.ExtractMethod
{
    internal abstract class AbstractExtractMethodCommandHandler : ICommandHandler<ExtractMethodCommandArgs>
    {
        private readonly ITextBufferUndoManagerProvider _undoManager;
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;
        private readonly IInlineRenameService _renameService;
        private readonly IWaitIndicator _waitIndicator;

        public AbstractExtractMethodCommandHandler(
            ITextBufferUndoManagerProvider undoManager,
            IEditorOperationsFactoryService editorOperationsFactoryService,
            IInlineRenameService renameService,
            IWaitIndicator waitIndicator)
        {
            Contract.ThrowIfNull(undoManager);
            Contract.ThrowIfNull(editorOperationsFactoryService);
            Contract.ThrowIfNull(renameService);
            Contract.ThrowIfNull(waitIndicator);

            _undoManager = undoManager;
            _editorOperationsFactoryService = editorOperationsFactoryService;
            _renameService = renameService;
            _waitIndicator = waitIndicator;
        }

        public CommandState GetCommandState(ExtractMethodCommandArgs args, Func<CommandState> nextHandler)
        {
            var spans = args.TextView.Selection.GetSnapshotSpansOnBuffer(args.SubjectBuffer);
            if (spans.Count(s => s.Length > 0) != 1)
            {
                return nextHandler();
            }

            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return nextHandler();
            }

            if (!document.Project.Solution.Workspace.CanApplyChange(ApplyChangesKind.ChangeDocument))
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

        public void ExecuteCommand(ExtractMethodCommandArgs args, Action nextHandler)
        {
            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
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

            // Finish any rename that had been started. We'll do this here before we enter the
            // wait indicator for Extract Method
            if (_renameService.ActiveSession != null)
            {
                _renameService.ActiveSession.Commit();
            }

            var executed = false;
            _waitIndicator.Wait(
                title: EditorFeaturesResources.ExtractMethod,
                message: EditorFeaturesResources.ApplyingExtractMethodRefactoring,
                allowCancel: true,
                action: waitContext =>
                {
                    executed = this.Execute(args.SubjectBuffer, args.TextView, waitContext.CancellationToken);
                });

            if (!executed)
            {
                nextHandler();
            }
        }

        private bool Execute(
            ITextBuffer textBuffer,
            ITextView view,
            CancellationToken cancellationToken)
        {
            var spans = view.Selection.GetSnapshotSpansOnBuffer(textBuffer);
            if (spans.Count(s => s.Length > 0) != 1)
            {
                return false;
            }

            var document = textBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return false;
            }

            var options = document.Project.Solution.Workspace.Options;
            var result = ExtractMethodService.ExtractMethodAsync(
                document, spans.Single().Span.ToTextSpan(), options, cancellationToken).WaitAndGetResult(cancellationToken);
            Contract.ThrowIfNull(result);

            if (!result.Succeeded && !result.SucceededWithSuggestion)
            {
                // if it failed due to out/ref parameter in async method, try it with different option
                var newResult = TryWithoutMakingValueTypesRef(document, spans, options, result, cancellationToken);
                if (newResult != null)
                {
                    var notificationService = document.Project.Solution.Workspace.Services.GetService<INotificationService>();
                    if (notificationService != null)
                    {
                        if (!notificationService.ConfirmMessageBox(
                                EditorFeaturesResources.ExtractMethodFailedReasons + Environment.NewLine + Environment.NewLine +
                                string.Join(Environment.NewLine, result.Reasons) + Environment.NewLine + Environment.NewLine +
                                EditorFeaturesResources.ExtractMethodAsyncErrorFix,
                                title: EditorFeaturesResources.ExtractMethod,
                                severity: NotificationSeverity.Error))
                        {
                            // We handled the command, displayed a notification and did not produce code.
                            return true;
                        }
                    }

                    // reset result
                    result = newResult;
                }
                else if (TryNotifyFailureToUser(document, result))
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
        private bool TryNotifyFailureToUser(Document document, ExtractMethodResult result)
        {
            var notificationService = document.Project.Solution.Workspace.Services.GetService<INotificationService>();

            // see whether we will allow best effort extraction and if it is possible.
            if (!document.Project.Solution.Workspace.Options.GetOption(ExtractMethodOptions.AllowBestEffort, document.Project.Language) ||
                !result.Status.HasBestEffort() || result.Document == null)
            {
                if (notificationService != null)
                {
                    notificationService.SendNotification(
                        EditorFeaturesResources.ExtractMethodFailedReasons + Environment.NewLine + Environment.NewLine +
                        string.Join(Environment.NewLine, result.Reasons),
                        title: EditorFeaturesResources.ExtractMethod,
                        severity:NotificationSeverity.Error);
                }

                return true;
            }

            // okay, best effort is turned on, let user know it is an best effort
            if (notificationService != null)
            {
                if (!notificationService.ConfirmMessageBox(
                        EditorFeaturesResources.ExtractMethodFailedReasons + Environment.NewLine + Environment.NewLine +
                        string.Join(Environment.NewLine, result.Reasons) + Environment.NewLine + Environment.NewLine +
                        EditorFeaturesResources.ExtractMethodStillGenerateCode,
                        title: EditorFeaturesResources.ExtractMethod,
                        severity: NotificationSeverity.Error))
                {
                    return true;
                }
            }

            return false;
        }

        private static ExtractMethodResult TryWithoutMakingValueTypesRef(
            Document document, NormalizedSnapshotSpanCollection spans, OptionSet options, ExtractMethodResult result, CancellationToken cancellationToken)
        {
            if (options.GetOption(ExtractMethodOptions.DontPutOutOrRefOnStruct, document.Project.Language) || !result.Reasons.IsSingle())
            {
                return null;
            }

            var reason = result.Reasons.FirstOrDefault();
            var length = FeaturesResources.AsyncMethodWithRefOutParameters.IndexOf(':');
            if (reason != null && length > 0 && reason.IndexOf(FeaturesResources.AsyncMethodWithRefOutParameters.Substring(0, length), 0, length, StringComparison.Ordinal) >= 0)
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
            using (var undoTransaction = _undoManager.GetTextBufferUndoManager(subjectBuffer).TextBufferUndoHistory.CreateTransaction("Extract Method"))
            {
                // apply extract method code to buffer
                var document = extractMethodResult.Document;
                document.Project.Solution.Workspace.ApplyDocumentChanges(document, cancellationToken);

                // apply changes
                undoTransaction.Complete();
            }
        }
    }
}
