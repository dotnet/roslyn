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
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.CompletionProviders.Snippets
{
    [ExportCompletionProvider(nameof(IfSnippetCompletionProvider), LanguageNames.CSharp)]
    [Shared]
    internal sealed class IfSnippetCompletionProvider : ControlStructureCompletionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public IfSnippetCompletionProvider()
        {
        }

        internal override string Language => LanguageNames.CSharp;

        public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey = null, CancellationToken cancellationToken = default)
        {
            if (!item.Properties.TryGetValue("InsertionText", out var snippetText))
            {
                return await base.GetChangeAsync(document, item, commitKey, cancellationToken).ConfigureAwait(false);
            }

            return CompletionChange.Create(new TextChange(item.Span, snippetText));
        }

        protected override IEnumerable<CompletionItem> GetCompletionItems(SyntaxToken token, SyntaxGenerator generator, ISyntaxKindsService syntaxKinds, ISyntaxFactsService syntaxFacts)
        {
            var insertionText = string.Format("if (true)\n{{\n}}");

            yield return CommonCompletionItem.Create(
                displayText: "Add if statement",
                displayTextSuffix: "",
                rules: CompletionItemRules.Default,
                Glyph.Snippet,
                inlineDescription: "",
                properties: ImmutableDictionary.Create<string, string>().Add("InsertionText", insertionText),
                isComplexTextEdit: false);

            yield return CommonCompletionItem.Create(
                displayText: "while loop",
                displayTextSuffix: "",
                rules: CompletionItemRules.Default,
                Glyph.Snippet,
                inlineDescription: "Add while loop",
                properties: ImmutableDictionary.Create<string, string>().Add("InsertionText", "while"),
                isComplexTextEdit: false);

            yield return CommonCompletionItem.Create(
                displayText: "do loop",
                displayTextSuffix: "",
                rules: CompletionItemRules.Default,
                Glyph.Snippet,
                inlineDescription: "Add do..while loop",
                properties: ImmutableDictionary.Create<string, string>().Add("InsertionText", "do"),
                isComplexTextEdit: false);

            yield return CommonCompletionItem.Create(
                displayText: "else",
                displayTextSuffix: "",
                rules: CompletionItemRules.Default,
                Glyph.Snippet,
                inlineDescription: "Add else statment",
                properties: ImmutableDictionary.Create<string, string>().Add("InsertionText", "else"),
                isComplexTextEdit: false);

            yield return CommonCompletionItem.Create(
                displayText: "for loop",
                displayTextSuffix: "",
                rules: CompletionItemRules.Default,
                Glyph.Snippet,
                inlineDescription: "Add for loop",
                properties: ImmutableDictionary.Create<string, string>().Add("InsertionText", "for"),
                isComplexTextEdit: false);

            yield return CommonCompletionItem.Create(
                displayText: "reverse for loop",
                displayTextSuffix: "",
                rules: CompletionItemRules.Default,
                Glyph.Snippet,
                inlineDescription: "Add reverse for loop",
                properties: ImmutableDictionary.Create<string, string>().Add("InsertionText", "forr"),
                isComplexTextEdit: false);

            yield return CommonCompletionItem.Create(
                displayText: "lock",
                displayTextSuffix: "",
                rules: CompletionItemRules.Default,
                Glyph.Snippet,
                inlineDescription: "Add lock statment",
                properties: ImmutableDictionary.Create<string, string>().Add("InsertionText", "lock"),
                isComplexTextEdit: false);

            yield return CommonCompletionItem.Create(
                displayText: "try catch",
                displayTextSuffix: "",
                rules: CompletionItemRules.Default,
                Glyph.Snippet,
                inlineDescription: "Add try catch statment",
                properties: ImmutableDictionary.Create<string, string>().Add("InsertionText", "try"),
                isComplexTextEdit: false);

            yield return CommonCompletionItem.Create(
                displayText: "try finally",
                displayTextSuffix: "",
                rules: CompletionItemRules.Default,
                Glyph.Snippet,
                inlineDescription: "Add try finally statment",
                properties: ImmutableDictionary.Create<string, string>().Add("InsertionText", "tryf"),
                isComplexTextEdit: false);

            yield return CommonCompletionItem.Create(
                displayText: "using",
                displayTextSuffix: "",
                rules: CompletionItemRules.Default,
                Glyph.Snippet,
                inlineDescription: "Add using statment",
                properties: ImmutableDictionary.Create<string, string>().Add("InsertionText", "using"),
                isComplexTextEdit: false);
        }
    }
}
