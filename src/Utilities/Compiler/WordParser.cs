// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Text;

namespace Analyzer.Utilities
{
    /// <summary>
    ///     Provides <see langword="static"/> methods for parsing words from text.
    /// </summary>
    internal class WordParser
    {
        // WordParser has two distinct modes; one where it breaks up only words in 
        // a given piece of text, and the other where it breaks up both words 
        // and individual compounds within words in a piece of text. Passing 
        // WordParserOptions.None to the constructor (or Parse) causes it to enter 
        // the former, and WordParserOptions.SplitCompoundWords the later.
        //
        // If you simply want to iterate over the words, you can avoid the 
        // allocation of a Collection<String> if you manually construct WordParser
        // and use the NextWord method instead of using the static Parse method.
        //
        // [char]:      Represents any Unicode character
        // [A-Z]:       Represents any Unicode uppercase letter
        // [a-z]:       Represents any Unicode lowercase letter
        // [0-9]:       Represents the numbers 0 to 9
        // [letter]:    Represents any Unicode letter
        //
        // <words>      -> <prefix>(<word> | <notword>)+
        // 
        // <notword>    -> !<word>
        //
        // <prefix>     -> [char]
        //
        // WordParserOptions.None:
        // <word>       -> ([0-9] | [letter])+
        //
        // WordParserOptions.SplitCompoundWords:
        // <word>       -> <numeric> | <uppercase> | <lowercase> | <nocase> | <csshex>
        // <numeric>    -> (<integer> | <hex>)
        // <integer>    -> [0-9]+
        // <hex>        -> [0x]([0-9] | [A-F] | [a-f])+
        // <uppercase>  -> ([A-Z](<lowercase> | <allcaps>)) | <allcaps>
        // <lowercase>  -> [a-z]+
        // <nocase>     -> ([letter] ![A-Z] ![a-z])+
        // <allcaps>    -> [A-Z]+(s) (unless next character is [a-z])
        // <csshex>     -> [#]([0-9] | [A-F] | [a-f])+

        private const char NullChar = '\0';
        private readonly WordParserOptions _options;
        private readonly StringBuilder _buffer;
        private readonly string _text;
        private string? _peekedWord;
        private int _index;
        private char _prefix;

        /// <summary>
        ///     Initializes a new instance of the <see cref="WordParser"/> class with the specified text and options.
        /// </summary>
        /// <param name="text">
        ///     A <see cref="String"/> containing the text to parse.
        /// </param>
        /// <param name="options">
        ///     One or more of the <see cref="WordParserOptions"/> specifying parsing and delimiting options.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="text"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="options"/> is not one or more of the <see cref="WordParserOptions"/> values.
        /// </exception>
        public WordParser(string text, WordParserOptions options) : this(text, options, NullChar)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="WordParser"/> class with the specified text, options and prefix.
        /// </summary>
        /// <param name="text">
        ///     A <see cref="String"/> containing the text to parse.
        /// </param>
        /// <param name="options">
        ///     One or more of the <see cref="WordParserOptions"/> specifying parsing and delimiting options.
        /// </param>
        /// <param name="prefix">
        ///     A <see cref="Char"/> representing an optional prefix of <paramref name="text"/>, that if present,
        ///     will be returned as a separate token.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="text"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="options"/> is not one or more of the <see cref="WordParserOptions"/> values.
        /// </exception>
        public WordParser(string text, WordParserOptions options, char prefix)
        {
            if (options < WordParserOptions.None || options > (WordParserOptions.IgnoreMnemonicsIndicators | WordParserOptions.SplitCompoundWords))
            {
                throw new ArgumentException($"'{nameof(options)}' ({((int)options).ToString()}) is invalid for Enum type'{typeof(WordParserOptions).Name}'");
            }

            _text = text ?? throw new ArgumentNullException(nameof(text));
            _options = options;
            _buffer = new StringBuilder(text.Length);
            _prefix = prefix;
        }

        private bool SkipMnemonics =>
            (_options & WordParserOptions.IgnoreMnemonicsIndicators) == WordParserOptions.IgnoreMnemonicsIndicators;

        private bool SplitCompoundWords =>
            (_options & WordParserOptions.SplitCompoundWords) == WordParserOptions.SplitCompoundWords;

        /// <summary>
        ///     Returns the words contained in the specified text, delimiting based on the specified options.
        /// </summary>
        /// <param name="text">
        ///     A <see cref="String"/> containing the text to parse.
        /// </param>
        /// <param name="options">
        ///     One or more of the <see cref="WordParserOptions"/> specifying parsing and delimiting options.
        /// </param>
        /// <returns>
        ///     A <see cref="Collection{T}"/> of strings containing the words contained in <paramref name="text"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="text"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="options"/> is not one or more of the <see cref="WordParserOptions"/> values.
        /// </exception>
        internal static Collection<string> Parse(string text, WordParserOptions options)
        {
            return Parse(text, options, NullChar);
        }

        /// <summary>
        ///     Returns the words contained in the specified text, delimiting based on the specified options.
        /// </summary>
        /// <param name="text">
        ///     A <see cref="String"/> containing the text to parse.
        /// </param>
        /// <param name="options">
        ///     One or more of the <see cref="WordParserOptions"/> specifying parsing and delimiting options.
        /// </param>
        /// <param name="prefix">
        ///     A <see cref="Char"/> representing an optional prefix of <paramref name="text"/>, that if present,
        ///     will be returned as a separate token.
        /// </param>
        /// <returns>
        ///     A <see cref="Collection{T}"/> of strings containing the words contained in <paramref name="text"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="text"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="options"/> is not one or more of the <see cref="WordParserOptions"/> values.
        /// </exception>
        internal static Collection<string> Parse(string text, WordParserOptions options, char prefix)
        {
            WordParser parser = new WordParser(text, options, prefix);
            Collection<string> words = new Collection<string>();

            string? word;
            while ((word = parser.NextWord()) != null)
            {
                words.Add(word);
            }

            return words;
        }

        /// <summary>
        ///     Returns a value indicating whether at least one of the specified words occurs, using a case-insensitive ordinal comparison, within the specified text.
        /// </summary>
        /// <param name="text">
        ///     A <see cref="String"/> containing the text to check.
        /// </param>    
        /// <param name="options">
        ///     One or more of the <see cref="WordParserOptions"/> specifying parsing and delimiting options.
        /// </param>
        /// <param name="words">
        ///     A <see cref="String"/> array containing the words to seek.
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if at least one of the elements within <paramref name="words"/> occurs within <paramref name="text"/>, otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="text"/> is <see langword="null"/>.
        ///     <para>
        ///      -or-  
        ///     </para>
        ///     <paramref name="words"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="options"/> is not one or more of the <see cref="WordParserOptions"/> values.
        /// </exception>
        public static bool ContainsWord(string text, WordParserOptions options, ImmutableArray<string> words)
        {
            return ContainsWord(text, options, NullChar, words);
        }

        /// <summary>
        ///     Returns a value indicating whether at least one of the specified words occurs, using a case-insensitive ordinal comparison, within the specified text.
        /// </summary>
        /// <param name="text">
        ///     A <see cref="String"/> containing the text to check.
        /// </param>    
        /// <param name="options">
        ///     One or more of the <see cref="WordParserOptions"/> specifying parsing and delimiting options.
        /// </param>
        /// <param name="prefix">
        ///     A <see cref="Char"/> representing an optional prefix of <paramref name="text"/>, that if present,
        ///     will be returned as a separate token.
        /// </param>
        /// <param name="words">
        ///     A <see cref="String"/> array containing the words to seek.
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if at least one of the elements within <paramref name="words"/> occurs within <paramref name="text"/>, otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="text"/> is <see langword="null"/>.
        ///     <para>
        ///      -or-  
        ///     </para>
        ///     <paramref name="words"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="options"/> is not one or more of the <see cref="WordParserOptions"/> values.
        /// </exception>
        internal static bool ContainsWord(string text, WordParserOptions options, char prefix, ImmutableArray<string> words)
        {
            if (words.IsDefault)
            {
                throw new ArgumentNullException(nameof(words));
            }

            WordParser parser = new WordParser(text, options, prefix);

            string? parsedWord;
            while ((parsedWord = parser.NextWord()) != null)
            {
                foreach (string word in words)
                {
                    if (string.Equals(parsedWord, word, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///     Returns the next word in the text.
        /// </summary>
        /// <returns>
        ///     A <see cref="String"/> containing the next word or <see langword="null"/> if there are no more words.
        /// </returns>
        public string? NextWord()
        {
            if (_peekedWord == null)
            {
                return NextWordCore();
            }

            string? word = _peekedWord;
            _peekedWord = null;
            return word;
        }

        /// <summary>
        ///     Returns the next word in the text without consuming it.
        /// </summary>
        /// <returns>
        ///     A <see cref="String"/> containing the next word or <see langword="null"/> if there are no more words.
        /// </returns>
        public string? PeekWord()
        {
            if (_peekedWord == null)
            {
                _peekedWord = NextWordCore();
            }

            return _peekedWord;
        }

        private string? NextWordCore()
        {
            // Reset buffer
            _buffer.Length = 0;

            if (ParseNext())
            { // We parsed something
                return _buffer.ToString();
            }

            return null;
        }

        private bool ParseNext()
        {
            if (TryParsePrefix())
            {   // Try parse the prefix ie 'I' in 'IInterface'.
                return true;
            }

            char c;
            char punctuation = NullChar;

            while ((c = Peek()) != NullChar)
            {
                if (!TryParseWord(c))
                {
                    if (punctuation != NullChar)
                    { // Intra-word punctuation next to unrecognized character ie 'Foo-?'
                        Unread();
                        Skip();
                        return true;
                    }

                    // Unrecognized character, ignore
                    Skip();
                    continue;
                }

                c = Peek();

                if (IsIntraWordPunctuation(c))
                { // Intra-word punctuation ie '-' in 'Foo-Bar'
                    punctuation = c;
                    Read();
                    continue;
                }

                // We parsed something
                return true;
            }

            if (punctuation != NullChar)
            {   // Ends with intra-word punctuation ie '-' in 'Foo-'
                Unread();
                return true;
            }

            return false;
        }

        private bool TryParseWord(char c)
        {
            if (SplitCompoundWords)
            {   // Parse both whole and compound words
                if (IsUpper(c))
                {   // 'ALLCAPS' or 'PascalCased'
                    ParseUppercase();
                    return true;
                }

                if (IsLower(c))
                {   // 'foo'
                    ParseLowercase();
                    return true;
                }

                if (IsDigit(c))
                {   // '123' or '0xABCDEF'
                    ParseNumeric();
                    return true;
                }

                if (IsLetterWithoutCase(c))
                {   // ie Japanese characters
                    ParseWithoutCase();
                    return true;
                }

                if (c == '#' && IsHexDigit(Peek(2)))
                {   // '#ABC123'
                    ParseHex();
                    return true;
                }
            }
            else if (IsLetterOrDigit(c))
            {   // Parse only whole words
                ParseWholeWord();
                return true;
            }

            // Unrecognized character
            return false;
        }

        private bool TryParsePrefix()
        {
            if (_prefix == NullChar)
            {
                return false;
            }

            char c = Peek();

            if (c == _prefix)
            {
                c = Peek(2);

                if (!IsLower(c))
                {   // 'IInterface' or 'T1', but not 'Interface', or 'Type'
                    // Consume the prefix
                    Read();

                    // We do not want to try and read the prefix again
                    _prefix = NullChar;
                    return true;
                }
            }

            // We do not want to try and read the prefix again
            _prefix = NullChar;
            return false;
        }

        private void ParseWholeWord()
        {
            char c;
            do
            {
                Read();
                c = Peek();
            }
            while (IsLetterOrDigit(c));
        }

        private void ParseInteger()
        {
            char c;
            do
            {
                Read();
                c = Peek();
            }
            while (IsDigit(c));
        }

        private void ParseHex()
        {
            char c;
            do
            {
                Read();
                c = Peek();
            }
            while (IsHexDigit(c));
        }

        private void ParseNumeric()
        {
            char c = Peek();

            if (c == '0')
            {
                c = Peek(2);

                if ((c == 'x' || c == 'X') && IsHexDigit(Peek(3)))
                {   // '0xA' or '0XA'
                    Read(); // Consume '0'
                    Read(); // Consume 'x' or 'X'

                    ParseHex();
                    return;
                }
            }

            ParseInteger();
        }

        private void ParseLowercase()
        {
            char c;
            do
            {
                Read();
                c = Peek();
            }
            while (IsLower(c));
        }

        private void ParseUppercase()
        {
            Read();

            char c = Peek();

            if (IsUpper(c))
            {   // 'ALLCAPS'
                ParseAllCaps();
            }
            else if (IsLower(c))
            {   // 'PascalCased'
                ParseLowercase();
            }
        }

        private void ParseWithoutCase()
        {   // Parses letters without any concept of case, 
            // ie Japanese

            char c;
            do
            {
                Read();
                c = Peek();
            }
            while (IsLetterWithoutCase(c));
        }

        private void ParseAllCaps()
        {
            char c;

            // Optimistically consume all consecutive uppercase letters
            do
            {
                Read();
                c = Peek();
            }
            while (IsUpper(c));

            // Optimistically consume a trailing 's'
            if (c == 's')
            {
                Read();
                c = Peek();
            }

            // Reject the final uppercase letter (and trailing 's') 
            // if they are followed by a lower case letter.
            while (IsLower(c))
            {
                Unread();
                c = Peek();
            }
        }

        private void Read()
        {
            char c = Peek();
            _buffer.Append(c);
            Skip();
        }

        private void Skip()
        {
            while (_index < _text.Length)
            {
                char c = _text[_index++];

                if (!IsIgnored(c))
                {
                    break;
                }
            }
        }

        private char Peek()
        {
            return Peek(1);
        }

        private char Peek(int lookAhead)
        {
            for (int index = _index; index < _text.Length; index++)
            {
                char c = _text[index];

                if (IsIgnored(c))
                {
                    continue;
                }

                if (--lookAhead == 0)
                {
                    return c;
                }
            }

            return NullChar;
        }

        private void Unread()
        {
            while (_index >= 0)
            {
                char c = _text[--_index];

                if (!IsIgnored(c))
                {
                    break;
                }
            }

            _buffer.Length--;
        }

        private bool IsIgnored(char c)
        {   // TODO: We should extend this to handle 'real' mnemonics, 
            // instead of just blindly skipping all ampersands and 
            // underscores.For example, '&&OK' should really be 
            // interpreted as '&OK', instead of 'OK'.
            if (SkipMnemonics)
            {
                return c == '&' || c == '_';
            }

            return false;
        }

        private static bool IsLower(char c)
        {
            return char.IsLower(c);
        }

        private static bool IsUpper(char c)
        {
            return char.IsUpper(c);
        }

        private static bool IsLetterOrDigit(char c)
        {
            return char.IsLetterOrDigit(c);
        }

        private static bool IsLetterWithoutCase(char c)
        {
            if (char.IsLetter(c) && !char.IsUpper(c))
            {
                return !char.IsLower(c);
            }

            return false;
        }

        private static bool IsDigit(char c)
        {
            return char.IsDigit(c);
        }

        private static bool IsHexDigit(char c)
        {
            switch (c)
            {
                case 'A':
                case 'a':
                case 'B':
                case 'b':
                case 'C':
                case 'c':
                case 'D':
                case 'd':
                case 'E':
                case 'e':
                case 'F':
                case 'f':
                    return true;
            }

            return IsDigit(c);
        }

        private static bool IsIntraWordPunctuation(char c)
        {   // Don't be tempted to add En dash and Em dash to this
            // list, as these should be treated as word delimiters.

            switch (c)
            {
                case '-':
                case '\u00AD': // Soft hyphen
                case '\'':
                case '\u2019': // Right Single Quotation Mark
                    return true;
            }

            return false;
        }
    }
}
