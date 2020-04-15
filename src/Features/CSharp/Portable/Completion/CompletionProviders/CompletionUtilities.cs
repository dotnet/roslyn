// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal static class CompletionUtilities
    {
        internal static TextSpan GetCompletionItemSpan(SourceText text, int position)
            => CommonCompletionUtilities.GetWordSpan(text, position, IsCompletionItemStartCharacter, IsWordCharacter);

        public static bool IsWordStartCharacter(char ch)
            => SyntaxFacts.IsIdentifierStartCharacter(ch);

        public static bool IsWordCharacter(char ch)
            => SyntaxFacts.IsIdentifierStartCharacter(ch) || SyntaxFacts.IsIdentifierPartCharacter(ch);

        public static bool IsCompletionItemStartCharacter(char ch)
            => ch == '@' || IsWordCharacter(ch);

        internal static bool IsTriggerCharacter(SourceText text, int characterPosition, OptionSet options)
        {
            var ch = text[characterPosition];
            if (ch == '.')
            {
                return true;
            }

            // Trigger for directive
            if (ch == '#')
            {
                return true;
            }

            // Trigger on pointer member access
            if (ch == '>' && characterPosition >= 1 && text[characterPosition - 1] == '-')
            {
                return true;
            }

            // Trigger on alias name
            if (ch == ':' && characterPosition >= 1 && text[characterPosition - 1] == ':')
            {
                return true;
            }

            if (options.GetOption(CompletionOptions.TriggerOnTypingLetters2, LanguageNames.CSharp) && IsStartingNewWord(text, characterPosition))
            {
                return true;
            }

            return false;
        }

        internal static ImmutableHashSet<char> CommonTriggerCharacters { get; } = ImmutableHashSet.Create('.', '#', '>', ':');

        internal static ImmutableHashSet<char> CommonTriggerCharactersWithArgumentList { get; } = ImmutableHashSet.Create('.', '#', '>', ':', '(', '[', ' ');

        internal static bool IsTriggerCharacterOrArgumentListCharacter(SourceText text, int characterPosition, OptionSet options)
            => IsTriggerCharacter(text, characterPosition, options) || IsArgumentListCharacter(text, characterPosition);

        private static bool IsArgumentListCharacter(SourceText text, int characterPosition)
            => IsArgumentListCharacter(text[characterPosition]);

        internal static bool IsArgumentListCharacter(char ch)
            => ch == '(' || ch == '[' || ch == ' ';

        internal static bool IsTriggerAfterSpaceOrStartOfWordCharacter(SourceText text, int characterPosition, OptionSet options)
        {
            // Bring up on space or at the start of a word.
            var ch = text[characterPosition];
            return SpaceTypedNotBeforeWord(ch, text, characterPosition) ||
                (IsStartingNewWord(text, characterPosition) && options.GetOption(CompletionOptions.TriggerOnTypingLetters2, LanguageNames.CSharp));
        }

        internal static ImmutableHashSet<char> SpaceTriggerCharacter => ImmutableHashSet.Create(' ');

        private static bool SpaceTypedNotBeforeWord(char ch, SourceText text, int characterPosition)
            => ch == ' ' && (characterPosition == text.Length - 1 || !IsWordStartCharacter(text[characterPosition + 1]));

        public static bool IsStartingNewWord(SourceText text, int characterPosition)
        {
            return CommonCompletionUtilities.IsStartingNewWord(
                text, characterPosition, IsWordStartCharacter, IsWordCharacter);
        }

        public static (string displayText, string suffix, string insertionText) GetDisplayAndSuffixAndInsertionText(
            ISymbol symbol, SyntaxContext context)
        {
            var insertionText = GetInsertionText(symbol, context);
            var suffix = symbol.GetArity() == 0 ? "" : "<>";

            return (insertionText, suffix, insertionText);
        }

        public static string GetInsertionText(ISymbol symbol, SyntaxContext context)
        {
            if (CommonCompletionUtilities.TryRemoveAttributeSuffix(symbol, context, out var name))
            {
                // Cannot escape Attribute name with the suffix removed. Only use the name with
                // the suffix removed if it does not need to be escaped.
                if (name.Equals(name.EscapeIdentifier()))
                {
                    return name;
                }
            }

            if (symbol.Kind == SymbolKind.Label &&
                symbol.DeclaringSyntaxReferences[0].GetSyntax().Kind() == SyntaxKind.DefaultSwitchLabel)
            {
                return symbol.Name;
            }

            return symbol.Name.EscapeIdentifier(isQueryContext: context.IsInQuery);
        }

        public static int GetTargetCaretPositionForMethod(MethodDeclarationSyntax methodDeclaration)
        {
            if (methodDeclaration.Body == null)
            {
                return methodDeclaration.GetLocation().SourceSpan.End;
            }
            else
            {
                // move to the end of the last statement in the method
                var lastStatement = methodDeclaration.Body.Statements.Last();
                return lastStatement.GetLocation().SourceSpan.End;
            }
        }
    }
}
