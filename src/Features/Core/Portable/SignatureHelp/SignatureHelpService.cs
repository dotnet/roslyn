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
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SignatureHelp;

/// <summary>
/// A service that is used to determine the appropriate signature help for a position in a document.
/// </summary>
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
[Export(typeof(SignatureHelpService)), Shared]
internal sealed class SignatureHelpService([ImportMany] IEnumerable<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> allProviders)
{
    private readonly ConcurrentDictionary<string, ImmutableArray<ISignatureHelpProvider>> _providersByLanguage = [];
    private readonly IEnumerable<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> _allProviders = allProviders;

    private ImmutableArray<ISignatureHelpProvider> GetProviders(string language)
    {
        return _providersByLanguage.GetOrAdd(language, language =>
            _allProviders
                .Where(p => p.Metadata.Language == language)
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
        CancellationToken cancellationToken)
    {
        return GetSignatureHelpAsync(
            GetProviders(document.Project.Language),
            document,
            position,
            triggerInfo,
            cancellationToken);
    }

    /// <summary>
    /// Gets the <see cref="ISignatureHelpProvider"/> and <see cref="SignatureHelpItems"/> associated with
    /// the position in the document.
    /// </summary>
    public static async Task<(ISignatureHelpProvider? provider, SignatureHelpItems? bestItems)> GetSignatureHelpAsync(
        ImmutableArray<ISignatureHelpProvider> providers,
        Document document,
        int position,
        SignatureHelpTriggerInfo triggerInfo,
        CancellationToken cancellationToken)
    {
        var extensionManager = document.Project.Solution.Services.GetRequiredService<IExtensionManager>();

        var options = await document.GetMemberDisplayOptionsAsync(cancellationToken).ConfigureAwait(false);

        ISignatureHelpProvider? bestProvider = null;
        SignatureHelpItems? bestItems = null;

        // returns the first non-empty quick info found (based on provider order)
        foreach (var provider in providers)
        {
            var items = await extensionManager.PerformFunctionAsync(
                provider,
                cancellationToken => provider.GetItemsAsync(document, position, triggerInfo, options, cancellationToken),
                defaultValue: null,
                cancellationToken).ConfigureAwait(false);

            if (items is null || !items.ApplicableSpan.IntersectsWith(position))
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
}
