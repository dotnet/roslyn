// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Formatting
{
    internal partial class FormatCommandHandler : ForegroundThreadAffinitizedObject
    {
        private const string s_experimentName = "CleanupOn";

        public VSCommanding.CommandState GetCommandState(FormatDocumentCommandArgs args)
        {
            return GetCommandState(args.SubjectBuffer);
        }

        private void ShowGoldBarForCodeCleanupConfiguration(Document document, bool performAdditionalCodeCleanupDuringFormatting)
        {
            AssertIsForeground();
            Logger.Log(FunctionId.CodeCleanupInfobar_BarDisplayed, KeyValueLogMessage.NoProperty);

            var workspace = document.Project.Solution.Workspace;

            // if info bar was shown already in same VS session, no need to show it again
            if (workspace.Options.GetOption(CodeCleanupOptions.CodeCleanupInfoBarShown, document.Project.Language))
            {
                return;
            }

            // set as infobar shown so that we never show it again in same VS session. we might show it again
            // in other VS sessions if it is not explicitly configured yet
            workspace.Options = workspace.Options.WithChangedOption(
                CodeCleanupOptions.CodeCleanupInfoBarShown, document.Project.Language, value: true);

            var optionPageService = document.GetLanguageService<IOptionPageService>();
            var infoBarService = document.Project.Solution.Workspace.Services.GetRequiredService<IInfoBarService>();

            // wording for the Code Cleanup infobar will be different depending on if the feature is enabled
            var infoBarMessage = performAdditionalCodeCleanupDuringFormatting
                ? EditorFeaturesResources.Format_document_performed_additional_cleanup
                : EditorFeaturesResources.Code_cleanup_is_not_configured;

            var configButtonText = performAdditionalCodeCleanupDuringFormatting
                ? EditorFeaturesResources.Change_configuration
                : EditorFeaturesResources.Configure_it_now;

            infoBarService.ShowInfoBarInGlobalView(
                infoBarMessage,
                new InfoBarUI(configButtonText,
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
                            CodeCleanupOrFormat(args, document, codeCleanupService, c.ProgressTracker, c.CancellationToken);
                        }

                        transaction.Complete();
                    }
                });

            return true;
        }

        private void CodeCleanupOrFormat(
            FormatDocumentCommandArgs args, Document document, ICodeCleanupService codeCleanupService,
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;
            var isFeatureTurnedOnThroughABTest = TurnOnCodeCleanupForGroupBIfInABTest(document, workspace);
            var performAdditionalCodeCleanupDuringFormatting = workspace.Options.GetOption(CodeCleanupOptions.PerformAdditionalCodeCleanupDuringFormatting, document.Project.Language);

            // if feature is turned on through AB test, we need to show the Gold bar even if they set NeverShowCodeCleanupInfoBarAgain == true before
            if (isFeatureTurnedOnThroughABTest ||
                !workspace.Options.GetOption(CodeCleanupOptions.NeverShowCodeCleanupInfoBarAgain, document.Project.Language))
            {
                // Show different gold bar text depends on PerformAdditionalCodeCleanupDuringFormatting value
                ShowGoldBarForCodeCleanupConfiguration(document, performAdditionalCodeCleanupDuringFormatting);
            }

            if (performAdditionalCodeCleanupDuringFormatting)
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
            }
            else
            {
                Format(args.TextView, document, selectionOpt: null, cancellationToken);
            }
        }

        private static bool TurnOnCodeCleanupForGroupBIfInABTest(Document document, Workspace workspace)
        {
            // If the feature is OFF and the feature options have not yet been Enabled to their assigned flight, do so now
            // Do not reset the options if we already set it for them once, so options won't get reset if user manually disabled it already after it is enabled by the flight
            if (!workspace.Options.GetOption(CodeCleanupOptions.PerformAdditionalCodeCleanupDuringFormatting, document.Project.Language)
                && !workspace.Options.GetOption(CodeCleanupABTestOptions.SettingIsAlreadyUpdatedByExperiment))
            {
                var experimentationService = document.Project.Solution.Workspace.Services.GetService<IExperimentationService>();

                if (experimentationService != null
                    && experimentationService.IsExperimentEnabled(s_experimentName))
                {
                    workspace.Options = workspace.Options.WithChangedOption(CodeCleanupABTestOptions.SettingIsAlreadyUpdatedByExperiment, true)
                                                         .WithChangedOption(CodeCleanupOptions.PerformAdditionalCodeCleanupDuringFormatting, document.Project.Language, true);
                    return true;
                }
            }

            return false;
        }

        private async Task<ImmutableArray<TextChange>> GetCodeCleanupAndFormatChangesAsync(
            Document document, ICodeCleanupService codeCleanupService,
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            var docOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var organizeUsingsSet = new OrganizeUsingsSet(docOptions);
            var enabledDiagnostics = codeCleanupService.GetEnabledDiagnostics(docOptions);

            var newDoc = await codeCleanupService.CleanupAsync(
                document, organizeUsingsSet, enabledDiagnostics, progressTracker, cancellationToken).ConfigureAwait(false);

            var changes = await newDoc.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);
            return changes.ToImmutableArrayOrEmpty();
        }
    }
}
