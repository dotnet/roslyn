// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    /// <summary>
    /// Keeps a sliding buffer over the SourceText of a file for the lexer.  Keeps a chunk of a <see cref="SourceText"/> in contiguous
    /// span of characters for easy access during lexing.
    /// </summary>
    internal sealed class SlidingTextWindow : IDisposable
    {
        private static readonly ObjectPool<char[]> s_windowPool = new ObjectPool<char[]>(() => new char[DefaultWindowLength]);

        /// <summary>
        /// In many cases, e.g. PeekChar, we need the ability to indicate that there are
        /// no characters left and we have reached the end of the stream, or some other
        /// invalid or not present character was asked for. Due to perf concerns, things
        /// like nullable or out variables are not viable. Instead we need to choose a
        /// char value which can never be legal.
        /// 
        /// In .NET, all characters are represented in 16 bits using the UTF-16 encoding.
        /// Fortunately for us, there are a variety of different bit patterns which
        /// are *not* legal UTF-16 characters. 0xffff (char.MaxValue) is one of these
        /// characters -- a legal Unicode code point, but not a legal UTF-16 bit pattern.
        /// </summary>
        public const char InvalidCharacter = char.MaxValue;

        private const int DefaultWindowLength = 2048;

        private readonly StringTable _strings;

        /// <summary>
        /// The underlying text being lexed/parsed.  Used to fill <see cref="CharacterWindow"/>.  Can also be queried
        /// directly in rare cases where a character is needed, and is not in <see cref="CharacterWindow"/> and we don't
        /// want to both pulling an entire chunk of data from it into the window.
        /// </summary>
        private readonly SourceText _text;

        /// <summary>
        /// Absolute end position of <see cref="_text"/>.
        /// </summary>
        private readonly int _textEnd;

        /// <summary>
        /// Current chunk of <see cref="_text"/> that is available to read as a low-overhead, contiguous span of characters.
        /// This represents the following region with <see cref="_text"/>
        /// 
        /// <code>
        /// [_characterWindowStartPositionInText, _characterWindowStartPositionInText + _characterWindow.Length)
        /// </code>
        /// 
        /// </summary>
        /// <remarks>
        /// Lexeme text within this span can be read directly from this array.  However, if the span to read goes out of bounds
        /// of the chunk of <see cref="_text"/> this corresponds to, then the Lexeme text to read will be fetched from the 
        /// <see cref="_text"/> value instead.
        /// </remarks>
        public ArraySegment<char> CharacterWindow { get; private set; }

        /// <summary>
        /// Where the <see cref="CharacterWindow"/> starts from inside <see cref="_text"/>
        /// </summary>
        private int _characterWindowStartPositionInText;

        /// <summary>
        /// The index within _text that the current character can be read from.
        /// </summary>
        private int _positionInText;

        public SlidingTextWindow(SourceText text)
        {
            _text = text;
            _textEnd = text.Length;
            _strings = StringTable.GetInstance();
            CharacterWindow = new(s_windowPool.Allocate());

            // Read the first chunk of the file into the character window.
            this.ReadChunkAt(0);
        }

        private static void ReturnArray(char[] array)
        {
            if (array.Length == DefaultWindowLength)
            {
                Array.Clear(array, 0, array.Length);
                s_windowPool.Free(array);
            }
        }

        public void Dispose()
        {
            var array = CharacterWindow.Array;
            if (array != null)
            {
                ReturnArray(array);
                CharacterWindow = default;
                _strings.Free();
            }
        }

        public SourceText Text => _text;

        /// <summary>
        /// The current absolute position in the text file.
        /// </summary>
        public int PositionInText => _positionInText;
        public int Position => PositionInText;

        public int CharacterWindowStartPositionInText => _characterWindowStartPositionInText;
        public int CharacterWindowEndPositionInText => _characterWindowStartPositionInText + CharacterWindow.Count;

        public void Reset(int positionInText)
        {
            positionInText = Math.Min(positionInText, _textEnd);

            _positionInText = positionInText;

            // if position is within already read character range then just use what we have
            if (_positionInText >= CharacterWindowStartPositionInText &&
                _positionInText < CharacterWindowEndPositionInText)
            {
                return;
            }

            ReadChunkAt(positionInText);
        }

        private void ReadChunkAt(int positionInText)
        {
            var array = CharacterWindow.Array!;
            var amountToRead = Math.Min(_text.Length - positionInText, array.Length);
            if (amountToRead > 0)
                _text.CopyTo(positionInText, array, 0, amountToRead);

            CharacterWindow = new(array, 0, amountToRead);
            _characterWindowStartPositionInText = positionInText;
        }

        public char CharAt(int positionInText)
        {
            // If it's not in the source text at all, return an invalid char.  This allows us to easily index without
            // having to do length checks.
            if (positionInText < 0 || positionInText >= _textEnd)
                return InvalidCharacter;

            // If the position is outside of the chunk we're currently pointing to, read in a chunk starting at the
            // location we're trying to read at.
            if (positionInText < CharacterWindowStartPositionInText ||
                positionInText >= CharacterWindowEndPositionInText)
            {
                this.ReadChunkAt(positionInText);
            }

            Debug.Assert(positionInText >= CharacterWindowStartPositionInText && positionInText < CharacterWindowEndPositionInText);
            return this.CharacterWindow.Array![positionInText - this.CharacterWindowStartPositionInText];
        }

        /// <summary>
        /// After reading <see cref=" InvalidCharacter"/>, a consumer can determine
        /// if the InvalidCharacter was in the user's source or a sentinel.
        /// 
        /// Comments and string literals are allowed to contain any Unicode character.
        /// </summary>
        /// <returns></returns>
        internal bool IsReallyAtEnd()
            => this.PositionInText >= _textEnd;

        /// <summary>
        /// Advance the current position by one. No guarantee that this
        /// position is valid.
        /// </summary>
        public void AdvanceChar()
            => _positionInText++;

        /// <summary>
        /// Advances the text window if it currently pointing at the <paramref name="c"/> character.  Returns <see
        /// langword="true"/> if it did advance, <see langword="false"/> otherwise.
        /// </summary>
        public bool TryAdvance(char c)
        {
            if (PeekChar() != c)
                return false;

            AdvanceChar();
            return true;
        }

        /// <summary>
        /// Advance the current position by n. No guarantee that this position
        /// is valid.
        /// </summary>
        public void AdvanceChar(int n)
            => _positionInText += n;

        /// <summary>
        /// Moves past the newline that the text window is currently pointing at.  The text window must be pointing at a
        /// newline.  If the newline is <c>\r\n</c> then that entire sequence will be skipped.  Otherwise, the text
        /// window will only advance past a single character.
        /// </summary>
        public void AdvancePastNewLine()
            => AdvanceChar(GetNewLineWidth());

        /// <summary>
        /// Gets the length of the newline the text window must be pointing at here.  For <c>\r\n</c> this is <c>2</c>,
        /// for everything else, this is <c>1</c>.
        /// </summary>
        public int GetNewLineWidth()
        {
            Debug.Assert(SyntaxFacts.IsNewLine(this.PeekChar()));
            return GetNewLineWidth(this.PeekChar(), this.PeekChar(1));
        }

        public static int GetNewLineWidth(char currentChar, char nextChar)
        {
            Debug.Assert(SyntaxFacts.IsNewLine(currentChar));
            return currentChar == '\r' && nextChar == '\n' ? 2 : 1;
        }

        /// <summary>
        /// Grab the next character and advance the position.
        /// </summary>
        /// <returns>
        /// The next character, <see cref="InvalidCharacter" /> if there were no characters 
        /// remaining.
        /// </returns>
        public char NextChar()
        {
            char c = PeekChar();
            if (c != InvalidCharacter)
            {
                this.AdvanceChar();
            }
            return c;
        }

        /// <summary>
        /// Gets the next character if there are any characters in the 
        /// SourceText. May advance the window if we are at the end.
        /// </summary>
        /// <returns>
        /// The next character if any are available. InvalidCharacter otherwise.
        /// </returns>
        public char PeekChar()
            => this.CharAt(this.Position);

        /// <summary>
        /// Gets the character at the given offset to the current position if
        /// the position is valid within the SourceText.
        /// </summary>
        /// <returns>
        /// The next character if any are available. InvalidCharacter otherwise.
        /// </returns>
        public char PeekChar(int delta)
            => this.CharAt(this.Position + delta);

        public char PreviousChar()
            => this.PeekChar(-1);

        public char PreviousChar()
        {
            Debug.Assert(this.Position > 0);
            if (_offset > 0)
            {
                // The allowed region of the window that can be read is from 0 to _characterWindowCount (which _offset
                // is in between).  So as long as _offset is greater than 0, we can read the previous character directly
                // from the current chunk of characters in the window.
                return this.CharacterWindow[_offset - 1];
            }

            // The prior character isn't in the window (trying to read the current character caused us to
            // read in the next chunk of text into the window, throwing out the preceding characters).
            // Just go back to the source text to find this character.  While more expensive, this should
            // be rare given that most of the time we won't be calling this right after loading a new text
            // chunk.
            return this.Text[this.Position - 1];
        }

        /// <summary>
        /// If the next characters in the window match the given string,
        /// then advance past those characters.  Otherwise, do nothing.
        /// </summary>
        internal bool AdvanceIfMatches(string desired)
        {
            int length = desired.Length;

            for (int i = 0; i < length; i++)
            {
                if (PeekChar(i) != desired[i])
                {
                    return false;
                }
            }

            AdvanceChar(length);
            return true;
        }

        public string Intern(StringBuilder text)
        {
            return _strings.Add(text);
        }

        public string Intern(char[] array, int start, int length)
        {
            return _strings.Add(array, start, length);
        }

        public string GetText(int startPosition, bool intern)
        {
            return this.GetText(startPosition, this.Position - startPosition, intern);
        }

        public string GetText(int position, int length, bool intern)
        {
            var start = position;
            var end = start + length;

            // PERF: Whether interning or not, there are some frequently occurring
            // easy cases we can pick off easily.
            switch (length)
            {
                case 0: return string.Empty;

                case 1:
                    {
                        var character = CharAt(position);
                        if (character == ' ')
                            return " ";

                        if (character == '\n')
                            return "\n";
                    }

                    break;

                case 2:
                    var firstChar = CharAt(position);
                    if (firstChar == '\r' && CharAt(position + 1) == '\n')
                        return "\r\n";

                    if (firstChar == '/' && CharAt(position + 1) == '/')
                        return "//";

                    break;

                case 3:
                    if (CharAt(position) == '/' && CharAt(position + 1) == '/' && CharAt(position + 2) == ' ')
                        return "// ";

                    break;
            }

            if (start >= this.CharacterWindowStartPositionInText && end <= this.CharacterWindowEndPositionInText)
            {
                var offset = position - this.CharacterWindowStartPositionInText;
                var array = this.CharacterWindow.Array!;
                return intern
                    ? this.Intern(array, offset, length)
                    : new string(array, offset, length);
            }

            // Text crosses beyond what the character window directly holds.  Just go to the underlying source text.
            return _text.ToString(new TextSpan(position, length));
        }

        internal static char GetCharsFromUtf32(uint codepoint, out char lowSurrogate)
        {
            if (codepoint < (uint)0x00010000)
            {
                lowSurrogate = InvalidCharacter;
                return (char)codepoint;
            }
            else
            {
                Debug.Assert(codepoint > 0x0000FFFF && codepoint <= 0x0010FFFF);
                lowSurrogate = (char)((codepoint - 0x00010000) % 0x0400 + 0xDC00);
                return (char)((codepoint - 0x00010000) / 0x0400 + 0xD800);
            }
        }

        internal TestAccessor GetTestAccessor()
            => new TestAccessor(this);

        internal readonly struct TestAccessor(SlidingTextWindow window)
        {
            private readonly SlidingTextWindow _window = window;

            internal void SetDefaultCharacterWindow()
                => _window._characterWindow = new char[DefaultWindowLength];
        }
    }
}
