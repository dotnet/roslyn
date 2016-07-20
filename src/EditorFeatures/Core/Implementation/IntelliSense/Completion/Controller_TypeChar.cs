// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        CommandState ICommandHandler<TypeCharCommandArgs>.GetCommandState(TypeCharCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();

            // We just defer to the editor here.  We do not interfere with typing normal characters.
            return nextHandler();
        }

        void ICommandHandler<TypeCharCommandArgs>.ExecuteCommand(TypeCharCommandArgs args, Action nextHandler)
        {
            Trace.WriteLine("Entered completion command handler for typechar.");

            AssertIsForeground();

            // When a character is typed it is *always* sent through to the editor.  This way the
            // editor always represents what would have been typed had completion not been involved
            // at this point.  That means that if we decide to commit, then undo'ing the commit will
            // return you to the code that you would have typed if completion was not up.
            //
            // The steps we follow for commit are as follows:
            //
            //      1) send the commit character through to the buffer.
            //      2) open a transaction.
            //          2a) roll back the text to before the text was sent through
            //          2b) commit the item.
            //          2c) send the commit character through again.*
            //          2d) commit the transaction.
            //
            // 2c is very important.  it makes sure that post our commit all our normal features
            // run depending on what got typed.  For example if the commit character was (
            // then brace completion may run.  If it was ; then formatting may run.  But, importantly
            // this code doesn't need to know anything about that.  Furthermore, because that code
            // runs within this transaction, then the user can always undo and get to what the code
            // would have been if completion was not involved.
            //
            // 2c*: note sending the commit character through to the buffer again can be controlled
            // by the completion item.  For example, completion items that want to totally handle
            // what gets output into the buffer can ask for this not to happen.  An example of this
            // is override completion.  If the user types "override Method(" then we'll want to 
            // spit out the entire method and *not* also spit out "(" again.

            // In order to support 2a (rolling back), we capture hte state of the buffer before
            // we send the character through.  We then just apply the edits in reverse order to
            // roll us back.
            var initialTextSnapshot = this.SubjectBuffer.CurrentSnapshot;

            var initialCaretPosition = GetCaretPointInViewBuffer();

            // Note: while we're doing this, we don't want to hear about buffer changes (since we
            // know they're going to happen).  So we disconnect and reconnect to the event
            // afterwards.  That way we can hear about changes to the buffer that don't happen
            // through us.

            // Automatic Brace Completion may also move the caret, so unsubscribe from that too
            this.TextView.TextBuffer.PostChanged -= OnTextViewBufferPostChanged;
            this.TextView.Caret.PositionChanged -= OnCaretPositionChanged;

            // In Venus/Razor, the user might be typing on the buffer's seam. This means that,
            // depending on the character typed, the character may not go into our buffer.
            var isOnSeam = IsOnSeam();

            try
            {
                nextHandler();
            }
            finally
            {
                this.TextView.TextBuffer.PostChanged += OnTextViewBufferPostChanged;
                this.TextView.Caret.PositionChanged += OnCaretPositionChanged;
            }

            // We only want to process typechar if it is a normal typechar and no one else is
            // involved.  i.e. if there was a typechar, but someone processed it and moved the caret
            // somewhere else then we don't want completion.  Also, if a character was typed but
            // something intercepted and placed different text into the editor, then we don't want
            // to proceed. 
            if (this.TextView.TypeCharWasHandledStrangely(this.SubjectBuffer, args.TypedChar))
            {
                Trace.WriteLine("typechar was handled by someone else, cannot have a completion session.");

                if (sessionOpt != null)
                {
                    // If we're on a seam (razor) with a computation, and the user types a character 
                    // that goes into the other side of the seam, the character may be a commit character.
                    // If it's a commit character, just commit without trying to check caret position,
                    // since the caret is no longer in our buffer.
                    if (isOnSeam && this.IsCommitCharacter(args.TypedChar))
                    {
                        Trace.WriteLine("typechar was on seam and a commit char, cannot have a completion session.");

                        this.CommitOnTypeChar(args.TypedChar, initialTextSnapshot, nextHandler);
                        return;
                    }
                    else if (_autoBraceCompletionChars.Contains(args.TypedChar) &&
                             this.SubjectBuffer.GetOption(InternalFeatureOnOffOptions.AutomaticPairCompletion) &&
                             this.IsCommitCharacter(args.TypedChar))
                    {
                        Trace.WriteLine("typechar was brace completion char and a commit char, cannot have a completion session.");

                        // I don't think there is any better way than this. if typed char is one of auto brace completion char,
                        // we don't do multiple buffer change check
                        this.CommitOnTypeChar(args.TypedChar, initialTextSnapshot, nextHandler);
                        return;
                    }
                    else
                    {
                        Trace.WriteLine("we stop model computation, cannot have a completion session.");

                        // If we were computing anything, we stop.  We only want to process a typechar
                        // if it was a normal character.
                        this.StopModelComputation();
                    }
                }

                return;
            }

            var completionService = this.GetCompletionService();
            if (completionService == null)
            {
                Trace.WriteLine("handling typechar, completion service is null, cannot have a completion session.");

                return;
            }

            var options = GetOptions();
            Contract.ThrowIfNull(options);

            var isTextuallyTriggered = IsTextualTriggerCharacter(completionService, args.TypedChar, options);
            var isPotentialFilterCharacter = IsPotentialFilterCharacter(args);
            var trigger = CompletionTrigger.CreateInsertionTrigger(args.TypedChar);

            if (sessionOpt == null)
            {
                // No computation at all.  If this is not a trigger character, we just ignore it and
                // stay in this state.  Otherwise, if it's a trigger character, start up a new
                // computation and start computing the model in the background.
                if (isTextuallyTriggered)
                {
                    Trace.WriteLine("no completion session yet and this is a trigger char, starting model computation.");

                    // First create the session that represents that we now have a potential
                    // completion list.  Then tell it to start computing.
                    StartNewModelComputation(completionService, trigger, filterItems: true, dismissIfEmptyAllowed: true);
                    return;
                }
                else
                {
                    Trace.WriteLine("no completion session yet and this is NOT a trigger char, we won't have completion.");

                    // No need to do anything.  Just stay in the state where we have no session.
                    return;
                }
            }
            else
            {
                Trace.WriteLine("we have a completion session.");

                sessionOpt.UpdateModelTrackingSpan(initialCaretPosition);

                // If the session is up, it may be in one of many states.  It may know nothing
                // (because it is currently computing the list of completions).  Or it may have a
                // list of completions that it has filtered. 

                // If the user types something which is absolutely known to be a filter character
                // then we can just proceed without blocking.
                if (isPotentialFilterCharacter)
                {
                    if (isTextuallyTriggered)
                    {
                        Trace.WriteLine("computing completion again and filtering...");

                        // The character typed was something like "a".  It can both filter a list if
                        // we have computed one, or it can trigger a new list.  Ask the computation
                        // to compute again. If nothing has been computed, then it will try to
                        // compute again, otherwise it will just ignore this request.
                        sessionOpt.ComputeModel(completionService, trigger, _roles, options);
                    }

                    // Now filter whatever result we have.
                    sessionOpt.FilterModel(
                        CompletionFilterReason.TypeChar,
                        recheckCaretPosition: false,
                        dismissIfEmptyAllowed: true,
                        filterState: null);
                }
                else
                {
                    // It wasn't a trigger or filter character. At this point, we make our
                    // determination on what to do based on what has actually been computed and
                    // what's being typed. This means waiting on the session and will effectively
                    // block the user.

                    // Again, from this point on we must block on the computation to decide what to
                    // do.

                    // What they type may end up filtering, committing, or else will dismiss.
                    //
                    // For example, we may filter in cases like this: "Color."
                    //
                    // "Color" will have already filtered the list down to some things like
                    // "Color", "Color.Red", "Color.Blue", etc.  When we process the 'dot', we
                    // actually want to filter some more.  But we can't know that ahead of time until
                    // we have computed the list of completions.
                    if (this.IsFilterCharacter(args.TypedChar))
                    {
                        Trace.WriteLine("filtering the session...");

                        // Known to be a filter character for the currently selected item.  So just 
                        // filter the session.
                        sessionOpt.FilterModel(CompletionFilterReason.TypeChar,
                            recheckCaretPosition: false,
                            dismissIfEmptyAllowed: true,
                            filterState: null);
                        return;
                    }

                    // It wasn't a filter character.  We'll either commit what's selected, or we'll
                    // dismiss the completion list.  First, ensure that what was typed is in the
                    // buffer.

                    // Now, commit if it was a commit character.
                    if (this.IsCommitCharacter(args.TypedChar))
                    {
                        Trace.WriteLine("committing the session...");

                        // Known to be a commit character for the currently selected item.  So just
                        // commit the session.
                        this.CommitOnTypeChar(args.TypedChar, initialTextSnapshot, nextHandler);
                    }
                    else
                    {
                        Trace.WriteLine("dismissing the session...");

                        // Now dismiss the session.
                        this.StopModelComputation();
                    }

                    // The character may commit/dismiss and then trigger completion again. So check
                    // for that here.

                    if (isTextuallyTriggered)
                    {
                        Trace.WriteLine("the char commit/dismiss -ed a session and is trigerring completion again. starting model computation.");

                        // First create the session that represents that we now have a potential
                        // completion list.
                        StartNewModelComputation(
                            completionService, trigger, filterItems: true, dismissIfEmptyAllowed: true);
                        return;
                    }
                }
            }
        }

        private bool IsOnSeam()
        {
            var caretPoint = TextView.Caret.Position.Point;
            var point1 = caretPoint.GetPoint(this.SubjectBuffer, PositionAffinity.Predecessor);
            var point2 = caretPoint.GetPoint(this.SubjectBuffer, PositionAffinity.Successor);
            if (point1.HasValue && point1 != point2)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// A potential filter character is something that can filter a completion lists and is
        /// *guaranteed* to not be a commit character.
        /// </summary>
        private static bool IsPotentialFilterCharacter(TypeCharCommandArgs args)
        {
            // TODO(cyrusn): Actually use the right unicode categories here.
            return char.IsLetter(args.TypedChar)
                || char.IsNumber(args.TypedChar)
                || args.TypedChar == '_';
        }

        private Document GetDocument()
        {
            return this.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        }

        private CompletionHelper GetCompletionHelper()
        {
            var document = GetDocument();
            if (document != null)
            {
                return CompletionHelper.GetHelper(document);
            }

            return null;
        }

        private bool IsTextualTriggerCharacter(CompletionService completionService, char ch, OptionSet options)
        {
            AssertIsForeground();

            // Note: When this function is called we've already guaranteed that
            // TypeCharWasHandledStrangely returned false.  That means we know that the caret is in
            // our buffer, and is after the character just typed.

            var caretPosition = this.TextView.GetCaretPoint(this.SubjectBuffer).Value;
            var previousPosition = caretPosition - 1;
            Contract.ThrowIfFalse(this.SubjectBuffer.CurrentSnapshot[previousPosition] == ch);

            var trigger = CompletionTrigger.CreateInsertionTrigger(ch);
            return completionService.ShouldTriggerCompletion(previousPosition.Snapshot.AsText(), caretPosition, trigger, _roles, options);
        }

        private bool IsCommitCharacter(char ch)
        {
            AssertIsForeground();

            // TODO(cyrusn): Find a way to allow the user to cancel out of this.
            var model = sessionOpt.WaitForModel();
            if (model == null || model.IsSoftSelection)
            {
                return false;
            }

            if (model.SelectedItem.IsSuggestionModeItem)
            {
                return char.IsLetterOrDigit(ch);
            }

            var completionService = GetCompletionService();
            var textTypedSoFar = GetTextTypedSoFar(model, model.SelectedItem.Item);
            return IsCommitCharacter(
                completionService.GetRules(), model.SelectedItem.Item, ch, textTypedSoFar);
        }

        /// <summary>
        /// Internal for testing purposes only.
        /// </summary>
        internal static bool IsCommitCharacter(
            CompletionRules completionRules, CompletionItem item, char ch, string textTypedSoFar)
        {
            // First see if the item has any specifc commit rules it wants followed.
            foreach (var rule in item.Rules.CommitCharacterRules)
            {
                switch (rule.Kind)
                {
                    case CharacterSetModificationKind.Add:
                        if (rule.Characters.Contains(ch))
                        {
                            return true;
                        }
                        continue;

                    case CharacterSetModificationKind.Remove:
                        if (rule.Characters.Contains(ch))
                        {
                            return false;
                        }
                        continue;

                    case CharacterSetModificationKind.Replace:
                        return rule.Characters.Contains(ch);
                }
            }

            // general rule: if the filtering text exactly matches the start of the item then it must be a filter character
            if (TextTypedSoFarMatchesItem(item, ch, textTypedSoFar))
            {
                return false;
            }

            // Fall back to the default rules for this language's completion service.
            return completionRules.DefaultCommitCharacters.IndexOf(ch) >= 0;
        }

        private bool IsFilterCharacter(char ch)
        {
            AssertIsForeground();

            // TODO(cyrusn): Find a way to allow the user to cancel out of this.
            var model = sessionOpt.WaitForModel();
            if (model == null)
            {
                return false;
            }

            if (model.SelectedItem.IsSuggestionModeItem)
            {
                return char.IsLetterOrDigit(ch);
            }

            var textTypedSoFar = GetTextTypedSoFar(model, model.SelectedItem.Item);
            return IsFilterCharacter(model.SelectedItem.Item, ch, textTypedSoFar);
        }

        private static bool TextTypedSoFarMatchesItem(CompletionItem item, char ch, string textTypedSoFar)
        {
            if (textTypedSoFar.Length > 0)
            {
                return item.DisplayText.StartsWith(textTypedSoFar, StringComparison.CurrentCultureIgnoreCase) ||
                       item.FilterText.StartsWith(textTypedSoFar, StringComparison.CurrentCultureIgnoreCase);
            }

            return false;
        }

        private static bool IsFilterCharacter(CompletionItem item, char ch, string textTypedSoFar)
        {
            // First see if the item has any specific filter rules it wants followed.
            foreach (var rule in item.Rules.FilterCharacterRules)
            {
                switch (rule.Kind)
                {
                    case CharacterSetModificationKind.Add:
                        if (rule.Characters.Contains(ch))
                        {
                            return true;
                        }
                        continue;

                    case CharacterSetModificationKind.Remove:
                        if (rule.Characters.Contains(ch))
                        {
                            return false;
                        }
                        continue;

                    case CharacterSetModificationKind.Replace:
                        return rule.Characters.Contains(ch);
                }
            }

            // general rule: if the filtering text exactly matches the start of the item then it must be a filter character
            if (TextTypedSoFarMatchesItem(item, ch, textTypedSoFar))
            {
                return true;
            }

            return false;
        }

        private string GetTextTypedSoFar(Model model, CompletionItem selectedItem)
        {
            var textSnapshot = this.TextView.TextSnapshot;
            var viewSpan = model.GetViewBufferSpan(selectedItem.Span);
            var filterText = model.GetCurrentTextInSnapshot(
                viewSpan, textSnapshot, GetCaretPointInViewBuffer());
            return filterText;
        }

        private void CommitOnTypeChar(
            char ch, ITextSnapshot initialTextSnapshot, Action nextHandler)
        {
            AssertIsForeground();

            // Note: this function is called after the character has already been inserted into the
            // buffer.
            var model = sessionOpt.WaitForModel();

            // We only call CommitOnTypeChar if ch was a commit character.  And we only know if ch
            // was commit character if we had a selected item.
            Contract.ThrowIfNull(model);

            this.Commit(
                model.SelectedItem, model, ch,
                initialTextSnapshot, nextHandler);
        }
    }
}