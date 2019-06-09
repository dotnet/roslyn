// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        VSCommanding.CommandState IChainedCommandHandler<TabKeyCommandArgs>.GetCommandState(TabKeyCommandArgs args, System.Func<VSCommanding.CommandState> nextHandler)
        {
            AssertIsForeground();
            return nextHandler();
        }

        void IChainedCommandHandler<TabKeyCommandArgs>.ExecuteCommand(TabKeyCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            AssertIsForeground();

            if (sessionOpt == null)
            {
                // The user may be trying to invoke snippets through question-tab
                if (TryInvokeSnippetCompletion(args))
                {
                    // We've taken care of the tab. Don't send it to the buffer.
                    return;
                }

                // No computation.  Nothing to do.  Just let the editor handle this.
                nextHandler();
                return;
            }

            // We are computing a model.  Try to commit the selected item if there was one. Note: If
            // it was able to commit, then we never send the tab to the buffer. That way, if the
            // user does an undo they'll get to the code they had *before* they hit tab. If the
            // session wasn't able to commit, then we do send the tab through to the buffer.
            CommitOnTab(nextHandler);

            // After tab, we always want to be in an inactive state.
            this.DismissSessionIfActive();
        }

        private bool TryInvokeSnippetCompletion(TabKeyCommandArgs args)
        {
            var subjectBuffer = args.SubjectBuffer;
            var caretPoint = args.TextView.GetCaretPoint(subjectBuffer).Value.Position;

            var text = subjectBuffer.AsTextContainer().CurrentText;
            // If the user types "<line start><spaces><question><tab>"
            // then the editor takes over and shows the normal *full* snippet picker UI.
            // i.e. the picker with all the folders and snippet organization.
            //
            // However, if the user instead has something like
            //
            //      "<line start><spaces><identifier><question><tab>"
            //
            // Then we take over and we show a completion list with all snippets in it
            // in a flat list.  This enables simple browsing and filtering of all items
            // based on what the user typed so far.
            //
            // If we detect this pattern, then we delete the previous character (the
            // question mark) and we don't send the tab through to the editor.  In 
            // essence, the <quesiton><tab> acts as the trigger, and we act as if that
            // text never makes it into the buffer.

            if (!Workspace.TryGetWorkspace(subjectBuffer.AsTextContainer(), out var workspace))
            {
                return false;
            }

            var documentId = workspace.GetDocumentIdInCurrentContext(subjectBuffer.AsTextContainer());
            if (documentId == null)
            {
                return false;
            }

            var document = workspace.CurrentSolution.GetDocument(documentId);
            if (document == null)
            {
                return false;
            }

            // There's was a buffer-Document mapping. We should be able
            // to get a CompletionService.
            var completionService = GetCompletionService();
            Contract.ThrowIfNull(completionService, nameof(completionService));

            var rules = completionService.GetRules();
            if (rules.SnippetsRule != SnippetsRule.IncludeAfterTypingIdentifierQuestionTab)
            {
                return false;
            }

            var syntaxFactsOpt = document.GetLanguageService<ISyntaxFactsService>();
            if (syntaxFactsOpt == null ||
                caretPoint < 2 ||
                text[caretPoint - 1] != '?' ||
                !CompletionSource.QuestionMarkIsPrecededByIdentifierAndWhitespace(text, caretPoint - 1, syntaxFactsOpt))
            {
                return false;
            }

            // Because <question><tab> is actually a command to bring up snippets,
            // we delete the last <question> that was typed.
            var textChange = new TextChange(TextSpan.FromBounds(caretPoint - 1, caretPoint), string.Empty);
            workspace.ApplyTextChanges(documentId, textChange, CancellationToken.None);
            this.StartNewModelComputation(
                completionService, new CompletionTrigger(CompletionTriggerKind.Snippets));
            return true;
        }

        private void CommitOnTab(Action nextHandler)
        {
            AssertIsForeground();

            var model = WaitForModel();

            // If there's no model, then there's nothing to commit.  So send the tab
            // through to the editor.
            if (model == null)
            {
                nextHandler();
                return;
            }

            // If there's no selected item, there's nothing to commit
            if (model.SelectedItemOpt == null)
            {
                nextHandler();
                return;
            }

            // If the selected item is the builder, there's not actually any work to do to commit
            if (model.SelectedItemOpt != model.SuggestionModeItem)
            {
                CommitOnNonTypeChar(model.SelectedItemOpt, model);
            }
        }
    }
}
