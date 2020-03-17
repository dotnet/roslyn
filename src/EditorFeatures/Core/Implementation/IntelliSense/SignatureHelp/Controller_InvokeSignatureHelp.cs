// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp
{
    internal partial class Controller
    {
        VSCommanding.CommandState IChainedCommandHandler<InvokeSignatureHelpCommandArgs>.GetCommandState(InvokeSignatureHelpCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            AssertIsForeground();
            return nextHandler();
        }

        void IChainedCommandHandler<InvokeSignatureHelpCommandArgs>.ExecuteCommand(InvokeSignatureHelpCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            AssertIsForeground();
            DismissSessionIfActive();

            var providers = GetProviders();
            if (providers == null)
            {
                return;
            }

            // Dismiss any completion sessions when Signature Help is explicitly invoked. The
            // primary motivation for this behavior is when both Signature Help and Completion
            // are activated at the same time, for example in argument lists.
            _completionBroker.GetSession(args.TextView)?.Dismiss();

            this.StartSession(providers, new SignatureHelpTriggerInfo(SignatureHelpTriggerReason.InvokeSignatureHelpCommand));
        }
    }
}
