// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.DateAndTime.LanguageServices;

internal class DateAndTimeEmbeddedLanguage : IEmbeddedLanguage
{
    public readonly EmbeddedLanguageInfo Info;

    public DateAndTimeEmbeddedLanguage(EmbeddedLanguageInfo info)
    {
        Info = info;
        CompletionProvider = new DateAndTimeEmbeddedCompletionProvider(this);
    }

    public EmbeddedLanguageCompletionProvider CompletionProvider { get; }

    public async Task<SyntaxToken?> TryGetDateAndTimeTokenAtPositionAsync(
        Document document, int position, CancellationToken cancellationToken)
    {
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var token = GetToken(syntaxFacts, root, position);

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var detector = DateAndTimeLanguageDetector.GetOrCreate(semanticModel.Compilation, this.Info);
        return detector.TryParseString(token, semanticModel, cancellationToken) != null
            ? token : null;
    }

    private static SyntaxToken GetToken(ISyntaxFactsService syntaxFacts, SyntaxNode root, int position)
    {
        var token = root.FindToken(position);
        var syntaxKinds = syntaxFacts.SyntaxKinds;

        if (token.RawKind == syntaxKinds.CloseBraceToken)
        {
            // Might be here:    $"Date is: {date:$$}" or
            //                   $"Date is: {date:G$$}"
            //
            // If so, we want to return the InterpolatedStringTextToken following the `:`
            var previous = token.GetPreviousToken();
            if (previous.RawKind == syntaxKinds.InterpolatedStringTextToken)
                return previous;

            if (previous.RawKind == syntaxKinds.ColonToken)
                return previous.GetNextToken(includeZeroWidth: true);
        }

        return token;
    }
}
