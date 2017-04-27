// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.SignatureHelp;
using VSInvokeSignatureHelpCommandArgs = Microsoft.VisualStudio.Text.UI.Commanding.Commands.InvokeSignatureHelpCommandArgs;
using VSCommandState = Microsoft.VisualStudio.Text.UI.Commanding.CommandState;
using VSC = Microsoft.VisualStudio.Text.UI.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp
{
    internal partial class Controller
    {
        VSCommandState VSC.ICommandHandler<VSInvokeSignatureHelpCommandArgs>.GetCommandState(VSInvokeSignatureHelpCommandArgs args)
        {
            AssertIsForeground();
            return VSCommandState.CommandIsUnavailable;
        }

        bool VSC.ICommandHandler<VSInvokeSignatureHelpCommandArgs>.ExecuteCommand(VSInvokeSignatureHelpCommandArgs args)
        {
            AssertIsForeground();
            DismissSessionIfActive();

            var providers = GetProviders();
            if (providers == null)
            {
                return false;
            }

            this.StartSession(providers, new SignatureHelpTriggerInfo(SignatureHelpTriggerReason.InvokeSignatureHelpCommand));
            return true;
        }
    }
}
