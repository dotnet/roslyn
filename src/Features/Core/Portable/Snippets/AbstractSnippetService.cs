// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Snippets;

internal abstract class AbstractSnippetService(IEnumerable<Lazy<ISnippetProvider, LanguageMetadata>> lazySnippetProviders) : ISnippetService
{
    private readonly ImmutableArray<Lazy<ISnippetProvider, LanguageMetadata>> _lazySnippetProviders = lazySnippetProviders.ToImmutableArray();
    private readonly Dictionary<string, ISnippetProvider> _identifierToProviderMap = [];
    private readonly object _snippetProvidersLock = new();
    private ImmutableArray<ISnippetProvider> _snippetProviders;

    /// <summary>
    /// This should never be called prior to GetSnippetsAsync because it gets populated
    /// at that point in time.
    /// </summary>
    public ISnippetProvider GetSnippetProvider(string snippetIdentifier)
    {
        Contract.ThrowIfFalse(_identifierToProviderMap.ContainsKey(snippetIdentifier));
        return _identifierToProviderMap[snippetIdentifier];
    }

    /// <summary>
    /// Iterates through all providers and determines if the snippet 
    /// can be added to the Completion list at the corresponding position.
    /// </summary>
    public async Task<ImmutableArray<SnippetData>> GetSnippetsAsync(SnippetContext context, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<SnippetData>.GetInstance(out var arrayBuilder);
        foreach (var provider in GetSnippetProviders(context.Document))
        {
            var snippetData = await provider.GetSnippetDataAsync(context, cancellationToken).ConfigureAwait(false);
            arrayBuilder.AddIfNotNull(snippetData);
        }

        return arrayBuilder.ToImmutable();
    }

    private ImmutableArray<ISnippetProvider> GetSnippetProviders(Document document)
    {
        lock (_snippetProvidersLock)
        {
            if (_snippetProviders.IsDefault)
            {
                using var _ = ArrayBuilder<ISnippetProvider>.GetInstance(out var arrayBuilder);
                foreach (var provider in _lazySnippetProviders.Where(p => p.Metadata.Language == document.Project.Language))
                {
                    var providerData = provider.Value;
                    Debug.Assert(!_identifierToProviderMap.TryGetValue(providerData.Identifier, out var _));
                    _identifierToProviderMap.Add(providerData.Identifier, providerData);
                    arrayBuilder.Add(providerData);
                }

                _snippetProviders = arrayBuilder.ToImmutable();
            }
        }

        return _snippetProviders;
    }
}
