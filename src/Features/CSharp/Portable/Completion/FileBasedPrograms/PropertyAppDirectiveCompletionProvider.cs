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

namespace Microsoft.CodeAnalysis.FileBasedPrograms;

[ExportCompletionProvider(nameof(PropertyAppDirectiveCompletionProvider), LanguageNames.CSharp)]
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

        if (token.IsKind(SyntaxKind.ColonToken))
        {
            addDirectiveKindCompletion();
        }
        else if (token.IsKind(SyntaxKind.StringLiteralToken))
        {
            var textLeftOfCaret = token.ValueText.AsSpan(start: 0, length: context.Position - token.SpanStart);
            if (DirectiveKind.StartsWith(textLeftOfCaret))
            {
                addDirectiveKindCompletion();
            }
        }

        void addDirectiveKindCompletion()
        {
            context.AddItem(CommonCompletionItem.Create(DirectiveKind, displayTextSuffix: "", CompletionItemRules.Default, glyph: Glyph.Keyword,
                // TODO2 localizable
                description: [
                    new(SymbolDisplayPartKind.Text, symbol: null, "#:property Name=Value"),
                    new(SymbolDisplayPartKind.LineBreak, symbol: null, ""),
                    new(SymbolDisplayPartKind.Text, symbol: null, "Defines a build property."),
                    ]));
        }
    }
}
