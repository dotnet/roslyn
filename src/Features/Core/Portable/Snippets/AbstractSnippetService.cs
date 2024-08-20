// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
    public ImmutableArray<SnippetData> GetSnippets(SnippetContext context, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<SnippetData>.GetInstance(out var arrayBuilder);
        EnsureSnippetsLoaded(context.Document.Project.Language);
        foreach (var provider in _snippetProviders)
        {
            if (provider.IsValidSnippetLocation(context, cancellationToken))
                arrayBuilder.Add(new(provider.Identifier, provider.Description, provider.AdditionalFilterTexts));
        }

        return arrayBuilder.ToImmutableAndClear();
    }

    internal void EnsureSnippetsLoaded(string language)
    {
        lock (_snippetProvidersLock)
        {
            if (_snippetProviders.IsDefault)
            {
                using var _ = ArrayBuilder<ISnippetProvider>.GetInstance(out var arrayBuilder);
                foreach (var provider in _lazySnippetProviders.Where(p => p.Metadata.Language == language))
                {
                    var providerData = provider.Value;
                    Debug.Assert(!_identifierToProviderMap.TryGetValue(providerData.Identifier, out var _));
                    _identifierToProviderMap.Add(providerData.Identifier, providerData);
                    arrayBuilder.Add(providerData);
                }

                _snippetProviders = arrayBuilder.ToImmutable();
            }
        }
    }
}
