// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.SignatureHelp;

/// <summary>
/// A service that is used to determine the appropriate signature help for a position in a document.
/// </summary>
internal abstract class SignatureHelpService : ILanguageService
{
    /// <summary>
    /// Gets the appropriate <see cref="SignatureHelpService"/> for the specified document.
    /// </summary>
    public static SignatureHelpService? GetService(Document? document)
        => document?.GetLanguageService<SignatureHelpService>();

    /// <summary>
    /// Gets the <see cref="ISignatureHelpProvider"/> and <see cref="SignatureHelpItems"/> associated with
    /// the position in the document.
    /// </summary>
    public abstract Task<(ISignatureHelpProvider? provider, SignatureHelpItems? bestItems)> GetSignatureHelpAsync(
        Document document,
        int position,
        SignatureHelpTriggerInfo triggerInfo,
        SignatureHelpOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the <see cref="ISignatureHelpProvider"/> and <see cref="SignatureHelpItems"/> associated with
    /// the position in the document.
    /// </summary>
    public async Task<(ISignatureHelpProvider? provider, SignatureHelpItems? bestItems)> GetSignatureHelpAsync(
        ImmutableArray<ISignatureHelpProvider> providers,
        Document document,
        int position,
        SignatureHelpTriggerInfo triggerInfo,
        SignatureHelpOptions options,
        CancellationToken cancellationToken)
    {
        ISignatureHelpProvider? bestProvider = null;
        SignatureHelpItems? bestItems = null;

        // returns the first non-empty quick info found (based on provider order)
        foreach (var provider in providers)
        {
            var items = await TryGetItemsAsync(document, position, triggerInfo, options, provider, cancellationToken).ConfigureAwait(false);
            if (items is null)
            {
                continue;
            }

            // If another provider provides sig help items, then only take them if they
            // start after the last batch of items.  i.e. we want the set of items that
            // conceptually are closer to where the caret position is.  This way if you have:
            //
            //  Goo(new Bar($$
            //
            // Then invoking sig help will only show the items for "new Bar(" and not also
            // the items for "Goo(..."
            if (bestItems is not null && items.ApplicableSpan.Start < bestItems.ApplicableSpan.Start)
            {
                continue;
            }

            bestProvider = provider;
            bestItems = items;
        }

        return (bestProvider, bestItems);
    }

    private static async Task<SignatureHelpItems?> TryGetItemsAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, SignatureHelpOptions options, ISignatureHelpProvider provider, CancellationToken cancellationToken)
    {
        // We're calling into extensions, we need to make ourselves resilient
        // to the extension crashing.
        try
        {
            var items = await provider.GetItemsAsync(document, position, triggerInfo, options, cancellationToken).ConfigureAwait(false);
            if (items is null)
            {
                return null;
            }

            if (!items.ApplicableSpan.IntersectsWith(position))
            {
                return null;
            }

            return items;
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
        {
            return null;
        }
    }
}
