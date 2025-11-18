// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.SignatureHelp;

internal readonly struct SignatureHelpTriggerInfo
{
    public SignatureHelpTriggerReason TriggerReason { get; }
    public char? TriggerCharacter { get; }

    internal SignatureHelpTriggerInfo(SignatureHelpTriggerReason triggerReason, char? triggerCharacter = null)
        : this()
    {
        Contract.ThrowIfTrue(triggerReason == SignatureHelpTriggerReason.TypeCharCommand && triggerCharacter == null);
        TriggerReason = triggerReason;
        TriggerCharacter = triggerCharacter;
    }
}
