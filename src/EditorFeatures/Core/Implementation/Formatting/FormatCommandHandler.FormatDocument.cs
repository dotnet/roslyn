// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Formatting
{
    internal partial class FormatCommandHandler : ForegroundThreadAffinitizedObject
    {
        public VSCommanding.CommandState GetCommandState(FormatDocumentCommandArgs args)
        {
            return GetCommandState(args.SubjectBuffer);
        }

        private void ShowGoldBarForCodeCleanupConfigurationIfNeeded(Document document)
        {
            AssertIsForeground();
            Logger.Log(FunctionId.CodeCleanupInfobar_BarDisplayed, KeyValueLogMessage.NoProperty);

            var workspace = document.Project.Solution.Workspace;

            // if info bar was shown before, no need to show it again
            if (workspace.Options.GetOption(CodeCleanupOptions.NeverShowCodeCleanupInfoBarAgain, document.Project.Language) ||
                workspace.Options.GetOption(CodeCleanupOptions.CodeCleanupInfoBarShown, document.Project.Language))
            {
                return;
            }

            // set as infobar shown so that we never show it again in same VS session. we might show it again
            // in other VS sessions if it is not explicitly configured yet
            workspace.Options = workspace.Options.WithChangedOption(
                CodeCleanupOptions.CodeCleanupInfoBarShown, document.Project.Language, value: true);

            var optionPageService = document.GetLanguageService<IOptionPageService>();
            var infoBarService = document.Project.Solution.Workspace.Services.GetRequiredService<IInfoBarService>();

            infoBarService.ShowInfoBarInGlobalView(
                EditorFeaturesResources.Code_cleanup_is_not_configured,
                new InfoBarUI(EditorFeaturesResources.Configure_it_now,
                              kind: InfoBarUI.UIKind.Button,
                              () =>
                              {
                                  Logger.Log(FunctionId.CodeCleanupInfobar_ConfigureNow, KeyValueLogMessage.NoProperty);
                                  optionPageService.ShowFormattingOptionPage();
                              }),
                new InfoBarUI(EditorFeaturesResources.Do_not_show_this_message_again,
                              kind: InfoBarUI.UIKind.Button,
                              () =>
                              {
                                  Logger.Log(FunctionId.CodeCleanupInfobar_NeverShowCodeCleanupInfoBarAgain, KeyValueLogMessage.NoProperty);
                                  workspace.Options = workspace.Options.WithChangedOption(
                                      CodeCleanupOptions.NeverShowCodeCleanupInfoBarAgain, document.Project.Language, value: true);
                              }));
        }

        public bool ExecuteCommand(FormatDocumentCommandArgs args, CommandExecutionContext context)
        {
            if (!CanExecuteCommand(args.SubjectBuffer))
            {
                return false;
            }

            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return false;
            }

            context.OperationContext.TakeOwnership();

            _waitIndicator.Wait(
                EditorFeaturesResources.Formatting_document,
                EditorFeaturesResources.Formatting_document,
                allowCancel: true,
                showProgress: true,
                c =>
                {
                    var docOptions = document.GetOptionsAsync(c.CancellationToken).WaitAndGetResult(c.CancellationToken);

                    using (Logger.LogBlock(FunctionId.FormatDocument, CodeCleanupLogMessage.Create(docOptions), c.CancellationToken))
                    using (var transaction = new CaretPreservingEditTransaction(
                        EditorFeaturesResources.Formatting, args.TextView, _undoHistoryRegistry, _editorOperationsFactoryService))
                    {
                        var codeCleanupService = document.GetLanguageService<ICodeCleanupService>();
                        if (codeCleanupService == null)
                        {
                            Format(args.TextView, document, selectionOpt: null, c.CancellationToken);
                        }
                        else
                        {
                            CodeCleanupOrFormat(args, document, c.ProgressTracker, c.CancellationToken);
                        }

                        transaction.Complete();
                    }
                });

            return true;
        }

        private void CodeCleanupOrFormat(
            FormatDocumentCommandArgs args, Document document,
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            var codeCleanupService = document.GetLanguageService<ICodeCleanupService>();

            var docOptions = document.GetOptionsAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            if (docOptions.GetOption(CodeCleanupOptions.PerformAdditionalCodeCleanupDuringFormatting))
            {
                // Start with a single progress item, which is the one to actually apply
                // the changes.
                progressTracker.AddItems(1);

                // Code cleanup
                var oldDoc = document;

                var codeCleanupChanges = GetCodeCleanupAndFormatChangesAsync(
                    document, codeCleanupService, progressTracker, cancellationToken).WaitAndGetResult(cancellationToken);

                if (codeCleanupChanges != null && codeCleanupChanges.Length > 0)
                {
                    progressTracker.Description = EditorFeaturesResources.Applying_changes;
                    ApplyChanges(oldDoc, codeCleanupChanges.ToList(), selectionOpt: null, cancellationToken);
                }

                progressTracker.ItemCompleted();
                return;
            }

            ShowGoldBarForCodeCleanupConfigurationIfNeeded(document);
            Format(args.TextView, document, selectionOpt: null, cancellationToken);
        }

        private async Task<ImmutableArray<TextChange>> GetCodeCleanupAndFormatChangesAsync(
            Document document, ICodeCleanupService codeCleanupService,
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            var newDoc = await codeCleanupService.CleanupAsync(
                document, progressTracker, cancellationToken).ConfigureAwait(false);

            var changes = await newDoc.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);
            return changes.ToImmutableArrayOrEmpty();
        }
    }
}
