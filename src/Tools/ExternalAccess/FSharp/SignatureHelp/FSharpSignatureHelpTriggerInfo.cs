// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.SignatureHelp
{
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
}
