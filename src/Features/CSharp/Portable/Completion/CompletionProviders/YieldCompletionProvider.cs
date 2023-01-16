// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(YieldCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(KeywordCompletionProvider))]
    [Shared]
    internal sealed class YieldCompletionProvider : LSPCompletionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public YieldCompletionProvider()
        {
        }

        public override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.CommonTriggerCharacters.Add(' ');
        internal override string Language => LanguageNames.CSharp;

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxFacts.IsInNonUserCode(syntaxTree, position, cancellationToken))
                return;

            var syntaxContext = await context.GetSyntaxContextWithExistingSpeculativeModelAsync(document, cancellationToken).ConfigureAwait(false);

            if (IsYieldCompletionsContext(syntaxContext))
                context.AddItems(CreateYieldCompletionItems());
        }

        // Make private if single `yield` keyword completion is decided to be removed
        internal static bool IsYieldCompletionsContext(SyntaxContext syntaxContext)
            => syntaxContext.IsStatementContext;

        private static IEnumerable<CompletionItem> CreateYieldCompletionItems()
        {
            yield return CommonCompletionItem.Create(
                displayText: "yield return",
                displayTextSuffix: "",
                sortText: "yield1", // Hack to ensure `yield return` comes before `yield break` as more commonly used one
                filterText: "yieldReturn", // Uppercase R and no space to select "yield return" if "yr" is written.
                glyph: Glyph.Keyword,
                rules: CompletionItemRules.Default);
            yield return CommonCompletionItem.Create(
                displayText: "yield break",
                displayTextSuffix: "",
                sortText: "yield2", // Hack to ensure `yield break` comes after `yield return` as less commonly used one
                filterText: "yieldBreak", // Uppercase B and no space to select "yield break" if "yb" is written.
                glyph: Glyph.Keyword,
                rules: CompletionItemRules.Default);
        }
    }
}
