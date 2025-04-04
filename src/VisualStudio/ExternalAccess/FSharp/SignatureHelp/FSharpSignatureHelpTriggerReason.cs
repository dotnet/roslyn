﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.SignatureHelp;

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
