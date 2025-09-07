// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp;

internal sealed partial class Controller
{
    CommandState IChainedCommandHandler<InvokeSignatureHelpCommandArgs>.GetCommandState(InvokeSignatureHelpCommandArgs args, Func<CommandState> nextHandler)
    {
        this.ThreadingContext.ThrowIfNotOnUIThread();
        return nextHandler();
    }

    void IChainedCommandHandler<InvokeSignatureHelpCommandArgs>.ExecuteCommand(InvokeSignatureHelpCommandArgs args, Action nextHandler, CommandExecutionContext context)
    {
        this.ThreadingContext.ThrowIfNotOnUIThread();
        DismissSessionIfActive();

        var providers = GetProviders();
        if (providers == null)
        {
            return;
        }

        // Dismiss any Completion sessions when Signature Help is explicitly invoked.
        // There are cases when both show up implicitly, for example in argument lists
        // when the user types the `(`. If both are showing and the user explicitly
        // invokes Signature Help, they are requesting that the Signature Help control 
        // be the focused one. Closing an existing Completion session accomplishes this.
        _completionBroker.GetSession(args.TextView)?.Dismiss();

        this.StartSession(providers, new SignatureHelpTriggerInfo(SignatureHelpTriggerReason.InvokeSignatureHelpCommand));
    }
}
