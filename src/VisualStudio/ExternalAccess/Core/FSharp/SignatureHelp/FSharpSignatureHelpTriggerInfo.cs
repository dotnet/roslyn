// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.SignatureHelp;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.SignatureHelp;
#endif

internal readonly struct FSharpSignatureHelpTriggerInfo
{
    public FSharpSignatureHelpTriggerReason TriggerReason { get; }
    public char? TriggerCharacter { get; }

    internal FSharpSignatureHelpTriggerInfo(FSharpSignatureHelpTriggerReason triggerReason, char? triggerCharacter = null)
    {
        Contract.ThrowIfTrue(triggerReason == FSharpSignatureHelpTriggerReason.TypeCharCommand && triggerCharacter == null);
        this.TriggerReason = triggerReason;
        this.TriggerCharacter = triggerCharacter;
    }
}
