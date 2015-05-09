// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal static class CompletionUtilities
    {
        internal static TextSpan GetTextChangeSpan(SourceText text, int position)
        {
            return CommonCompletionUtilities.GetTextChangeSpan(text, position, IsTextChangeSpanStartCharacter, IsWordCharacter);
        }

        public static bool IsWordStartCharacter(char ch)
        {
            return SyntaxFacts.IsIdentifierStartCharacter(ch);
        }

        public static bool IsWordCharacter(char ch)
        {
            return SyntaxFacts.IsIdentifierStartCharacter(ch) || SyntaxFacts.IsIdentifierPartCharacter(ch);
        }

        public static bool IsTextChangeSpanStartCharacter(char ch)
        {
            return ch == '@' || IsWordCharacter(ch);
        }

        internal static bool IsCommitCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            // TODO(cyrusn): Don't hardcode this in.  Suck this out of the user options.
            var commitCharacters = new[]
            {
                ' ', '{', '}', '[', ']', '(', ')', '.', ',', ':',
                ';', '+', '-', '*', '/', '%', '&', '|', '^', '!',
                '~', '=', '<', '>', '?', '@', '#', '\'', '\"', '\\'
            };

            return commitCharacters.Contains(ch);
        }

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

            if (options.GetOption(CompletionOptions.TriggerOnTypingLetters, LanguageNames.CSharp) && IsStartingNewWord(text, characterPosition))
            {
                return true;
            }

            return false;
        }

        internal static bool IsTriggerAfterSpaceOrStartOfWordCharacter(SourceText text, int characterPosition, OptionSet options)
        {
            // Bring up on space or at the start of a word.
            var ch = text[characterPosition];
            return SpaceTypedNotBeforeWord(ch, text, characterPosition) ||
                (CompletionUtilities.IsStartingNewWord(text, characterPosition) && options.GetOption(CompletionOptions.TriggerOnTypingLetters, LanguageNames.CSharp));
        }

        private static bool SpaceTypedNotBeforeWord(char ch, SourceText text, int characterPosition)
        {
            return ch == ' ' && (characterPosition == text.Length - 1 || !IsWordStartCharacter(text[characterPosition + 1]));
        }

        public static bool IsStartingNewWord(SourceText text, int characterPosition)
        {
            return CommonCompletionUtilities.IsStartingNewWord(
                text, characterPosition, IsWordStartCharacter, IsWordCharacter);
        }

        internal static bool SendEnterThroughToEditor(CompletionItem completionItem, string textTypedSoFar)
        {
            // If the text doesn't match, no reason to even check the options
            if (completionItem.DisplayText != textTypedSoFar)
            {
                return false;
            }

            Workspace workspace = null;
            var csharpCompletionItem = completionItem as CSharpCompletionItem;

            if (csharpCompletionItem != null)
            {
                workspace = csharpCompletionItem.Workspace;
            }
            else
            {
                workspace = ((SymbolCompletionItem)completionItem).Context.Workspace;
            }

            var optionService = workspace.Services.GetService<IOptionService>();
            return optionService.GetOption(CSharpCompletionOptions.AddNewLineOnEnterAfterFullyTypedWord);
        }
    }
}
