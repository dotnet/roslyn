// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
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
                                  optionPageService.ShowFormattingOptionPage();
                              }),
                new InfoBarUI(EditorFeaturesResources.Do_not_show_this_message_again,
                              kind: InfoBarUI.UIKind.Button,
                              () =>
                              {
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

            using (context.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Formatting_document))
            {
                var cancellationToken = context.OperationContext.UserCancellationToken;

                using (var transaction = new CaretPreservingEditTransaction(
                    EditorFeaturesResources.Formatting, args.TextView, _undoHistoryRegistry, _editorOperationsFactoryService))
                {
                    var codeCleanupService = document.GetLanguageService<ICodeCleanupService>();
                    if (codeCleanupService == null)
                    {
                        Format(args.TextView, document, selectionOpt: null, cancellationToken);
                    }
                    else
                    {
                        CodeCleanupOrFormat(args, document, cancellationToken);
                    }

                    transaction.Complete();
                }
            }

            return true;
        }

        private void CodeCleanupOrFormat(FormatDocumentCommandArgs args, Document document, CancellationToken cancellationToken)
        {
            var codeCleanupService = document.GetLanguageService<ICodeCleanupService>();

            var docOptions = document.GetOptionsAsync(cancellationToken).WaitAndGetResult(cancellationToken);

            // simplest pass. code cleanup is on. just run code clean up.
            if (docOptions.GetOption(CodeCleanupOptions.PerformAdditionalCodeCleanupDuringFormatting))
            {
                // Code cleanup
                var oldDoc = document;
                var codeCleanupChanges = GetCodeCleanupAndFormatChangesAsync(document, codeCleanupService, cancellationToken).WaitAndGetResult(cancellationToken);

                if (codeCleanupChanges != null && codeCleanupChanges.Count() > 0)
                {
                    ApplyChanges(oldDoc, codeCleanupChanges.ToList(), selectionOpt: null, cancellationToken);
                }

                return;
            }

            ShowGoldBarForCodeCleanupConfigurationIfNeeded(document);
            Format(args.TextView, document, selectionOpt: null, cancellationToken);
        }

        private async Task<IEnumerable<TextChange>> GetCodeCleanupAndFormatChangesAsync(Document document, ICodeCleanupService codeCleanupService, CancellationToken cancellationToken)
        {
            var newDoc = await codeCleanupService.CleanupAsync(document, cancellationToken).ConfigureAwait(false);

            return await newDoc.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);
        }
    }
}
