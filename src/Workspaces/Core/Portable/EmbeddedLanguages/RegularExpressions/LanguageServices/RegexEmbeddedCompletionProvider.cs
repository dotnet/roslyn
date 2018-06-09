// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions.LanguageServices
{
    using RegexToken = EmbeddedSyntaxToken<RegexKind>;

    internal class RegexEmbeddedCompletionProvider : IEmbeddedCompletionProvider
    {
        private readonly RegexEmbeddedLanguage _language;

        public RegexEmbeddedCompletionProvider(RegexEmbeddedLanguage language)
        {
            _language = language;
        }

        public bool ShouldTriggerCompletion(SourceText text, int caretPosition, EmbeddedCompletionTrigger trigger, OptionSet options)
        {
            if (trigger.Kind == EmbeddedCompletionTriggerKind.Invoke ||
                trigger.Kind == EmbeddedCompletionTriggerKind.InvokeAndCommitIfUnique)
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
            var treeAndStringToken = await _language.TryGetTreeAndTokenAtPositionAsync(
                context.Document, position, context.CancellationToken).ConfigureAwait(false);
            if (treeAndStringToken == null)
            {
                return;
            }

            if (context.Trigger.Kind != EmbeddedCompletionTriggerKind.Invoke &&
                context.Trigger.Kind != EmbeddedCompletionTriggerKind.InvokeAndCommitIfUnique &&
                context.Trigger.Kind != EmbeddedCompletionTriggerKind.Insertion)
            {
                return;
            }

            // First, act as if the user just inserted the previous character.  This will cause us
            // to complete down to the set of relevant items based on that character. If we get
            // anything, we're done and can just show the user those items.  If we have no items to
            // add *and* the user was explicitly invoking completion, then just add the entire set
            // of suggestions to help the user out.
            var count = context.Items.Count;

            ProvideCompletionsAfterInsertion(context, treeAndStringToken.Value.tree, treeAndStringToken.Value.token);

            if (count != context.Items.Count)
            {
                // We added items.  Nothing else to do here.
                return;
            }

            if (context.Trigger.Kind == EmbeddedCompletionTriggerKind.Insertion)
            {
                // The user was typing a character, and we had nothing to add for them.  Just bail
                // out immediately as we cannot help in this circumstance.
                return;
            }

            // We added no items, but the user explicitly asked for completion.  Add all the
            // items we can to help them out.
            // Otherwise, we want to provide all completions.
        }

        private void ProvideCompletionsAfterInsertion(
            EmbeddedCompletionContext context, RegexTree tree, SyntaxToken stringToken)
        {
            var position = context.Position;
            var previousVirtualChar = tree.Text.FirstOrNullable(vc => vc.Span.Contains(position - 1));
            if (previousVirtualChar == null)
            {
                return;
            }

            var result = FindToken(tree.Root, previousVirtualChar.Value, inCharacterClass: false);
            if (result == null)
            {
                return;
            }

            var (parent, token, inCharacterClass) = result.Value;
            switch (token.Kind)
            {
                case RegexKind.BackslashToken:
                    ProvideEscapeCompletions(context, stringToken, parent, inCharacterClass);
                    return;
                case RegexKind.OpenBracketToken:
                    // ProvideCharacterClassCompletions(context);
                    return;
                case RegexKind.OpenParenToken:
                case RegexKind.QuestionToken:
                case RegexKind.LessThanToken:
                case RegexKind.EqualsToken:
                case RegexKind.SingleQuoteToken:
                case RegexKind.ExclamationToken:
                    // ProvideGroupingCompletions(context);
                    return;
                case RegexKind.OpenBraceToken:
                    // ProvideEscapeCategoryCompletions(context);
                    return;
                case RegexKind.OptionsToken:
                    // ProvideOptionsCompletions(context, optionsT);
                    return;
            }
        }

        private void ProvideEscapeCompletions(
            EmbeddedCompletionContext context, SyntaxToken stringToken,
            RegexNode parentOpt, bool inCharacterClass)
        {
            if (parentOpt != null && !(parentOpt is RegexEscapeNode))
            {
                return;
            }

            if (!inCharacterClass)
            {
                AddIfMissing(context, CreateEscapeItem(stringToken, @"\A", "", context, parentOpt));
                AddIfMissing(context, CreateEscapeItem(stringToken, @"\b", "", context, parentOpt));
                AddIfMissing(context, CreateEscapeItem(stringToken, @"\B", "", context, parentOpt));
                AddIfMissing(context, CreateEscapeItem(stringToken, @"\d", "", context, parentOpt));
                AddIfMissing(context, CreateEscapeItem(stringToken, @"\D", "", context, parentOpt));
                AddIfMissing(context, CreateEscapeItem(stringToken, @"\G", "", context, parentOpt));
                AddIfMissing(context, CreateEscapeItem(stringToken, @"\p{}", "", context, parentOpt, @"\p{".Length));
                AddIfMissing(context, CreateEscapeItem(stringToken, @"\P{}", "", context, parentOpt, @"\p{".Length));
                AddIfMissing(context, CreateEscapeItem(stringToken, @"\s", "", context, parentOpt));
                AddIfMissing(context, CreateEscapeItem(stringToken, @"\S", "", context, parentOpt));
                AddIfMissing(context, CreateEscapeItem(stringToken, @"\w", "", context, parentOpt));
                AddIfMissing(context, CreateEscapeItem(stringToken, @"\W", "", context, parentOpt));
                AddIfMissing(context, CreateEscapeItem(stringToken, @"\z", "", context, parentOpt));
                AddIfMissing(context, CreateEscapeItem(stringToken, @"\Z", "", context, parentOpt));

                AddIfMissing(context, CreateEscapeItem(stringToken, @"\k<>", "", context, parentOpt, @"\k<".Length));
                AddIfMissing(context, CreateEscapeItem(stringToken, @"\<>", "", context, parentOpt, @"\<".Length));
                AddIfMissing(context, CreateEscapeItem(stringToken, @"\1-9", "", context, parentOpt, @"\".Length, @"\"));
            }

            AddIfMissing(context, CreateEscapeItem(stringToken, @"\a", "", context, parentOpt));
            AddIfMissing(context, CreateEscapeItem(stringToken, @"\b", "", context, parentOpt));
            AddIfMissing(context, CreateEscapeItem(stringToken, @"\e", "", context, parentOpt));
            AddIfMissing(context, CreateEscapeItem(stringToken, @"\f", "", context, parentOpt));
            AddIfMissing(context, CreateEscapeItem(stringToken, @"\n", "", context, parentOpt));
            AddIfMissing(context, CreateEscapeItem(stringToken, @"\r", "carriage return", context, parentOpt));
            AddIfMissing(context, CreateEscapeItem(stringToken, @"\t", "horizontal tab", context, parentOpt));
            AddIfMissing(context, CreateEscapeItem(stringToken, @"\v", "vertical tab", context, parentOpt));

            AddIfMissing(context, CreateEscapeItem(stringToken, @"\x##", "", context, parentOpt, @"\x".Length, @"\x"));
            AddIfMissing(context, CreateEscapeItem(stringToken, @"\u####", "", context, parentOpt, @"\u".Length, @"\u"));
            AddIfMissing(context, CreateEscapeItem(stringToken, @"\c", "", context, parentOpt, @"\c".Length, @"\c"));
        }

        private void AddIfMissing(EmbeddedCompletionContext context, EmbeddedCompletionItem item)
        {
            foreach (var existingItem in context.Items)
            {
                if (existingItem.DisplayText == item.DisplayText)
                {
                    return;
                }
            }

            context.Items.Add(item);
        }

        private EmbeddedCompletionItem CreateEscapeItem(
            SyntaxToken stringToken, string displayText, string description,
            EmbeddedCompletionContext context, RegexNode parentOpt, 
            int? positionOffset = null, string insertionText = null)
        {
            var replacementStart = parentOpt != null
                ? parentOpt.GetSpan().Start
                : context.Position;

            var replacementSpan = TextSpan.FromBounds(replacementStart, context.Position);
            var newPosition = replacementStart + positionOffset;

            insertionText = _language.EscapeText(insertionText ?? displayText, stringToken);
            if (description != "")
            {
                displayText += "  -  " + description;
            }

            return new EmbeddedCompletionItem(
                displayText, description, 
                new EmbeddedCompletionChange(
                    new TextChange(replacementSpan, insertionText), newPosition, includesCommitCharacter: false));
        }

        private (RegexNode parent, RegexToken Token, bool inCharacterClass)? FindToken(
            RegexNode parent, VirtualChar ch, bool inCharacterClass)
        {
            foreach (var child in parent)
            {
                if (child.IsNode)
                {
                    var result = FindToken(child.Node, ch, inCharacterClass || child.Node is RegexBaseCharacterClassNode);
                    if (result != null)
                    {
                        return result;
                    }
                }
                else
                {
                    if (child.Token.VirtualChars.Contains(ch))
                    {
                        return (parent, child.Token, inCharacterClass);
                    }
                }
            }

            return null;
        }
    }
}
