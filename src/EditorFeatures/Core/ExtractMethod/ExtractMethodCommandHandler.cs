// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using EnvDTE;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    [Name(PredefinedCommandHandlerNames.ExtractMethod)]
    [Order(After = PredefinedCommandHandlerNames.DocumentationComments)]
    internal sealed class ExtractMethodCommandHandler : ICommandHandler<ExtractMethodCommandArgs>
    {
        private readonly IThreadingContext _threadingContext;
        private readonly ITextBufferUndoManagerProvider _undoManager;
        private readonly IInlineRenameService _renameService;
        private readonly IGlobalOptionService _globalOptions;
        private readonly IAsynchronousOperationListener _asyncListener;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ExtractMethodCommandHandler(
            IThreadingContext threadingContext,
            ITextBufferUndoManagerProvider undoManager,
            IInlineRenameService renameService,
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider asyncListenerProvider)
        {
            Contract.ThrowIfNull(threadingContext);
            Contract.ThrowIfNull(undoManager);
            Contract.ThrowIfNull(renameService);

            _threadingContext = threadingContext;
            _undoManager = undoManager;
            _renameService = renameService;
            _globalOptions = globalOptions;
            _asyncListener = asyncListenerProvider.GetListener(FeatureAttribute.ExtractMethod);
        }

        public string DisplayName => EditorFeaturesResources.Extract_Method;

        public CommandState GetCommandState(ExtractMethodCommandArgs args)
        {
            var spans = args.TextView.Selection.GetSnapshotSpansOnBuffer(args.SubjectBuffer);
            if (spans.Count(s => s.Length > 0) != 1)
            {
                return CommandState.Unspecified;
            }

            if (!args.SubjectBuffer.TryGetWorkspace(out var workspace) ||
                !workspace.CanApplyChange(ApplyChangesKind.ChangeDocument) ||
                !args.SubjectBuffer.SupportsRefactorings())
            {
                return CommandState.Unspecified;
            }

            return CommandState.Available;
        }

        public bool ExecuteCommand(ExtractMethodCommandArgs args, CommandExecutionContext context)
        {
            // Finish any rename that had been started. We'll do this here before we enter the
            // wait indicator for Extract Method
            if (_renameService.ActiveSession != null)
            {
                _threadingContext.JoinableTaskFactory.Run(() => _renameService.ActiveSession.CommitAsync(previewChanges: false, CancellationToken.None));
            }

            if (!args.SubjectBuffer.SupportsRefactorings())
                return false;

            var view = args.TextView;
            var textBuffer = args.SubjectBuffer;
            var spans = view.Selection.GetSnapshotSpansOnBuffer(textBuffer).Where(s => s.Length > 0).ToList();
            if (spans.Count != 1)
                return false;

            var span = spans[0];

            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document is null)
                return false;

            _ = ExecuteAsync(view, textBuffer, document, span);
            return true;
        }

        private async Task ExecuteAsync(
            ITextView view,
            ITextBuffer textBuffer,
            Document document,
            SnapshotSpan span)
        {
            _threadingContext.ThrowIfNotOnUIThread();
            var indicatorFactory = document.Project.Solution.Services.GetRequiredService<IBackgroundWorkIndicatorFactory>();
            using var indicatorContext = indicatorFactory.Create(
                view, span, EditorFeaturesResources.Applying_Extract_Method_refactoring, cancelOnEdit: true, cancelOnFocusLost: true);

            using var asyncToken = _asyncListener.BeginAsyncOperation(nameof(ExecuteCommand));
            await ExecuteWorkerAsync(view, textBuffer, span.Span.ToTextSpan(), indicatorContext).ConfigureAwait(false);
        }

        private async Task ExecuteWorkerAsync(
            ITextView view,
            ITextBuffer textBuffer,
            TextSpan span,
            IBackgroundWorkIndicatorContext waitContext)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            var cancellationToken = waitContext.UserCancellationToken;

            var document = await textBuffer.CurrentSnapshot.GetFullyLoadedOpenDocumentInCurrentContextWithChangesAsync(waitContext).ConfigureAwait(false);
            if (document is null)
                return;

            var options = await document.GetExtractMethodGenerationOptionsAsync(_globalOptions, cancellationToken).ConfigureAwait(false);
            var result = await ExtractMethodService.ExtractMethodAsync(
                document, span, localFunction: false, options, cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(result);

            if (!result.Succeeded && !result.SucceededWithSuggestion)
            {
                // if it failed due to out/ref parameter in async method, try it with different option
                var newResult = await TryWithoutMakingValueTypesRefAsync(
                    document, span, result, options, cancellationToken).ConfigureAwait(false);
                if (newResult != null)
                {
                    var notificationService = document.Project.Solution.Services.GetService<INotificationService>();
                    if (notificationService != null)
                    {
                        // We are about to show a modal UI dialog so we should take over the command execution
                        // wait context. That means the command system won't attempt to show its own wait dialog 
                        // and also will take it into consideration when measuring command handling duration.
                        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                        if (!notificationService.ConfirmMessageBox(
                                EditorFeaturesResources.Extract_method_encountered_the_following_issues + Environment.NewLine + Environment.NewLine +
                                string.Join(Environment.NewLine, result.Reasons) + Environment.NewLine + Environment.NewLine +
                                EditorFeaturesResources.We_can_fix_the_error_by_not_making_struct_out_ref_parameter_s_Do_you_want_to_proceed,
                                title: EditorFeaturesResources.Extract_Method,
                                severity: NotificationSeverity.Error))
                        {
                            // We handled the command, displayed a notification and did not produce code.
                            return;
                        }

                        await TaskScheduler.Default;
                    }

                    // reset result
                    result = newResult;
                }
                else if (await TryNotifyFailureToUserAsync(document, result, cancellationToken).ConfigureAwait(false))
                {
                    // We handled the command, displayed a notification and did not produce code.
                    return;
                }
            }

            var cleanupOptions = await document.GetCodeCleanupOptionsAsync(_globalOptions, cancellationToken).ConfigureAwait(false);
            var (formattedDocument, methodNameAtInvocation) = await result.GetFormattedDocumentAsync(cleanupOptions, cancellationToken).ConfigureAwait(false);
            var changes = await formattedDocument.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);

            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            ApplyChange_OnUIThread(textBuffer, changes, waitContext);

            // start inline rename to allow the user to change the name if they want.
            var textSnapshot = textBuffer.CurrentSnapshot;
            document = textSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document != null)
                _renameService.StartInlineSession(document, methodNameAtInvocation.Span, cancellationToken);

            // select invocation span
            view.TryMoveCaretToAndEnsureVisible(new SnapshotPoint(textSnapshot, methodNameAtInvocation.Span.End));
            view.SetSelection(methodNameAtInvocation.Span.ToSnapshotSpan(textSnapshot));
        }

        private void ApplyChange_OnUIThread(
            ITextBuffer textBuffer, IEnumerable<TextChange> changes, IBackgroundWorkIndicatorContext waitContext)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            using var undoTransaction = _undoManager.GetTextBufferUndoManager(textBuffer).TextBufferUndoHistory.CreateTransaction("Extract Method");

            // We're about to make an edit ourselves.  so disable the cancellation that happens on editing.
            waitContext.CancelOnEdit = false;
            textBuffer.ApplyChanges(changes);

            // apply changes
            undoTransaction.Complete();
        }

        /// <returns>
        /// True: if a failure notification was displayed or the user did not want to proceed in a best effort scenario. 
        ///       Extract Method does not proceed further and is done.
        /// False: the user proceeded to a best effort scenario.
        /// </returns>
        private async Task<bool> TryNotifyFailureToUserAsync(
            Document document, ExtractMethodResult result, CancellationToken cancellationToken)
        {
            // We are about to show a modal UI dialog so we should take over the command execution
            // wait context. That means the command system won't attempt to show its own wait dialog 
            // and also will take it into consideration when measuring command handling duration.
            var project = document.Project;
            var solution = project.Solution;
            var notificationService = solution.Services.GetService<INotificationService>();

            // see whether we will allow best effort extraction and if it is possible.
            if (!_globalOptions.GetOption(ExtractMethodPresentationOptionsStorage.AllowBestEffort, document.Project.Language) ||
                !result.Status.HasBestEffort() ||
                result.DocumentWithoutFinalFormatting == null)
            {
                if (notificationService != null)
                {
                    await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
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
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
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

        private static async Task<ExtractMethodResult?> TryWithoutMakingValueTypesRefAsync(
            Document document, TextSpan span, ExtractMethodResult result, ExtractMethodGenerationOptions options, CancellationToken cancellationToken)
        {
            if (options.ExtractOptions.DoNotPutOutOrRefOnStruct || !result.Reasons.IsSingle())
                return null;

            var reason = result.Reasons.FirstOrDefault();
            var length = FeaturesResources.Asynchronous_method_cannot_have_ref_out_parameters_colon_bracket_0_bracket.IndexOf(':');
            if (reason != null && length > 0 && reason.IndexOf(FeaturesResources.Asynchronous_method_cannot_have_ref_out_parameters_colon_bracket_0_bracket[..length], 0, length, StringComparison.Ordinal) >= 0)
            {
                var newResult = await ExtractMethodService.ExtractMethodAsync(
                    document,
                    span,
                    localFunction: false,
                    options with { ExtractOptions = options.ExtractOptions with { DoNotPutOutOrRefOnStruct = true } },
                    cancellationToken).ConfigureAwait(false);

                // retry succeeded, return new result
                if (newResult.Succeeded || newResult.SucceededWithSuggestion)
                    return newResult;
            }

            return null;
        }
    }
}
