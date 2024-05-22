// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SignatureHelp;

/// <summary>
/// A service that is used to determine the appropriate signature help for a position in a document.
/// </summary>
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
[Export(typeof(ISignatureHelpService)), Shared]
internal sealed class SignatureHelpService([ImportMany] IEnumerable<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> allProviders)
    : ISignatureHelpService
{
    private readonly ConcurrentDictionary<string, ImmutableArray<ISignatureHelpProvider>> _providersByLanguage = [];
    private readonly IEnumerable<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> _allProviders = allProviders;

    private ImmutableArray<ISignatureHelpProvider> GetProviders(string language)
    {
        return _providersByLanguage.GetOrAdd(language, language
            => _allProviders.Where(p => p.Metadata.Language == language)
                .SelectAsArray(p => p.Value));
    }

    /// <summary>
    /// Gets the <see cref="ISignatureHelpProvider"/> and <see cref="SignatureHelpItems"/> associated with
    /// the position in the document.
    /// </summary>
    public Task<(ISignatureHelpProvider? provider, SignatureHelpItems? bestItems)> GetSignatureHelpAsync(
        Document document,
        int position,
        SignatureHelpTriggerInfo triggerInfo,
        SignatureHelpOptions options,
        CancellationToken cancellationToken = default)
    {
        return GetSignatureHelpAsync(
            GetProviders(document.Project.Language),
            document,
            position,
            triggerInfo,
            options,
            cancellationToken);
    }

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
