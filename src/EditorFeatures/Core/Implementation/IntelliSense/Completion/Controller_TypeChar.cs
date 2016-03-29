// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Extensibility.Completion;
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

            var initialCaretPosition = GetCaretPointInViewBuffer();

            // When a character is typed it is *always* sent through to the editor.  This way the
            // editor always represents what would have been typed had completion not been involved
            // at this point.  After we send the character into the buffer we then decide what to do
            // with the completion set.  If we decide to commit it then we will replace the
            // appropriate span (which will include the character just sent to the buffer) with the
            // appropriate insertion text *and* the character typed.  This way, after we commit, the
            // editor has the insertion text of the selected item, and the character typed.  It
            // also means that if we then undo that we'll see the text that would have been typed
            // had no completion been active.

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

                        this.CommitOnTypeChar(args.TypedChar);
                        return;
                    }
                    else if (_autoBraceCompletionChars.Contains(args.TypedChar) &&
                             this.SubjectBuffer.GetOption(InternalFeatureOnOffOptions.AutomaticPairCompletion) &&
                             this.IsCommitCharacter(args.TypedChar))
                    {
                        Trace.WriteLine("typechar was brace completion char and a commit char, cannot have a completion session.");

                        // I don't think there is any better way than this. if typed char is one of auto brace completion char,
                        // we don't do multiple buffer change check
                        this.CommitOnTypeChar(args.TypedChar);
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
            var triggerInfo = CompletionTriggerInfo.CreateTypeCharTriggerInfo(args.TypedChar);

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
                    StartNewModelComputation(completionService, triggerInfo, filterItems: true);
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
                        sessionOpt.ComputeModel(completionService, triggerInfo, options, GetCompletionProviders());
                    }

                    // Now filter whatever result we have.
                    sessionOpt.FilterModel(CompletionFilterReason.TypeChar);
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
                        sessionOpt.FilterModel(CompletionFilterReason.TypeChar);
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
                        this.CommitOnTypeChar(args.TypedChar);
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
                        StartNewModelComputation(completionService, triggerInfo, filterItems: true);
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

        private CompletionRules GetCompletionRules()
        {
            var document = this.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document != null)
            {
                var service = document.Project.LanguageServices.GetService<ICompletionService>();
                if (service != null)
                {
                    return service.GetCompletionRules();
                }
            }

            return null;
        }

        private IEnumerable<CompletionListProvider> GetCompletionProviders()
        {
            var defaultProviders = GetDefaultCompletionProviders();

            Workspace workspace;
            if (Workspace.TryGetWorkspace(this.SubjectBuffer.AsTextContainer(), out workspace))
            {
                var extensionProviders = workspace.Services.SelectMatchingExtensionValues(
                    _allCompletionProviders, this.SubjectBuffer.ContentType, this.TextView.Roles);

                return defaultProviders.Concat(extensionProviders.Where(p => !(p is SnippetCompletionProvider)));
            }

            return defaultProviders;
        }

        private IEnumerable<CompletionListProvider> GetSnippetCompletionProviders()
        {
            Workspace workspace;
            if (Workspace.TryGetWorkspace(this.SubjectBuffer.AsTextContainer(), out workspace))
            {
                var extensionProviders = workspace.Services.SelectMatchingExtensionValues(
                    _allCompletionProviders, this.SubjectBuffer.ContentType);

                return extensionProviders.OfType<SnippetCompletionProvider>();
            }

            return SpecializedCollections.EmptyEnumerable<CompletionListProvider>();
        }

        private IEnumerable<CompletionListProvider> GetDefaultCompletionProviders()
        {
            var document = this.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document != null)
            {
                var service = document.Project.LanguageServices.GetService<ICompletionService>();
                if (service != null)
                {
                    return service.GetDefaultCompletionProviders();
                }
            }

            return SpecializedCollections.EmptyEnumerable<CompletionListProvider>();
        }

        private bool IsTextualTriggerCharacter(ICompletionService completionService, char ch, OptionSet options)
        {
            AssertIsForeground();

            // Note: When this function is called we've already guaranteed that
            // TypeCharWasHandledStrangely returned false.  That means we know that the caret is in
            // our buffer, and is after the character just typed.

            var previousPosition = this.TextView.GetCaretPoint(this.SubjectBuffer).Value - 1;
            Contract.ThrowIfFalse(this.SubjectBuffer.CurrentSnapshot[previousPosition] == ch);

            return completionService.IsTriggerCharacter(previousPosition.Snapshot.AsText(), previousPosition, GetCompletionProviders(), options);
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

            var selectedItem = GetExternallyUsableCompletionItem(model.SelectedItem);
            if (selectedItem.IsBuilder)
            {
                return char.IsLetterOrDigit(ch);
            }

            var filterText = GetCurrentFilterText(model, selectedItem);

            return GetCompletionRules().IsCommitCharacter(selectedItem, ch, filterText);
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

            var selectedItem = Controller.GetExternallyUsableCompletionItem(model.SelectedItem);
            if (selectedItem.IsBuilder)
            {
                return char.IsLetterOrDigit(ch);
            }

            var filterText = GetCurrentFilterText(model, selectedItem);

            return GetCompletionRules().IsFilterCharacter(selectedItem, ch, filterText);
        }

        private string GetCurrentFilterText(Model model, CompletionItem selectedItem)
        {
            var textSnapshot = this.TextView.TextSnapshot;
            var viewSpan = model.GetSubjectBufferFilterSpanInViewBuffer(selectedItem.FilterSpan);
            var filterText = model.GetCurrentTextInSnapshot(
                viewSpan, textSnapshot, GetCaretPointInViewBuffer());
            return filterText;
        }

        private void CommitOnTypeChar(char ch)
        {
            AssertIsForeground();

            // Note: this function is called after the character has already been inserted into the
            // buffer.

            var model = sessionOpt.WaitForModel();

            // We only call CommitOnTypeChar if ch was a commit character.  And we only know if ch
            // was commit character if we had a selected item.
            Contract.ThrowIfNull(model);

            // Replace the selected text span with the desired insertion text.  Note: The provided
            // text span will end up including the last typed char (because we track it in an edge
            // inclusive manner).  Because the span includes the last typed character, it will get
            // lost unless we add it back in.  So we actually insert the desired item text *and* the
            // character.  By doing this we also get proper undo behavior.  i.e. if the user types:
            //
            // WriteL(
            //
            // Then we will first input "WriteL(" into the buffer.  We will then replace "WriteL("
            // with "WriteLine(".  That way if they undo, they will end up with "WriteL" again.

            var selectedItem = Controller.GetExternallyUsableCompletionItem(model.SelectedItem);
            var textChange = GetCompletionRules().GetTextChange(selectedItem, ch, GetCurrentFilterText(model, selectedItem));
            this.Commit(selectedItem, new TextChange(textChange.Span, textChange.NewText + ch), model, ch);
        }
    }
}
