// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.SignatureHelp;
using Microsoft.CodeAnalysis.SignatureHelp;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.SignatureHelp
{
    internal static class FSharpSignatureHelpTriggerReasonHelpers
    {
        public static FSharpSignatureHelpTriggerReason ConvertFrom(SignatureHelpTriggerReason triggerReason)
        {
            switch (triggerReason)
            {
                case SignatureHelpTriggerReason.InvokeSignatureHelpCommand:
                    {
                        return FSharpSignatureHelpTriggerReason.InvokeSignatureHelpCommand;
                    }

                case SignatureHelpTriggerReason.RetriggerCommand:
                    {
                        return FSharpSignatureHelpTriggerReason.RetriggerCommand;
                    }

                case SignatureHelpTriggerReason.TypeCharCommand:
                    {
                        return FSharpSignatureHelpTriggerReason.TypeCharCommand;
                    }

                default:
                    {
                        throw ExceptionUtilities.UnexpectedValue(triggerReason);
                    }
            }
        }
    }
}
