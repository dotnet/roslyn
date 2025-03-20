// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp
{
    internal partial class Controller
    {
        CommandState IChainedCommandHandler<TypeCharCommandArgs>.GetCommandState(TypeCharCommandArgs args, Func<CommandState> nextHandler)
        {
            this.ThreadingContext.ThrowIfNotOnUIThread();

            // We just defer to the editor here.  We do not interfere with typing normal characters.
            return nextHandler();
        }

        void IChainedCommandHandler<TypeCharCommandArgs>.ExecuteCommand(TypeCharCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            this.ThreadingContext.ThrowIfNotOnUIThread();

            var allProviders = GetProviders();
            if (allProviders == null)
            {
                nextHandler();
                return;
            }

            // Note: while we're doing this, we don't want to hear about buffer changes (since we
            // know they're going to happen).  So we disconnect and reconnect to the event
            // afterwards.  That way we can hear about changes to the buffer that don't happen
            // through us.
            this.TextView.TextBuffer.PostChanged -= OnTextViewBufferPostChanged;
            try
            {
                nextHandler();
            }
            finally
            {
                this.TextView.TextBuffer.PostChanged += OnTextViewBufferPostChanged;
            }

            // We only want to process typechar if it is a normal typechar and no one else is
            // involved.  i.e. if there was a typechar, but someone processed it and moved the caret
            // somewhere else then we don't want signature help.  Also, if a character was typed but
            // something intercepted and placed different text into the editor, then we don't want
            // to proceed. 
            //
            // Note: we do not want to pass along a text version here.  It is expected that multiple
            // version changes may happen when we call 'nextHandler' and we will still want to
            // proceed.  For example, if the user types "WriteL(", then that will involve two text
            // changes as completion commits that out to "WriteLine(".  But we still want to provide
            // sig help in this case.
            if (this.TextView.TypeCharWasHandledStrangely(this.SubjectBuffer, args.TypedChar))
            {
                // If we were computing anything, we stop.  We only want to process a typechar
                // if it was a normal character.
                DismissSessionIfActive();
                return;
            }

            // Separate the sig help providers into two buckets; one bucket for those that were triggered
            // by the typed character, and those that weren't.  To keep our queries to a minimum, we first
            // check with the textually triggered providers.  If none of those produced any sig help items
            // then we query the other providers to see if they can produce anything viable.  This takes
            // care of cases where the filtered set of providers didn't provide anything but one of the
            // other providers could still be valid, but doesn't explicitly treat the typed character as
            // a trigger character.
            var (textuallyTriggeredProviders, untriggeredProviders) = FilterProviders(allProviders, args.TypedChar);
            var triggerInfo = new SignatureHelpTriggerInfo(SignatureHelpTriggerReason.TypeCharCommand, args.TypedChar);

            if (!IsSessionActive)
            {
                // No computation at all.  If this is not a trigger character, we just ignore it and
                // stay in this state.  Otherwise, if it's a trigger character, start up a new
                // computation and start computing the model in the background.
                if (textuallyTriggeredProviders.Any())
                {
                    // First create the session that represents that we now have a potential 
                    // signature help list. Then tell it to start computing.
                    StartSession(textuallyTriggeredProviders, triggerInfo);
                    return;
                }
                else
                {
                    // No need to do anything.  Just stay in the state where we have no session.
                    return;
                }
            }
            else
            {
                var computed = false;
                if (allProviders.Any(static (p, args) => p.IsRetriggerCharacter(args.TypedChar), args))
                {
                    // The user typed a character that might close the scope of the current model.
                    // In this case, we should requery all providers.
                    //
                    // e.g.     Math.Max(Math.Min(1,2)$$
                    sessionOpt.ComputeModel(allProviders, new SignatureHelpTriggerInfo(SignatureHelpTriggerReason.RetriggerCommand, triggerInfo.TriggerCharacter));
                    computed = true;
                }

                if (textuallyTriggeredProviders.Any())
                {
                    // The character typed was something like "(".  It can both filter a list if
                    // it was in a string like: Goo(bar, "(
                    //
                    // Or it can trigger a new list. Ask the computation to compute again.
                    sessionOpt.ComputeModel(
                        textuallyTriggeredProviders.Concat(untriggeredProviders), triggerInfo);
                    computed = true;
                }

                if (!computed)
                {
                    // A character was typed and we haven't updated our model; do so now.
                    sessionOpt.ComputeModel(allProviders, new SignatureHelpTriggerInfo(SignatureHelpTriggerReason.RetriggerCommand));
                }
            }
        }

        private (ImmutableArray<ISignatureHelpProvider> matched, ImmutableArray<ISignatureHelpProvider> unmatched) FilterProviders(
            ImmutableArray<ISignatureHelpProvider> providers, char ch)
        {
            this.ThreadingContext.ThrowIfNotOnUIThread();

            using var matchedProvidersDisposer = ArrayBuilder<ISignatureHelpProvider>.GetInstance(out var matchedProviders);
            using var unmatchedProvidersDisposer = ArrayBuilder<ISignatureHelpProvider>.GetInstance(out var unmatchedProviders);
            foreach (var provider in providers)
            {
                if (provider.IsTriggerCharacter(ch))
                {
                    matchedProviders.Add(provider);
                }
                else
                {
                    unmatchedProviders.Add(provider);
                }
            }

            return (matchedProviders.ToImmutable(), unmatchedProviders.ToImmutable());
        }
    }
}
