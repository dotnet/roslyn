// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.FileSystem
{
    internal static class PathCompletionUtilities
    {
        internal static bool IsTriggerCharacter(SourceText text, int characterPosition)
        {
            // Bring up completion when the user types a quote (i.e.: #r "), or if they type a slash
            // path separator character, or if they type a comma (#r "foo,version...").
            //
            // Also, if they're starting a word.  i.e. #r "c:\W
            var ch = text[characterPosition];
            return ch == '"' || ch == '\\' || ch == ',' ||
                CommonCompletionUtilities.IsStartingNewWord(text, characterPosition, char.IsLetter, char.IsLetterOrDigit);
        }

        internal static bool IsFilterCharacter(CompletionItem item, char ch, string textTypedSoFar)
        {
            // If the user types something that matches what is in the completion list, then just
            // count it as a filter character.

            // For example, if they type "Program " and "Program Files" is in the list, the <space>
            // should be counted as a filter character and not a commit character.
            return item.DisplayText.StartsWith(textTypedSoFar, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsCommitcharacter(CompletionItem item, char ch, string textTypedSoFar)
        {
            return ch == '"' || ch == '\\' || ch == ',';
        }

        internal static bool SendEnterThroughToEditor(CompletionItem item, string textTypedSoFar)
        {
            return false;
        }

        internal static string GetPathThroughLastSlash(string quotedPath, int quotedPathStart, int position)
        {
            Contract.ThrowIfTrue(quotedPath[0] != '"');

            const int QuoteLength = 1;

            var positionInQuotedPath = position - quotedPathStart;
            var path = quotedPath.Substring(QuoteLength, positionInQuotedPath - QuoteLength).Trim();
            var afterLastSlashIndex = AfterLastSlashIndex(path, path.Length);

            // We want the portion up to, and including the last slash if there is one.  That way if
            // the user pops up completion in the middle of a path (i.e. "C:\Win") then we'll
            // consider the path to be "C:\" and we will show appropriate completions.
            return afterLastSlashIndex >= 0 ? path.Substring(0, afterLastSlashIndex) : path;
        }

        internal static TextSpan GetTextChangeSpan(string quotedPath, int quotedPathStart, int position)
        {
            // We want the text change to be from after the last slash to the end of the quoted
            // path. If there is no last slash, then we want it from right after the start quote
            // character.
            var positionInQuotedPath = position - quotedPathStart;

            // Where we want to start tracking is right after the slash (if we have one), or else
            // right after the string starts.
            var afterLastSlashIndex = PathCompletionUtilities.AfterLastSlashIndex(quotedPath, positionInQuotedPath);
            var afterFirstQuote = 1;

            var startIndex = Math.Max(afterLastSlashIndex, afterFirstQuote);
            var endIndex = quotedPath.Length;

            // If the string ends with a quote, the we do not want to consume that.
            if (EndsWithQuote(quotedPath))
            {
                endIndex--;
            }

            return TextSpan.FromBounds(startIndex + quotedPathStart, endIndex + quotedPathStart);
        }

        internal static bool EndsWithQuote(string quotedPath)
        {
            return quotedPath.Length >= 2 && quotedPath[quotedPath.Length - 1] == '"';
        }

        /// <summary>
        /// Returns the index right after the last slash that precedes 'position'.  If there is no
        /// slash in the string, -1 is returned.
        /// </summary>
        private static int AfterLastSlashIndex(string text, int position)
        {
            // Position might be out of bounds of the string (if the string is unterminated.  Make
            // sure it's within bounds.
            position = Math.Min(position, text.Length - 1);

            int index;
            if ((index = text.LastIndexOf('/', position)) >= 0 ||
                (index = text.LastIndexOf('\\', position)) >= 0)
            {
                return index + 1;
            }

            return -1;
        }
    }
}
