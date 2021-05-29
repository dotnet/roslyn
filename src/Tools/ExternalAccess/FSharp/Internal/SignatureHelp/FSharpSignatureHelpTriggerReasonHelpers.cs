// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
