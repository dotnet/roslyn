// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal abstract class AbstractYieldCompletionProvider(string yieldKeyword, string tooltip) : AbstractAsyncSupportingCompletionProvider
{
    protected abstract bool IsYieldKeywordContext(SyntaxContext syntaxContext);

    public sealed override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        var document = context.Document;
        var position = context.Position;
        var cancellationToken = context.CancellationToken;
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        if (syntaxFacts.IsInNonUserCode(syntaxTree, position, cancellationToken))
            return;

        var syntaxContext = await context.GetSyntaxContextWithExistingSpeculativeModelAsync(document, cancellationToken).ConfigureAwait(false);

        if (!IsYieldKeywordContext(syntaxContext))
            return;

        var leftToken = syntaxContext.LeftToken;
        var declaration = GetAsyncSupportingDeclaration(leftToken, position);

        if (declaration == null)
            return;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (!IsValidContext(declaration, semanticModel, cancellationToken))
            return;

        using var builder = TemporaryArray<KeyValuePair<string, string>>.Empty;

        builder.Add(KeyValuePair.Create(Position, position.ToString()));
        builder.Add(KeyValuePair.Create(LeftTokenPosition, leftToken.SpanStart.ToString()));

        var addModifiers = declaration is not null && ShouldAddModifiers(syntaxContext, declaration, semanticModel, cancellationToken);
        if (addModifiers)
            builder.Add(KeyValuePair.Create(AddModifiers, string.Empty));

        var properties = builder.ToImmutableAndClear();

        context.AddItem(CommonCompletionItem.Create(
            displayText: yieldKeyword,
            displayTextSuffix: "",
            filterText: yieldKeyword,
            rules: CompletionItemRules.Default,
            glyph: Glyph.Keyword,
            description: RecommendedKeyword.CreateDisplayParts(yieldKeyword, tooltip),
            isComplexTextEdit: addModifiers,
            properties: properties));
    }

    protected virtual bool IsValidContext(SyntaxNode declaration, SemanticModel semanticModel, CancellationToken cancellationToken)
        => true;

    protected abstract bool ShouldAddModifiers(SyntaxContext syntaxContext, SyntaxNode declaration, SemanticModel semanticModel, CancellationToken cancellationToken);
}
