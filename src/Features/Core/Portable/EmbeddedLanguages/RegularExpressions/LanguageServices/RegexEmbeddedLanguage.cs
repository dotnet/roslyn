// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions.LanguageServices;

internal sealed class RegexEmbeddedLanguage : IEmbeddedLanguage
{
    public readonly EmbeddedLanguageInfo Info;

    private readonly AbstractEmbeddedLanguagesProvider _provider;

    public EmbeddedLanguageCompletionProvider CompletionProvider { get; }

    public RegexEmbeddedLanguage(
        AbstractEmbeddedLanguagesProvider provider,
        EmbeddedLanguageInfo info)
    {
        Info = info;

        _provider = provider;

        CompletionProvider = new RegexEmbeddedCompletionProvider(this);
    }

    internal async Task<(RegexTree tree, SyntaxToken token)> TryGetTreeAndTokenAtPositionAsync(
        Document document, int position, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var token = root.FindToken(position);

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var detector = RegexLanguageDetector.GetOrCreate(semanticModel.Compilation, this.Info);
        var tree = detector.TryParseString(token, semanticModel, cancellationToken);
        return tree == null ? default : (tree, token);
    }

    public string EscapeText(string text, SyntaxToken token)
        => _provider.EscapeText(text, token);
}
