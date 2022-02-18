// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers.Snippets;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.CompletionProviders.Snippets
{
    [ExportLanguageService(typeof(ISnippetService), LanguageNames.CSharp), Shared]
    internal class CSharpSnippetService : ISnippetService
    {
        private readonly IEnumerable<Lazy<ISnippetProvider>> _snippetProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpSnippetService([ImportMany] IEnumerable<Lazy<ISnippetProvider, LanguageMetadata>> snippetProvider)
        {
            _snippetProvider = snippetProvider;
        }

        public async Task<TextSpan> GetInvocationSpanAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
            var syntaxContext = (CSharpSyntaxContext)document.GetRequiredLanguageService<ISyntaxContextService>().CreateContext(document, semanticModel, position, cancellationToken);
            return syntaxContext.LeftToken.Span;
        }

        public ISnippetProvider GetSnippetProvider(SnippetData data)
        {
            foreach (var provider in _snippetProvider)
            {
                if (data.DisplayName == provider.Value.GetSnippetText())
                {
                    return provider.Value;
                }
            }

            throw ExceptionUtilities.UnexpectedValue(data.DisplayName);
        }

        public async Task<ImmutableArray<SnippetData?>> GetSnippetsAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var arrayBuilder = ImmutableArray.CreateBuilder<SnippetData?>();
            foreach (var provider in _snippetProvider)
            {
                var snippetData = await provider.Value.GetSnippetDataAsync(document, position, cancellationToken).ConfigureAwait(false);
                if (snippetData is not null)
                {
                    arrayBuilder.Add(snippetData);
                }
            }

            return arrayBuilder.ToImmutable();
        }
    }
}
