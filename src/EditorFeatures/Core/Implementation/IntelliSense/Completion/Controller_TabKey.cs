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
                // The user may be trying to invoke snippets
                var completionService = GetCompletionService();
                if (completionService != null &&
                    completionService.SupportSnippetCompletionListOnTab &&
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

        private bool TryInvokeSnippetCompletion(TabKeyCommandArgs args, ICompletionService completionService)
        {
            var subjectBuffer = args.SubjectBuffer;
            var caretPoint = args.TextView.GetCaretPoint(subjectBuffer).Value.Position;

            var text = subjectBuffer.AsTextContainer().CurrentText;

            // Delete the ? and invoke completion
            Workspace workspace = null;

            if (Workspace.TryGetWorkspace(subjectBuffer.AsTextContainer(), out workspace))
            {
                var documentId = workspace.GetDocumentIdInCurrentContext(subjectBuffer.AsTextContainer());
                if (documentId != null)
                {
                    var document = workspace.CurrentSolution.GetDocument(documentId);
                    if (document != null)
                    {
                        var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

                        if (caretPoint >= 2 && text[caretPoint - 1] == '?' && QuestionMarkIsPrecededByIdentifierAndWhitespace(text, caretPoint - 2, syntaxFacts))
                        {
                            var textChange = new TextChange(TextSpan.FromBounds(caretPoint - 1, caretPoint), string.Empty);
                            workspace.ApplyTextChanges(documentId, textChange, CancellationToken.None);
                            this.StartNewModelComputation(completionService, CompletionTriggerInfo.CreateSnippetTriggerInfo(), filterItems: false);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool QuestionMarkIsPrecededByIdentifierAndWhitespace(SourceText text, int p, ISyntaxFactsService syntaxFacts)
        {
            int start = text.Lines.GetLineFromPosition(p).Start;
            bool seenIdentifier = false;

            while (p >= start)
            {
                if (!(syntaxFacts.IsIdentifierStartCharacter(text[p]) || syntaxFacts.IsIdentifierPartCharacter(text[p])))
                {
                    break;
                }

                seenIdentifier = true;
                p--;
            }

            while (p >= start)
            {
                if (!char.IsWhiteSpace(text[p]))
                {
                    break;
                }

                p--;
            }

            return seenIdentifier && p <= start;
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

            var selectedItem = Controller.GetExternallyUsableCompletionItem(model.SelectedItem);

            // If the selected item is the builder, there's not actually any work to do to commit
            if (selectedItem.IsBuilder)
            {
                committed = true;
                this.StopModelComputation();
                return;
            }

            var textChange = GetCompletionRules().GetTextChange(selectedItem);

            Commit(selectedItem, textChange, model, null);
            committed = true;
        }
    }
}
