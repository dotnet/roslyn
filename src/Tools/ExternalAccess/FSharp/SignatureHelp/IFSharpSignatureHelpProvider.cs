// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.SignatureHelp
{
    internal interface IFSharpSignatureHelpProvider
    {
        /// <summary>
        /// Returns true if the character might trigger completion, 
        /// e.g. '(' and ',' for method invocations 
        /// </summary>
        bool IsTriggerCharacter(char ch);

        /// <summary>
        /// Returns true if the character might end a Signature Help session, 
        /// e.g. ')' for method invocations.  
        /// </summary>
        bool IsRetriggerCharacter(char ch);

        /// <summary>
        /// Returns valid signature help items at the specified position in the document.
        /// </summary>
        Task<FSharpSignatureHelpItems> GetItemsAsync(Document document, int position, FSharpSignatureHelpTriggerInfo triggerInfo, CancellationToken cancellationToken);
    }
}
