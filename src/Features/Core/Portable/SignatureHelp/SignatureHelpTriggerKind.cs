// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.SignatureHelp
{
    internal enum SignatureHelpTriggerKind
    {
        /// <summary>
        /// Signature Help was triggered via some other mechanism.
        /// </summary>
        Other,

        /// <summary>
        /// Signature Help was triggered via an action inserting a character into the document.
        /// </summary>
        Insertion,

        /// <summary>
        /// Signature Help is being updated.
        /// </summary>
        Update,
    }
}
