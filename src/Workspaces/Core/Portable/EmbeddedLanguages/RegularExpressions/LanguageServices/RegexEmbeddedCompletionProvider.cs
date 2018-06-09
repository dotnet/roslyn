// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using System;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions.LanguageServices
{
    using static EmbeddedSyntaxHelpers;

    using RegexToken = EmbeddedSyntaxToken<RegexKind>;
    using RegexTrivia = EmbeddedSyntaxTrivia<RegexKind>;

    internal class RegexEmbeddedCompletionProvider : IEmbeddedCompletionProvider
    {
        private readonly RegexEmbeddedLanguage _language;

        public RegexEmbeddedCompletionProvider(RegexEmbeddedLanguage language)
        {
            _language = language;
        }

        public bool ShouldTriggerCompletion(SourceText text, int caretPosition, EmbeddedCompletionTrigger trigger, OptionSet options)
        {
            if (trigger.Kind == EmbeddedCompletionTriggerKind.Invoke)
            {
                return true;
            }

            if (trigger.Kind == EmbeddedCompletionTriggerKind.Insertion)
            {
                return IsTriggerCharacter(trigger.Character);
            }

            return false;
        }

        private bool IsTriggerCharacter(char ch)
        {
            switch (ch)
            {
                case '\\': // any escape
                case '[':  
                case '^':  // character class
                case '(':  // any group
                case '?':  // (?
                case '<':  // (?<
                case '=':  // (?<=
                case '\'': // (?'
                case '!':  // (?<!
                case '{':  // \p{
                case '+': case '-':
                case 'i': case 'I':
                case 'm': case 'M':
                case 'n': case 'N':
                case 's': case 'S':
                case 'x': case 'X': // (?options
                    return true;
            }

            return false;
        }

        public async Task ProvideCompletionsAsync(EmbeddedCompletionContext context)
        {
            if (!context.Options.GetOption(RegularExpressionsOptions.ProvideRegexCompletions, context.Document.Project.Language))
            {
                return;
            }

            var position = context.Position;
            var tree = await _language.TryGetTreeAtPositionAsync(
                context.Document, position, context.CancellationToken).ConfigureAwait(false);
            if (tree == null)
            {
                return;
            }

            var previousVirtualChar = tree.Text.FirstOrNullable(vc => vc.Span.Contains(position - 1));
            if (previousVirtualChar == null)
            {
                return;
            }

            var token = FindToken(tree.Root, previousVirtualChar.Value);
            if (token == null)
            {
                return;
            }

            switch (token.Value.Kind)
            {
                case RegexKind.BackslashToken:
                    ProvideEscapeCompletions(context);
                    return;
                case RegexKind.OpenBracketToken:
                    ProvideCharacterClassCompletions(context);
                    return;
                case RegexKind.OpenParenToken:
                case RegexKind.QuestionToken:
                case RegexKind.LessThanToken:
                case RegexKind.EqualsToken:
                case RegexKind.SingleQuoteToken:
                case RegexKind.ExclamationToken:
                    ProvideGroupingCompletions(context);
                    return;
                case RegexKind.OpenBraceToken:
                    ProvideEscapeCategoryCompletions(context);
                    return;
                case RegexKind.OptionsToken:
                    ProvideOptionsCompletions(context);
                    return;
            }

            /*
       case '\\': // any escape
                case '[':  
                case '^':  // character class
                case '(':  // any group
                case '?':  // (?
                case '<':  // (?<
                case '=':  // (?<=
                case '\'': // (?'
                case '!':  // (?<!
                case '+': case '-':
                case 'i': case 'I':
                case 'm': case 'M':
                case 'n': case 'N':
                case 's': case 'S':
                case 'x': case 'X': // (?options
                    return true;      
     */
        }

        private RegexToken? FindToken(RegexNode node, VirtualChar ch)
        {
            foreach (var child in node)
            {
                if (child.IsNode)
                {
                    var token = FindToken(child.Node, ch);
                    if (token != null)
                    {
                        return token;
                    }
                }
                else
                {
                    if (child.Token.VirtualChars.Contains(ch))
                    {
                        return child.Token;
                    }
                }
            }

            return null;
        }
    }
}
