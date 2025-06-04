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

        /// <summary>
        /// The underlying text being lexed/parsed.  Used to fill <see cref="_characterWindow"/>.  Can also be queried
        /// directly in rare cases where a character is needed, and is not in <see cref="_characterWindow"/> and we don't
        /// want to both pulling an entire chunk of data from it into the window.
        /// </summary>
        private readonly SourceText _text;

        /// <summary>
        /// Absolute end position of <see cref="_text"/>.
        /// </summary>
        private readonly int _textEnd;

        ///// <summary>
        ///// Backing store where we keep the most recent chunk of data read in from <see cref="SourceText"/>.  Will be resized
        ///// and replaced if a token is being read that is larger than what fits in this window.
        ///// </summary>
        //private char[] _characterWindow_doNotReadDirectly;

        /// <summary>
        /// Sub span of <see cref="_characterWindow_doNotReadDirectly"/> representing the chunk of <see cref="_text"/> that
        /// has been copied and can be read directly.  This represents the following region with <see cref="_text"/>
        /// 
        /// <code>
        /// [_characterWindowStartPositionInText, _characterWindowStartPositionInText + _characterWindow.Length)
        /// </code>
        /// 
        /// </summary>
        private ArraySegment<char> _characterWindow;

        /// <summary>
        /// Where the <see cref="_characterWindow"/> starts from inside <see cref="_text"/>
        /// </summary>
        private int _characterWindowStartPositionInText;

        /// <summary>
        /// The index within _text that the current character can be read from.
        /// </summary>
        private int _positionInText;

        public ArraySegment<char> CharacterWindow => _characterWindow;

        //private int _basis;                                // Offset of the window relative to the SourceText start.
        //private int _offset;                               // Offset from the start of the window.
        //private int _characterWindowCount;                 // # of valid characters in chars buffer

        // private int _lexemeStart;                          // Start of current lexeme relative to the window start.

        // Example for the above variables:
        // The text starts at 0.
        // The window onto the text starts at basis.
        // The current character is at (basis + offset), AKA the current "Position".
        // The current lexeme started at (basis + lexemeStart), which is <= (basis + offset)
        // The current lexeme is the characters between the lexemeStart and the offset.

        private readonly StringTable _strings;

        public SlidingTextWindow(SourceText text)
        {
            _text = text;
            //_basis = 0;
            //_offset = 0;
            _textEnd = text.Length;
            _strings = StringTable.GetInstance();
            _characterWindow = new(s_windowPool.Allocate());
            // _lexemeStart = 0;
        }

        private static void ReturnArray(char[] array)
        {
            if (array.Length == DefaultWindowLength)
                s_windowPool.Free(array);
        }

        public void Dispose()
        {
            var array = _characterWindow.Array;
            if (array != null)
            {
                ReturnArray(array);
                _characterWindow = default;
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
        private int CharacterWindowEndPositionInText => _characterWindowStartPositionInText + _characterWindow.Count;

        ///// <summary>
        ///// The current offset inside the window (relative to the window start).
        ///// </summary>
        //public int Offset
        //{
        //    get
        //    {
        //        return _offset;
        //    }
        //}

        ///// <summary>
        ///// The buffer backing the current window.
        ///// </summary>
        //public char[] CharacterWindow
        //{
        //    get
        //    {
        //        return _characterWindow;
        //    }
        //}

        /// <summary>
        /// Returns the start of the current lexeme relative to the window start.
        /// </summary>
        //public int LexemeRelativeStart
        //{
        //    get
        //    {
        //        return _lexemeStart;
        //    }
        //}

        /// <summary>
        /// Number of characters in the character window.
        /// </summary>
        //public int CharacterWindowCount
        //{
        //    get
        //    {
        //        return _characterWindowCount;
        //    }
        //}

        /// <summary>
        /// The absolute position of the start of the current lexeme in the given
        /// SourceText.
        /// </summary>
        //public int LexemeStartPosition
        //{
        //    get
        //    {
        //        return _basis + _lexemeStart;
        //    }
        //}

        /// <summary>
        /// The number of characters in the current lexeme.
        /// </summary>
        //public int Width
        //{
        //    get
        //    {
        //        return _offset - _lexemeStart;
        //    }
        //}

        ///// <summary>
        ///// Start parsing a new lexeme.
        ///// </summary>
        //public void Start()
        //{
        //    _lexemeStart = _offset;
        //}
        public void Reset(int position)
            => ResetToPositionInText(position);

        public void ResetToPositionInText(int positionInText)
        {
            // if position is within already read character range then just use what we have
            if (positionInText >= _characterWindowStartPositionInText &&
                positionInText < CharacterWindowEndPositionInText)
            {
                _positionInText = positionInText;
                return;
            }

            var array = _characterWindow.Array!;
            var amountToRead = Math.Min(_text.Length - positionInText, array.Length);
            if (amountToRead > 0)
                _text.CopyTo(_positionInText, array, 0, amountToRead);

            _characterWindow = new(array, 0, amountToRead);
            _positionInText = positionInText;
            _characterWindowStartPositionInText = positionInText;
        }

        private bool MoreChars()
        {
            if (this.PositionInText >= _textEnd)
                return false;

            //if (this.PositionInText >= this.CharacterWindowStartPositionInText && this.PositionInText < this.CharacterWindowEndPositionInText)
            //    return true;

            this.ResetToPositionInText(this.PositionInText);

            //if (_offset >= _characterWindowCount)
            //{
            //    if (this.Position >= _textEnd)
            //    {
            //        return false;
            //    }

            //    // if lexeme scanning is sufficiently into the char buffer, 
            //    // then refocus the window onto the lexeme
            //    if (_lexemeStart > (_characterWindowCount / 4))
            //    {
            //        Array.Copy(_characterWindow,
            //            _lexemeStart,
            //            _characterWindow,
            //            0,
            //            _characterWindowCount - _lexemeStart);
            //        _characterWindowCount -= _lexemeStart;
            //        _offset -= _lexemeStart;
            //        _basis += _lexemeStart;
            //        _lexemeStart = 0;
            //    }

            //    if (_characterWindowCount >= _characterWindow.Length)
            //    {
            //        // grow char array, since we need more contiguous space
            //        char[] oldWindow = _characterWindow;
            //        char[] newWindow = new char[_characterWindow.Length * 2];
            //        Array.Copy(oldWindow, 0, newWindow, 0, _characterWindowCount);
            //        s_windowPool.ForgetTrackedObject(oldWindow, newWindow);
            //        _characterWindow = newWindow;
            //    }

            //    int amountToRead = Math.Min(_textEnd - (_basis + _characterWindowCount),
            //        _characterWindow.Length - _characterWindowCount);
            //    _text.CopyTo(_basis + _characterWindowCount,
            //        _characterWindow,
            //        _characterWindowCount,
            //        amountToRead);
            //    _characterWindowCount += amountToRead;
            //    return amountToRead > 0;
            //}

            return true;
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
        {
            if (this.PositionInText >= _textEnd)
                return InvalidCharacter;

            this.ResetToPositionInText(this.PositionInText);
            return this._characterWindow.Array![this.PositionInText - this._characterWindowStartPositionInText];
        }

        /// <summary>
        /// Gets the character at the given offset to the current position if
        /// the position is valid within the SourceText.
        /// </summary>
        /// <returns>
        /// The next character if any are available. InvalidCharacter otherwise.
        /// </returns>
        public char PeekChar(int delta)
        {
            var originalPosition = this.PositionInText;
            this.AdvanceChar(delta);
            if (this.PositionInText >= _textEnd)
                return InvalidCharacter;

            this.ResetToPositionInText(this.PositionInText);
            var result = this._characterWindow.Array![this.PositionInText - this._characterWindowStartPositionInText];
            this.ResetToPositionInText(originalPosition);
            return result;
            //char ch;
            //if (_offset >= _characterWindowCount
            //    && !MoreChars())
            //{
            //    ch = InvalidCharacter;
            //}
            //else
            //{
            //    // N.B. MoreChars may update the offset.
            //    ch = _characterWindow[_offset];
            //}

            //this.Reset(position);
            //return ch;
        }

        public char PreviousChar()
        {
            Debug.Assert(this.PositionInText > 0);
            var desiredPosition = this.PositionInText - 1;
            if (desiredPosition >= this.CharacterWindowStartPositionInText && desiredPosition < this.CharacterWindowEndPositionInText)
                return this._characterWindow.Array![this.PositionInText - this._characterWindowStartPositionInText];

            // The prior character isn't in the window (trying to read the current character caused us to
            // read in the next chunk of text into the window, throwing out the preceding characters).
            // Just go back to the source text to find this character.  While more expensive, this should
            // be rare given that most of the time we won't be calling this right after loading a new text
            // chunk.
            return this.Text[this.PositionInText - 1];
        }

        public char CharAt(int positionInText)
        {
            if (positionInText >= _textEnd)
                return InvalidCharacter;

            this.ResetToPositionInText(positionInText);
            return this._characterWindow.Array![this.PositionInText - this._characterWindowStartPositionInText];
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

        //public string GetInternedText()
        //{
        //    return this.Intern(_characterWindow, _lexemeStart, this.Width);
        //}

        //public string GetText(bool intern)
        //{
        //    return this.GetText(this.LexemeStartPosition, this.Width, intern);
        //}
        //public string GetInternedText(int )
        //{
        //    return this.Intern(_characterWindow, _lexemeStart, this.Width);
        //}

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
                    if (CharAt(position) == '/' && CharAt(position + 1) == '/' && CharAt(position + 1) == ' ')
                        return "// ";

                    break;
            }

            if (start >= this.CharacterWindowStartPositionInText && end <= this.CharacterWindowEndPositionInText)
            {
                var offset = position - this.CharacterWindowStartPositionInText;
                var array = this._characterWindow.Array!;
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

        //public TestAccessor GetTestAccessor()
        //    => new(this);

        //public readonly struct TestAccessor(SlidingTextWindow window)
        //{
        //    private readonly SlidingTextWindow _window = window;

        //    public void SetDefaultCharacterWindow()
        //        => _window._characterWindow = new char[DefaultWindowLength];
        //}
    }
}
