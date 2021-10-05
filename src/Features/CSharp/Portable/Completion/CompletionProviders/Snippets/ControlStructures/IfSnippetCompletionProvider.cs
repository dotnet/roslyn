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

namespace Microsoft.CodeAnalysis.CSharp.Completion.CompletionProviders.Snippets.ControlStructures
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

        public override async Task<TextChange?> GetTextChangeAsync(Document document, CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
        {
            if (selectedItem.Properties.TryGetValue("InserionText", out var insertionText))
            {
                return new TextChange(selectedItem.Span, insertionText);
            }

            return await base.GetTextChangeAsync(document, selectedItem, ch, cancellationToken).ConfigureAwait(false);
        }

        protected override IEnumerable<CompletionItem> GetCompletionItems(SyntaxToken token, SyntaxGenerator generator, ISyntaxKindsService syntaxKinds, ISyntaxFactsService syntaxFacts)
        {
            yield return CommonCompletionItem.Create(
                displayText: "if",
                displayTextSuffix: "",
                rules: CompletionItemRules.Default,
                Glyph.Snippet,
                inlineDescription: "Add if statment",
                properties: ImmutableDictionary.Create<string, string>().Add("InserionText", "if"),
                isComplexTextEdit: false);

            yield return CommonCompletionItem.Create(
                displayText: "while loop",
                displayTextSuffix: "",
                rules: CompletionItemRules.Default,
                Glyph.Snippet,
                inlineDescription: "Add whipe loop",
                properties: ImmutableDictionary.Create<string, string>().Add("InserionText", "while"),
                isComplexTextEdit: false);

            yield return CommonCompletionItem.Create(
                displayText: "do loop",
                displayTextSuffix: "",
                rules: CompletionItemRules.Default,
                Glyph.Snippet,
                inlineDescription: "Add do..while loop",
                properties: ImmutableDictionary.Create<string, string>().Add("InserionText", "do"),
                isComplexTextEdit: false);

            yield return CommonCompletionItem.Create(
                displayText: "else",
                displayTextSuffix: "",
                rules: CompletionItemRules.Default,
                Glyph.Snippet,
                inlineDescription: "Add else statment",
                properties: ImmutableDictionary.Create<string, string>().Add("InserionText", "else"),
                isComplexTextEdit: false);

            yield return CommonCompletionItem.Create(
                displayText: "for loop",
                displayTextSuffix: "",
                rules: CompletionItemRules.Default,
                Glyph.Snippet,
                inlineDescription: "Add for loop",
                properties: ImmutableDictionary.Create<string, string>().Add("InserionText", "for"),
                isComplexTextEdit: false);

            yield return CommonCompletionItem.Create(
                displayText: "reverse for loop",
                displayTextSuffix: "",
                rules: CompletionItemRules.Default,
                Glyph.Snippet,
                inlineDescription: "Add reverse for loop",
                properties: ImmutableDictionary.Create<string, string>().Add("InserionText", "forr"),
                isComplexTextEdit: false);

            yield return CommonCompletionItem.Create(
                displayText: "lock",
                displayTextSuffix: "",
                rules: CompletionItemRules.Default,
                Glyph.Snippet,
                inlineDescription: "Add lock statment",
                properties: ImmutableDictionary.Create<string, string>().Add("InserionText", "lock"),
                isComplexTextEdit: false);

            yield return CommonCompletionItem.Create(
                displayText: "try catch",
                displayTextSuffix: "",
                rules: CompletionItemRules.Default,
                Glyph.Snippet,
                inlineDescription: "Add try catch statment",
                properties: ImmutableDictionary.Create<string, string>().Add("InserionText", "try"),
                isComplexTextEdit: false);

            yield return CommonCompletionItem.Create(
                displayText: "try finally",
                displayTextSuffix: "",
                rules: CompletionItemRules.Default,
                Glyph.Snippet,
                inlineDescription: "Add try finally statment",
                properties: ImmutableDictionary.Create<string, string>().Add("InserionText", "tryf"),
                isComplexTextEdit: false);

            yield return CommonCompletionItem.Create(
                displayText: "using",
                displayTextSuffix: "",
                rules: CompletionItemRules.Default,
                Glyph.Snippet,
                inlineDescription: "Add using statment",
                properties: ImmutableDictionary.Create<string, string>().Add("InserionText", "using"),
                isComplexTextEdit: false);
        }
    }
}
