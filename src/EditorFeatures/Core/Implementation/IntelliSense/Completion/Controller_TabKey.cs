// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        CommandState ICommandHandler<TabKeyCommandArgs>.GetCommandState(TabKeyCommandArgs args, System.Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return nextHandler();
        }

        void ICommandHandler<TabKeyCommandArgs>.ExecuteCommand(TabKeyCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();

            if (sessionOpt == null)
            {
                // The user may be trying to invoke snippets through question-tab
                var completionService = GetCompletionService();

                if (completionService != null &&
                    TryInvokeSnippetCompletion(args, completionService))
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

            bool committed;
            CommitOnTab(out committed);

            // We did not commit based on tab.  So our computation will still be running.  Stop it now.
            // Also, send the tab through to the editor.
            if (!committed)
            {
                this.StopModelComputation();
                nextHandler();
            }
        }

        private bool TryInvokeSnippetCompletion(TabKeyCommandArgs args, CompletionService completionService)
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
            Workspace workspace = null;

            if (!Workspace.TryGetWorkspace(subjectBuffer.AsTextContainer(), out workspace))
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

            var rules = GetCompletionService().GetRules();
            if (rules.SnippetsRule != SnippetsRule.IncludeAfterTypingIdentifierQuestionTab)
            {
                return false;
            }

            var syntaxFactsOpt = document.GetLanguageService<ISyntaxFactsService>();
            if (syntaxFactsOpt == null ||
                caretPoint < 2 ||
                text[caretPoint - 1] != '?' ||
                !QuestionMarkIsPrecededByIdentifierAndWhitespace(text, caretPoint - 1, syntaxFactsOpt))
            {
                return false;
            }

            // Because <question><tab> is actually a command to bring up snippets,
            // we delete the last <question> that was typed.
            var textChange = new TextChange(TextSpan.FromBounds(caretPoint - 1, caretPoint), string.Empty);
            workspace.ApplyTextChanges(documentId, textChange, CancellationToken.None);
            this.StartNewModelComputation(
                completionService, new CompletionTrigger(CompletionTriggerKind.Snippets),
                filterItems: false, dismissIfEmptyAllowed: true);
            return true;
        }

        private bool QuestionMarkIsPrecededByIdentifierAndWhitespace(
            SourceText text, int questionPosition, ISyntaxFactsService syntaxFacts)
        {
            var startOfLine = text.Lines.GetLineFromPosition(questionPosition).Start;

            // First, skip all the whitespace.
            var current = startOfLine;
            while (current < questionPosition && char.IsWhiteSpace(text[current]))
            {
                current++;
            }

            if (current < questionPosition && syntaxFacts.IsIdentifierStartCharacter(text[current]))
            {
                current++;
            }
            else
            {
                return false;
            }

            while (current < questionPosition && syntaxFacts.IsIdentifierPartCharacter(text[current]))
            {
                current++;
            }

            return current == questionPosition;
        }

        private void CommitOnTab(out bool committed)
        {
            AssertIsForeground();

            var model = sessionOpt.WaitForModel();

            // If there's no model, then there's nothing to commit.
            if (model == null)
            {
                committed = false;
                return;
            }

            // If the selected item is the builder, there's not actually any work to do to commit
            if (model.SelectedItem.IsSuggestionModeItem)
            {
                committed = true;
                this.StopModelComputation();
                return;
            }

            CommitOnNonTypeChar(model.SelectedItem, model);
            committed = true;
        }
    }
}