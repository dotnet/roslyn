// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

        public static bool TreatAsDot(SyntaxToken token, int characterPosition)
        {
            if (token.Kind() == SyntaxKind.DotToken)
                return true;

            // if we're right after the first dot in .. then that's considered completion on dot.
            if (token.Kind() == SyntaxKind.DotDotToken && token.SpanStart == characterPosition)
                return true;

            return false;
        }

        public static SyntaxToken? GetDotTokenLeftOfPosition(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            var tokenOnLeft = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken, includeSkipped: true);
            var dotToken = tokenOnLeft.GetPreviousTokenIfTouchingWord(position);

            // Has to be a . or a .. token
            if (!TreatAsDot(dotToken, position - 1))
                return null;

            // don't want to trigger after a number. All other cases after dot are ok.
            if (dotToken.GetPreviousToken().Kind() == SyntaxKind.NumericLiteralToken)
                return null;

            return dotToken;
        }

        internal static bool IsTriggerCharacter(SourceText text, int characterPosition, in CompletionOptions options)
        {
            var ch = text[characterPosition];

            // Trigger off of a normal `.`, but not off of `..`
            if (ch == '.' && !(characterPosition >= 1 && text[characterPosition - 1] == '.'))
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

            if (options.TriggerOnTypingLetters && IsStartingNewWord(text, characterPosition))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tells if we are in positions like this: <c>#nullable $$</c> or <c>#pragma warning $$</c>
        /// </summary>
        internal static bool IsCompilerDirectiveTriggerCharacter(SourceText text, int characterPosition)
        {
            while (text[characterPosition] == ' ' ||
                   char.IsLetter(text[characterPosition]))
            {
                characterPosition--;

                if (characterPosition < 0)
                    return false;
            }

            return text[characterPosition] == '#';
        }

        internal static ImmutableHashSet<char> CommonTriggerCharacters { get; } = ImmutableHashSet.Create('.', '#', '>', ':');

        internal static ImmutableHashSet<char> CommonTriggerCharactersWithArgumentList { get; } = ImmutableHashSet.Create('.', '#', '>', ':', '(', '[', ' ');

        internal static bool IsTriggerCharacterOrArgumentListCharacter(SourceText text, int characterPosition, in CompletionOptions options)
            => IsTriggerCharacter(text, characterPosition, options) || IsArgumentListCharacter(text, characterPosition);

        private static bool IsArgumentListCharacter(SourceText text, int characterPosition)
            => IsArgumentListCharacter(text[characterPosition]);

        internal static bool IsArgumentListCharacter(char ch)
            => ch is '(' or '[' or ' ';

        internal static bool IsTriggerAfterSpaceOrStartOfWordCharacter(SourceText text, int characterPosition, in CompletionOptions options)
        {
            // Bring up on space or at the start of a word.
            var ch = text[characterPosition];
            return SpaceTypedNotBeforeWord(ch, text, characterPosition) ||
                (IsStartingNewWord(text, characterPosition) && options.TriggerOnTypingLetters);
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
