// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
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

            var codeStyleConfigureService = workspace.Services.GetRequiredService<ICodeStyleConfigureService>();
            var infoBarService = workspace.Services.GetRequiredService<IInfoBarService>();
            infoBarService.ShowInfoBarInGlobalView(
                EditorFeaturesResources.Code_cleanup_is_not_configured,
                new InfoBarUI(EditorFeaturesResources.Configure_it_now,
                              kind: InfoBarUI.UIKind.Button,
                               () =>
                               {
                                   codeStyleConfigureService.ShowFormattingOptionPage();
                                   _infoBarOpen = false;
                               }),
                new InfoBarUI(EditorFeaturesResources.Donot_show_this_again,
                              kind: InfoBarUI.UIKind.Button,
                               () => { _infoBarOpen = false; }));
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

            if (!document.Project.Solution.Workspace.Options.GetOption(FeatureOnOffOptions.IsCodeCleanupRulesConfigured, LanguageNames.CSharp))
            {
                ShowGoldBarForCodeCleanupConfiguration(document.Project.Solution.Workspace);
            }

            using (context.WaitContext.AddScope(allowCancellation: true, EditorFeaturesResources.Formatting_document))
            {
                var cancellationToken = context.WaitContext.UserCancellationToken;

                using (var transaction = new CaretPreservingEditTransaction(
                    EditorFeaturesResources.Formatting, args.TextView, _undoHistoryRegistry, _editorOperationsFactoryService))
                {
                    document = _codeCleanupService.CleanupDocument(document, cancellationToken);

                    var formattingService = document.GetLanguageService<IEditorFormattingService>();
                    if (formattingService == null || !formattingService.SupportsFormatDocument)
                    {
                        return false;
                    }

                    Format(args.TextView, document, null, cancellationToken);

                    transaction.Complete();
                    return true;
                }
            }
        }
    }
}
