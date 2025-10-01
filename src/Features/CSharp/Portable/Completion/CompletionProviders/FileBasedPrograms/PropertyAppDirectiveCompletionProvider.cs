// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

[ExportCompletionProvider(nameof(PropertyAppDirectiveCompletionProvider), LanguageNames.CSharp)]
[ExtensionOrder(After = nameof(ReferenceDirectiveCompletionProvider))]
[Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class PropertyAppDirectiveCompletionProvider() : LSPCompletionProvider
{
    private const string DirectiveKind = "property";

    public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
    {
        return TriggerCharacters.Contains(text[characterPosition])
            || (options.TriggerOnTypingLetters && CompletionUtilities.IsStartingNewWord(text, characterPosition));
    }

    public override ImmutableHashSet<char> TriggerCharacters { get; } = [':'];

    internal override string Language => LanguageNames.CSharp;

    public override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        var tree = await context.Document.GetRequiredSyntaxTreeAsync(context.CancellationToken).ConfigureAwait(false);
        if (!tree.Options.Features.ContainsKey("FileBasedProgram"))
            return;

        var token = tree.GetRoot(context.CancellationToken).FindTokenOnLeftOfPosition(context.Position, includeDirectives: true);
        if (token.Parent is not IgnoredDirectiveTriviaSyntax ignoredDirective)
            return;

        // Note that in the `#: $$` case, the whitespace is trailing trivia on the colon-token.
        if (token == ignoredDirective.ColonToken)
        {
            addDirectiveKindCompletion();
        }
        else if (token == ignoredDirective.Content)
        {
            // For a test case like '#: pro$$ Name=Value', then:
            // We know that 'token.Text == "pro Name=Value"', and, the below expressions correspond to text positions as shown:
            // #: pro Name=Value
            //    │  │
            //    │  └─context.Position
            //    └────token.SpanStart
            var textLeftOfCaret = token.Text.AsSpan(start: 0, length: context.Position - token.SpanStart);
            if (DirectiveKind.StartsWith(textLeftOfCaret))
            {
                addDirectiveKindCompletion();
            }
        }

        void addDirectiveKindCompletion()
        {
            context.AddItem(CommonCompletionItem.Create(DirectiveKind, displayTextSuffix: "", CompletionItemRules.Default, glyph: Glyph.Keyword,
                description: [
                    new(SymbolDisplayPartKind.Text, symbol: null, CSharpFeaturesResources.Hash_colon_property__Name_equals_Value),
                    new(SymbolDisplayPartKind.LineBreak, symbol: null, ""),
                    new(SymbolDisplayPartKind.Text, symbol: null, CSharpFeaturesResources.Defines_a_build_property),
                    ]));
        }
    }
}
