// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Snippets
{
    [ExportLanguageService(typeof(ISnippetService), LanguageNames.CSharp), Shared]
    internal class CSharpSnippetService : ISnippetService
    {
        private readonly IEnumerable<Lazy<ISnippetProvider, LanguageMetadata>> _snippetProvider;
        private readonly Lazy<Dictionary<string, ISnippetProvider>> _snippetProviderDictionary;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpSnippetService([ImportMany] IEnumerable<Lazy<ISnippetProvider, LanguageMetadata>> snippetProvider)
        {
            _snippetProvider = snippetProvider;
            _snippetProviderDictionary = new Lazy<Dictionary<string, ISnippetProvider>>(()
                => _snippetProvider.ToDictionary(provider => provider.Value.SnippetIdentifier, provider => provider.Value));
        }

        public ISnippetProvider GetSnippetProvider(string snippetIdentifier)
        {
            return _snippetProviderDictionary.Value[snippetIdentifier];
        }

        /// <summary>
        /// Iterates through all providers and determines if the snippet 
        /// can be added to the Completion list at the corresponding position.
        /// </summary>
        public async Task<ImmutableArray<SnippetData>> GetSnippetsAsync(Document document, int position, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<SnippetData>.GetInstance(out var arrayBuilder);
            foreach (var provider in _snippetProvider.Where(b => b.Metadata.Language == document.Project.Language))
            {
                var snippetData = await provider.Value.GetSnippetDataAsync(document, position, cancellationToken).ConfigureAwait(false);
                arrayBuilder.AddIfNotNull(snippetData);
            }

            return arrayBuilder.ToImmutable();
        }
    }
}
