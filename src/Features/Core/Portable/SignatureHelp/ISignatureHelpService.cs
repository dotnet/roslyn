// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.SignatureHelp;

/// <summary>
/// A service that is used to determine the appropriate signature help for a position in a document.
/// </summary>
internal interface ISignatureHelpService
{
    /// <summary>
    /// Gets the <see cref="ISignatureHelpProvider"/> and <see cref="SignatureHelpItems"/> associated with
    /// the position in the document.
    /// </summary>
    public Task<(ISignatureHelpProvider? provider, SignatureHelpItems? bestItems)> GetSignatureHelpAsync(
        Document document,
        int position,
        SignatureHelpTriggerInfo triggerInfo,
        SignatureHelpOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the <see cref="ISignatureHelpProvider"/> and <see cref="SignatureHelpItems"/> associated with
    /// the position in the document.
    /// </summary>
    public Task<(ISignatureHelpProvider? provider, SignatureHelpItems? bestItems)> GetSignatureHelpAsync(
        ImmutableArray<ISignatureHelpProvider> providers,
        Document document,
        int position,
        SignatureHelpTriggerInfo triggerInfo,
        SignatureHelpOptions options,
        CancellationToken cancellationToken);
}
