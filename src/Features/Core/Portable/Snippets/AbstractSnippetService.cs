﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Snippets
{
    internal abstract class AbstractSnippetService : ISnippetService
    {
        private readonly ImmutableArray<Lazy<ISnippetProvider, LanguageMetadata>> _lazySnippetProviders;
        private readonly Dictionary<string, ISnippetProvider> _identifierToProviderMap = new();
        private readonly object _snippetProvidersLock = new();
        private ImmutableArray<AbstractSnippetProvider> _snippetProviders;

        public AbstractSnippetService(IEnumerable<Lazy<ISnippetProvider, LanguageMetadata>> lazySnippetProviders)
        {
            _lazySnippetProviders = lazySnippetProviders.ToImmutableArray();
        }

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
        public async Task<ImmutableArray<SnippetData>> GetSnippetsAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxFacts.IsInNonUserCode(syntaxTree, position, cancellationToken))
            {
                return ImmutableArray<SnippetData>.Empty;
            }

            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
            var context = document.GetRequiredLanguageService<ISyntaxContextService>().CreateContext(document, semanticModel, position, cancellationToken);

            using var _ = ArrayBuilder<SnippetData>.GetInstance(out var arrayBuilder);
            foreach (var provider in GetSnippetProviders(document))
            {
                var snippetData = provider.GetSnippetData(context, cancellationToken);
                arrayBuilder.AddIfNotNull(snippetData);
            }

            return arrayBuilder.ToImmutable();
        }

        private ImmutableArray<AbstractSnippetProvider> GetSnippetProviders(Document document)
        {
            lock (_snippetProvidersLock)
            {
                if (_snippetProviders.IsDefault)
                {
                    using var _ = ArrayBuilder<AbstractSnippetProvider>.GetInstance(out var arrayBuilder);
                    Debug.Assert(document.Project.Language is LanguageNames.CSharp or LanguageNames.VisualBasic);
                    foreach (var provider in _lazySnippetProviders.Where(p => p.Metadata.Language == document.Project.Language))
                    {
                        var providerData = provider.Value;
                        Debug.Assert(!_identifierToProviderMap.TryGetValue(providerData.Identifier, out var _));
                        Debug.Assert(providerData is AbstractSnippetProvider);
                        var abstractSnippetProvider = (AbstractSnippetProvider)providerData;
                        _identifierToProviderMap.Add(providerData.Identifier, abstractSnippetProvider);
                        arrayBuilder.Add(abstractSnippetProvider);
                    }

                    _snippetProviders = arrayBuilder.ToImmutable();
                }
            }

            return _snippetProviders;
        }
    }
}
