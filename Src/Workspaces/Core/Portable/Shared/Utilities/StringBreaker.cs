// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal static class StringBreaker
    {
        /// <summary>
        /// Breaks an identifier string into constituent parts.
        /// </summary>
        public static List<TextSpan> BreakIntoCharacterParts(string identifier)
        {
            return BreakIntoParts(identifier, word: false);
        }

        /// <summary>
        /// Breaks an identifier string into constituent parts.
        /// </summary>
        public static List<TextSpan> BreakIntoWordParts(string identifier)
        {
            return BreakIntoParts(identifier, word: true);
        }

        public static List<TextSpan> BreakIntoParts(string identifier, bool word)
        {
            var result = new List<TextSpan>();

            int wordStart = 0;
            for (int i = 1; i < identifier.Length; i++)
            {
                var lastIsDigit = char.IsDigit(identifier[i - 1]);
                var currentIsDigit = char.IsDigit(identifier[i]);

                var transitionFromLowerToUpper = TransitionFromLowerToUpper(identifier, word, i);
                var transitionFromUpperToLower = TransitionFromUpperToLower(identifier, word, i, wordStart);

                if (char.IsPunctuation(identifier[i - 1]) ||
                    char.IsPunctuation(identifier[i]) ||
                    lastIsDigit != currentIsDigit ||
                    transitionFromLowerToUpper ||
                    transitionFromUpperToLower)
                {
                    if (!IsAllPunctuation(identifier, wordStart, i))
                    {
                        result.Add(new TextSpan(wordStart, i - wordStart));
                    }

                    wordStart = i;
                }
            }

            if (!IsAllPunctuation(identifier, wordStart, identifier.Length))
            {
                result.Add(new TextSpan(wordStart, identifier.Length - wordStart));
            }

            return result;
        }

        private static bool IsAllPunctuation(string identifier, int start, int end)
        {
            for (int i = start; i < end; i++)
            {
                var ch = identifier[i];

                // We don't consider _ as punctuation as there may be things with that name.
                if (!char.IsPunctuation(ch) || ch == '_')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TransitionFromUpperToLower(string identifier, bool word, int index, int wordStart)
        {
            if (word)
            {
                // Cases this supports:
                // 1) IDisposable -> I, Disposable
                // 2) UIElement -> UI, Element
                // 3) HTMLDocument -> HTML, Document
                //
                // etc.
                if (index != wordStart &&
                    index + 1 < identifier.Length)
                {
                    var currentIsUpper = char.IsUpper(identifier[index]);
                    var nextIsLower = char.IsLower(identifier[index + 1]);
                    if (currentIsUpper && nextIsLower)
                    {
                        // We have a transition from an upper to a lower letter here.  But we only
                        // want to break if all the letters that preceded are uppercase.  i.e. if we
                        // have "Foo" we don't want to break that into "F, oo".  But if we have
                        // "IFoo" or "UIFoo", then we want to break that into "I, Foo" and "UI,
                        // Foo".  i.e. the last uppercase letter belongs to the lowercase letters
                        // that follows.  Note: this will make the following not split properly:
                        // "HELLOthere".  However, these sorts of names do not show up in .Net
                        // programs.
                        for (int i = wordStart; i < index; i++)
                        {
                            if (!char.IsUpper(identifier[i]))
                            {
                                return false;
                            }
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TransitionFromLowerToUpper(string identifier, bool word, int index)
        {
            var lastIsUpper = char.IsUpper(identifier[index - 1]);
            var currentIsUpper = char.IsUpper(identifier[index]);

            // See if the casing indicates we're starting a new word. Note: if we're breaking on
            // words, then just seeing an upper case character isn't enough.  Instead, it has to
            // be uppercase and the previous character can't be uppercase. 
            //
            // For example, breaking "AddMetadata" on words would make: Add Metadata
            //
            // on characters would be: A dd M etadata
            //
            // Break "AM" on words would be: AM
            //
            // on characters would be: A M
            //
            // We break the search string on characters.  But we break the symbol name on words.
            var transition = word
                ? (currentIsUpper && !lastIsUpper)
                : currentIsUpper;
            return transition;
        }
    }
}