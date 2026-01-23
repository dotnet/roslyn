// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.SignatureHelp;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.SignatureHelp;
#endif

internal abstract class AbstractFSharpSignatureHelpProvider
{
    /// <summary>
    /// The set of characters that might trigger a Signature Help session,
    /// e.g. '(' and ',' for method invocations 
    /// </summary>
    public abstract ImmutableArray<char> TriggerCharacters { get; }

    /// <summary>
    /// The set of characters that might end a Signature Help session,
    /// e.g. ')' for method invocations.  
    /// </summary>
    public abstract ImmutableArray<char> RetriggerCharacters { get; }

    /// <summary>
    /// Returns valid signature help items at the specified position in the document.
    /// </summary>
    public abstract Task<FSharpSignatureHelpItems> GetItemsAsync(Document document, int position, FSharpSignatureHelpTriggerInfo triggerInfo, CancellationToken cancellationToken);
}
