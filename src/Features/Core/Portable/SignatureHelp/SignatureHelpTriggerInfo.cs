// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SignatureHelp
{
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
}
