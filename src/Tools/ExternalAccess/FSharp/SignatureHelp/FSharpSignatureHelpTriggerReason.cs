// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.SignatureHelp
{
    internal enum FSharpSignatureHelpTriggerReason
    {
        /// <summary>
        /// Signature Help was triggered through the 'Invoke Signature Help' command
        /// </summary>
        InvokeSignatureHelpCommand,

        /// <summary>
        /// Signature Help was triggered through the 'Type Char' command.
        /// </summary>
        TypeCharCommand,

        /// <summary>
        /// Signature Help was triggered through typing a closing brace.
        /// </summary>
        RetriggerCommand,
    }
}
