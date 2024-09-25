// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers.DeclarationName;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.ExternalAccess.Pythia.Api;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.ExternalAccess.Pythia;

[ExportDeclarationNameRecommender(nameof(PythiaDeclarationNameRecommender)), Shared]
[ExtensionOrder(Before = nameof(DeclarationNameRecommender))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class PythiaDeclarationNameRecommender([Import(AllowDefault = true)] Lazy<IPythiaDeclarationNameRecommenderImplementation>? implementation) : IDeclarationNameRecommender
{
    private readonly Lazy<IPythiaDeclarationNameRecommenderImplementation>? _lazyImplementation = implementation;

    public async Task<ImmutableArray<(string name, Glyph glyph)>> ProvideRecommendedNamesAsync(
        CompletionContext completionContext,
        Document document,
        CSharpSyntaxContext syntaxContext,
        NameDeclarationInfo nameInfo,
        CancellationToken cancellationToken)
    {
        if (_lazyImplementation is null || nameInfo.PossibleSymbolKinds.IsEmpty)
            return [];

        var context = new PythiaDeclarationNameContext(syntaxContext);
        var result = await _lazyImplementation.Value.ProvideRecommendationsAsync(context, cancellationToken).ConfigureAwait(false);

        // We just pick the first possible symbol kind for glyph.
        return result.SelectAsArray(
            name => (name, NameDeclarationInfo.GetGlyph(NameDeclarationInfo.GetSymbolKind(nameInfo.PossibleSymbolKinds[0]), nameInfo.DeclaredAccessibility)));
    }
}
