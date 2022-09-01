// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion
{
    internal static class CommonCompletionUtilities
    {
        private const string NonBreakingSpaceString = "\x00A0";

        public static TextSpan GetWordSpan(SourceText text, int position,
            Func<char, bool> isWordStartCharacter, Func<char, bool> isWordCharacter)
        {
            return GetWordSpan(text, position, isWordStartCharacter, isWordCharacter, alwaysExtendEndSpan: false);
        }

        public static TextSpan GetWordSpan(SourceText text, int position,
            Func<char, bool> isWordStartCharacter, Func<char, bool> isWordCharacter, bool alwaysExtendEndSpan = false)
        {
            var start = position;
            while (start > 0 && isWordStartCharacter(text[start - 1]))
            {
                start--;
            }

            // If we're brought up in the middle of a word, extend to the end of the word as well.
            // This means that if a user brings up the completion list at the start of the word they
            // will "insert" the text before what's already there (useful for qualifying existing
            // text).  However, if they bring up completion in the "middle" of a word, then they will
            // "overwrite" the text. Useful for correcting misspellings or just replacing unwanted
            // code with new code.
            var end = position;
            if (start != position || alwaysExtendEndSpan)
            {
                while (end < text.Length && isWordCharacter(text[end]))
                {
                    end++;
                }
            }

            return TextSpan.FromBounds(start, end);
        }

        public static bool IsStartingNewWord(SourceText text, int characterPosition, Func<char, bool> isWordStartCharacter, Func<char, bool> isWordCharacter)
        {
            var ch = text[characterPosition];
            if (!isWordStartCharacter(ch))
            {
                return false;
            }

            // Only want to trigger if we're the first character in an identifier.  If there's a
            // character before or after us, then we don't want to trigger.
            if (characterPosition > 0 &&
                isWordCharacter(text[characterPosition - 1]))
            {
                return false;
            }

            if (characterPosition < text.Length - 1 &&
                isWordCharacter(text[characterPosition + 1]))
            {
                return false;
            }

            return true;
        }

        public static Func<CancellationToken, Task<CompletionDescription>> CreateDescriptionFactory(
            HostWorkspaceServices workspaceServices,
            SemanticModel semanticModel,
            int position,
            ISymbol symbol,
            SymbolDescriptionOptions options)
        {
            return CreateDescriptionFactory(workspaceServices, semanticModel, position, options, new[] { symbol });
        }

        public static Func<CancellationToken, Task<CompletionDescription>> CreateDescriptionFactory(
            HostWorkspaceServices workspaceServices, SemanticModel semanticModel, int position, SymbolDescriptionOptions options, IReadOnlyList<ISymbol> symbols)
        {
            return c => CreateDescriptionAsync(workspaceServices, semanticModel, position, symbols, options, supportedPlatforms: null, cancellationToken: c);
        }

        public static Func<CancellationToken, Task<CompletionDescription>> CreateDescriptionFactory(
            HostWorkspaceServices workspaceServices, SemanticModel semanticModel, int position, IReadOnlyList<ISymbol> symbols, SymbolDescriptionOptions options, SupportedPlatformData supportedPlatforms)
        {
            return c => CreateDescriptionAsync(workspaceServices, semanticModel, position, symbols, options, supportedPlatforms: supportedPlatforms, cancellationToken: c);
        }

        public static async Task<CompletionDescription> CreateDescriptionAsync(
            HostWorkspaceServices workspaceServices, SemanticModel semanticModel, int position, ISymbol symbol, int overloadCount, SymbolDescriptionOptions options, SupportedPlatformData? supportedPlatforms, CancellationToken cancellationToken)
        {
            var symbolDisplayService = workspaceServices.GetLanguageServices(semanticModel.Language).GetRequiredService<ISymbolDisplayService>();
            var formatter = workspaceServices.GetLanguageServices(semanticModel.Language).GetRequiredService<IDocumentationCommentFormattingService>();

            // TODO(cyrusn): Figure out a way to cancel this.
            var sections = await symbolDisplayService.ToDescriptionGroupsAsync(semanticModel, position, ImmutableArray.Create(symbol), options, cancellationToken).ConfigureAwait(false);

            if (!sections.ContainsKey(SymbolDescriptionGroups.MainDescription))
            {
                return CompletionDescription.Empty;
            }

            var textContentBuilder = new List<TaggedText>();
            textContentBuilder.AddRange(sections[SymbolDescriptionGroups.MainDescription]);

            switch (symbol.Kind)
            {
                case SymbolKind.Method:
                case SymbolKind.Property:
                case SymbolKind.NamedType:
                    if (overloadCount > 0)
                    {
                        var isGeneric = symbol.GetArity() > 0;

                        textContentBuilder.AddSpace();
                        textContentBuilder.AddPunctuation("(");
                        textContentBuilder.AddPunctuation("+");
                        textContentBuilder.AddText(NonBreakingSpaceString + overloadCount.ToString());

                        AddOverloadPart(textContentBuilder, overloadCount, isGeneric);

                        textContentBuilder.AddPunctuation(")");
                    }

                    break;
            }

            AddDocumentationPart(textContentBuilder, symbol, semanticModel, position, formatter, cancellationToken);

            if (sections.TryGetValue(SymbolDescriptionGroups.AwaitableUsageText, out var parts))
            {
                textContentBuilder.AddRange(parts);
            }

            if (sections.TryGetValue(SymbolDescriptionGroups.StructuralTypes, out parts))
            {
                if (!parts.IsDefaultOrEmpty)
                {
                    textContentBuilder.AddLineBreak();
                    textContentBuilder.AddLineBreak();
                    textContentBuilder.AddRange(parts);
                }
            }

            if (supportedPlatforms != null)
            {
                textContentBuilder.AddLineBreak();
                textContentBuilder.AddRange(supportedPlatforms.ToDisplayParts().ToTaggedText());
            }

            return CompletionDescription.Create(textContentBuilder.AsImmutable());
        }

        public static Task<CompletionDescription> CreateDescriptionAsync(
            HostWorkspaceServices workspaceServices, SemanticModel semanticModel, int position, IReadOnlyList<ISymbol> symbols, SymbolDescriptionOptions options, SupportedPlatformData? supportedPlatforms, CancellationToken cancellationToken)
        {
            // Lets try to find the first non-obsolete symbol (overload) and fall-back
            // to the first symbol if all are obsolete.
            var symbol = symbols.FirstOrDefault(s => !s.IsObsolete()) ?? symbols[0];

            return CreateDescriptionAsync(workspaceServices, semanticModel, position, symbol, overloadCount: symbols.Count - 1, options, supportedPlatforms, cancellationToken);
        }

        private static void AddOverloadPart(List<TaggedText> textContentBuilder, int overloadCount, bool isGeneric)
        {
            var text = isGeneric
                ? overloadCount == 1
                    ? FeaturesResources.generic_overload
                    : FeaturesResources.generic_overloads
                : overloadCount == 1
                    ? FeaturesResources.overload
                    : FeaturesResources.overloads_;

            textContentBuilder.AddText(NonBreakingSpaceString + text);
        }

        private static void AddDocumentationPart(
            List<TaggedText> textContentBuilder, ISymbol symbol, SemanticModel semanticModel, int position, IDocumentationCommentFormattingService formatter, CancellationToken cancellationToken)
        {
            var documentation = symbol.GetDocumentationParts(semanticModel, position, formatter, cancellationToken);

            if (documentation.Any())
            {
                textContentBuilder.AddLineBreak();
                textContentBuilder.AddRange(documentation);
            }
        }

        internal static bool IsTextualTriggerString(SourceText text, int characterPosition, string value)
        {
            // The character position starts at the last character of 'value'.  So if 'value' has
            // length 1, then we don't want to move, if it has length 2 we want to move back one,
            // etc.
            characterPosition = characterPosition - value.Length + 1;

            for (var i = 0; i < value.Length; i++, characterPosition++)
            {
                if (characterPosition < 0 || characterPosition >= text.Length)
                {
                    return false;
                }

                if (text[characterPosition] != value[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static bool TryRemoveAttributeSuffix(ISymbol symbol, SyntaxContext context, [NotNullWhen(true)] out string? name)
        {
            var isAttributeNameContext = context.IsAttributeNameContext;
            var syntaxFacts = context.GetRequiredLanguageService<ISyntaxFactsService>();

            if (!isAttributeNameContext)
            {
                name = null;
                return false;
            }

            // Do the symbol textual check first. Then the more expensive symbolic check.
            if (!symbol.Name.TryGetWithoutAttributeSuffix(syntaxFacts.IsCaseSensitive, out name) ||
                !symbol.IsAttribute())
            {
                return false;
            }

            return true;
        }

        internal static ImmutableHashSet<char> GetTriggerCharacters(CompletionProvider provider)
        {
            if (provider is LSPCompletionProvider lspProvider)
            {
                return lspProvider.TriggerCharacters;
            }

            return ImmutableHashSet<char>.Empty;
        }
    }
}
