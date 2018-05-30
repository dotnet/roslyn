// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.CodeCleanup;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Formatting
{
    internal partial class FormatCommandHandler : ForegroundThreadAffinitizedObject
    {
        private bool _infoBarOpen = false;
        public VSCommanding.CommandState GetCommandState(FormatDocumentCommandArgs args)
        {
            return GetCommandState(args.SubjectBuffer);
        }

        private void ShowGoldBarForCodeCleanupConfiguration(Workspace workspace)
        {
            AssertIsForeground();

            // If the gold bar is already open, do not show
            if (_infoBarOpen)
            {
                return;
            }

            _infoBarOpen = true;

            var optionPageService = workspace.Services.GetRequiredService<IOptionPageService>();
            var infoBarService = workspace.Services.GetRequiredService<IInfoBarService>();
            infoBarService.ShowInfoBarInGlobalView(
                EditorFeaturesResources.Code_cleanup_is_not_configured,
                new InfoBarUI(EditorFeaturesResources.Configure_it_now,
                              kind: InfoBarUI.UIKind.Button,
                               () =>
                               {
                                   optionPageService.ShowFormattingOptionPage();
                                   _infoBarOpen = false;
                               }),
                new InfoBarUI(EditorFeaturesResources.Donot_show_this_message_again,
                              kind: InfoBarUI.UIKind.Button,
                              () => {}));
        }

        public bool ExecuteCommand(FormatDocumentCommandArgs args, CommandExecutionContext context)
        {
            if (!args.SubjectBuffer.CanApplyChangeDocumentToWorkspace())
            {
                return false;
            }

            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return false;
            }

            return ExecuteCommandAsync(document, args.TextView, context).Result;
        }

        private async Task<bool> ExecuteCommandAsync(Document document, ITextView textView, CommandExecutionContext context)
        {
            var docOptions = await document.GetOptionsAsync(context.WaitContext.UserCancellationToken).ConfigureAwait(false);

            if (!docOptions.GetOption(FeatureOnOffOptions.IsCodeCleanupRulesConfigured))
            {
                ShowGoldBarForCodeCleanupConfiguration(document.Project.Solution.Workspace);
                await Format(textView, document, selectionOpt: null, context.WaitContext.UserCancellationToken).ConfigureAwait(false);
            }
            else
            {

                using (context.WaitContext.AddScope(allowCancellation: true, EditorFeaturesResources.Formatting_document))
                {
                    var cancellationToken = context.WaitContext.UserCancellationToken;

                    using (var transaction = new CaretPreservingEditTransaction(
                        EditorFeaturesResources.Formatting, textView, _undoHistoryRegistry, _editorOperationsFactoryService))
                    {
                        var formattingService = document.GetLanguageService<IEditorFormattingService>();
                        if (formattingService == null || !formattingService.SupportsFormatDocument)
                        {
                            return false;
                        }

                        await Format(textView, document, selectionOpt: null, cancellationToken).ConfigureAwait(false);

                        var codeCleanupService = document.GetLanguageService<ICodeCleanupService>();
                        if (codeCleanupService == null)
                        {
                            return false;
                        }

                        var oldDoc = document;
                        var newDoc = await codeCleanupService.CleanupDocument(document, cancellationToken).ConfigureAwait(false);

                        var codeFixChanges = await newDoc.GetTextChangesAsync(oldDoc, cancellationToken).ConfigureAwait(false);
                        if (codeFixChanges.Count() > 0)
                        {
                            ApplyChanges(oldDoc, codeFixChanges.ToList(), selectionOpt: null, cancellationToken);
                        }

                        transaction.Complete();
                    }
                }
            }

            return true;
        }
    }
}
