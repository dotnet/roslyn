// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.SignatureHelp;

internal interface ISignatureHelpProvider
{
    /// <summary>
    /// The set of characters that might trigger a Signature Help session,
    /// e.g. '(' and ',' for method invocations 
    /// </summary>
    ImmutableArray<char> TriggerCharacters { get; }

    /// <summary>
    /// The set of characters that might end a Signature Help session,
    /// e.g. ')' for method invocations.  
    /// </summary>
    ImmutableArray<char> RetriggerCharacters { get; }

    /// <summary>
    /// Returns valid signature help items at the specified position in the document.
    /// </summary>
    Task<SignatureHelpItems?> GetItemsAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, MemberDisplayOptions options, CancellationToken cancellationToken);
}
