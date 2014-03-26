// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    /// <summary>
    /// Keeps a sliding buffer over the SourceText of a file for the lexer. Also
    /// provides the lexer with the ability to keep track of a current "lexeme"
    /// by leaving a marker and advancing ahead the offset. The lexer can then
    /// decide to "keep" the lexeme by erasing the marker, or abandon the current
    /// lexeme by moving the offset back to the marker.
    /// </summary>
    internal sealed class SlidingTextWindow : IDisposable
    {
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

        private readonly SourceText text;                 // Source of text to parse.
        private int basis;                                // Offset of the window relative to the SourceText start.
        private int offset;                               // Offset from the start of the window.
        private readonly int textEnd;                     // Absolute end position
        private char[] characterWindow;                   // Moveable window of chars from source text
        private int characterWindowCount;                 // # of valid characters in chars buffer

        private int lexemeStart;                          // Start of current lexeme relative to the window start.

        // Example for the above variables:
        // The text starts at 0.
        // The window onto the text starts at basis.
        // The current character is at (basis + offset), AKA the current "Position".
        // The current lexeme started at (basis + lexemeStart), which is <= (basis + offset)
        // The current lexeme is the characters between the lexemeStart and the offset.

        private readonly StringTable strings;

        private static readonly ObjectPool<char[]> windowPool = new ObjectPool<char[]>(() => new char[DefaultWindowLength]);

        public SlidingTextWindow(SourceText text)
        {
            this.text = text;
            this.basis = 0;
            this.offset = 0;
            this.textEnd = text.Length;
            this.strings = StringTable.GetInstance();
            this.characterWindow = windowPool.Allocate();
            this.lexemeStart = 0;
        }

        public void Dispose()
        {
            if (this.characterWindow != null)
            {
                windowPool.Free(this.characterWindow);
                this.characterWindow = null;
                this.strings.Free();
            }
        }

        public SourceText Text
        {
            get
            {
                return this.text;
            }
        }

        /// <summary>
        /// The current absolute position in the text file.
        /// </summary>
        public int Position
        {
            get
            {
                return this.basis + this.offset;
            }
        }

        /// <summary>
        /// The current offset inside the window (relative to the window start).
        /// </summary>
        public int Offset
        {
            get
            {
                return this.offset;
            }
        }

        /// <summary>
        /// The buffer backing the current window.
        /// </summary>
        public char[] CharacterWindow
        {
            get
            {
                return this.characterWindow;
            }
        }

        /// <summary>
        /// Returns the start of the current lexeme relative to the window start.
        /// </summary>
        public int LexemeRelativeStart
        {
            get
            {
                return this.lexemeStart;
            }
        }

        /// <summary>
        /// Number of characters in the character window.
        /// </summary>
        public int CharacterWindowCount
        {
            get
            {
                return this.characterWindowCount;
            }
        }

        /// <summary>
        /// The absolute position of the start of the current lexeme in the given
        /// SourceText.
        /// </summary>
        public int LexemeStartPosition
        {
            get
            {
                return this.basis + this.lexemeStart;
            }
        }

        /// <summary>
        /// The number of characters in the current lexeme.
        /// </summary>
        public int Width
        {
            get
            {
                return this.offset - this.lexemeStart;
            }
        }

        /// <summary>
        /// Start parsing a new lexeme.
        /// </summary>
        public void Start()
        {
            this.lexemeStart = this.offset;
        }

        public void Reset(int position)
        {
            // if position is within already read character range then just use what we have
            int relative = position - this.basis;
            if (relative >= 0 && relative <= this.characterWindowCount)
            {
                this.offset = relative;
            }
            else
            {
                // we need to reread text buffer
                int amountToRead = Math.Min(this.text.Length, position + this.characterWindow.Length) - position;
                amountToRead = Math.Max(amountToRead, 0);
                if (amountToRead > 0)
                {
                    this.text.CopyTo(position, this.characterWindow, 0, amountToRead);
                }

                this.lexemeStart = 0;
                this.offset = 0;
                this.basis = position;
                this.characterWindowCount = amountToRead;
            }
        }

        private bool MoreChars()
        {
            if (this.offset >= this.characterWindowCount)
            {
                if (this.Position >= this.textEnd)
                {
                    return false;
                }

                // if lexeme scanning is sufficiently into the char buffer, 
                // then refocus the window onto the lexeme
                if (this.lexemeStart > (this.characterWindowCount / 4))
                {
                    Array.Copy(this.characterWindow,
                        this.lexemeStart,
                        this.characterWindow,
                        0,
                        characterWindowCount - lexemeStart);
                    this.characterWindowCount -= lexemeStart;
                    this.offset -= lexemeStart;
                    this.basis += this.lexemeStart;
                    this.lexemeStart = 0;
                }

                if (this.characterWindowCount >= this.characterWindow.Length)
                {
                    // grow char array, since we need more contiguous space
                    char[] oldWindow = this.characterWindow;
                    char[] newWindow = new char[this.characterWindow.Length * 2];
                    Array.Copy(oldWindow, 0, newWindow, 0, this.characterWindowCount);
                    windowPool.ForgetTrackedObject(oldWindow, newWindow);
                    this.characterWindow = newWindow;
                }

                int amountToRead = Math.Min(this.textEnd - (this.basis + this.characterWindowCount),
                    this.characterWindow.Length - this.characterWindowCount);
                this.text.CopyTo(this.basis + this.characterWindowCount,
                    this.characterWindow,
                    this.characterWindowCount,
                    amountToRead);
                this.characterWindowCount += amountToRead;
                return amountToRead > 0;
            }

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
        {
            return offset >= characterWindowCount && Position >= textEnd;
        }

        /// <summary>
        /// Advance the current position by one. No guarantee that this
        /// position is valid.
        /// </summary>
        public void AdvanceChar()
        {
            this.offset++;
        }

        /// <summary>
        /// Advance the current position by n. No guarantee that this position
        /// is valid.
        /// </summary>
        public void AdvanceChar(int n)
        {
            this.offset += n;
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
        /// The next character if any are available. InvalidCharacterSentinal otherwise.
        /// </returns>
        public char PeekChar()
        {
            if (this.offset >= this.characterWindowCount
                && !MoreChars())
            {
                return InvalidCharacter;
            }

            // N.B. MoreChars may update the offset.
            return this.characterWindow[this.offset];
        }

        /// <summary>
        /// Gets the character at the given offset to the current position if
        /// the position is valid within the SourceText.
        /// </summary>
        /// <returns>
        /// The next character if any are available. InvalidCharacterSentinal otherwise.
        /// </returns>
        public char PeekChar(int delta)
        {
            int position = this.Position;
            this.AdvanceChar(delta);

            char ch;
            if (this.offset >= this.characterWindowCount
                && !MoreChars())
            {
                ch = InvalidCharacter;
            }
            else
            {
                // N.B. MoreChars may update the offset.
                ch = this.characterWindow[this.offset];
            }

            this.Reset(position);
            return ch;
        }

        public bool IsUnicodeEscape()
        {
            if (this.PeekChar() == '\\')
            {
                var ch2 = this.PeekChar(1);
                if (ch2 == 'U' || ch2 == 'u')
                {
                    return true;
                }
            }

            return false;
        }

        public char PeekCharOrUnicodeEscape(out char surrogateCharacter)
        {
            if (this.IsUnicodeEscape())
            {
                return this.PeekUnicodeEscape(out surrogateCharacter);
            }
            else
            {
                surrogateCharacter = InvalidCharacter;
                return this.PeekChar();
            }
        }

        public char PeekUnicodeEscape(out char surrogateCharacter)
        {
            int position = this.Position;

            // if we're peeking, then we don't want to change the position
            SyntaxDiagnosticInfo info;
            var ch = this.ScanUnicodeEscape(peek: true, surrogateCharacter: out surrogateCharacter, info: out info);
            Debug.Assert(info == null, "Never produce a diagnostic while peeking.");
            this.Reset(position);
            return ch;
        }

        public char NextCharOrUnicodeEscape(out char surrogateCharacter, out SyntaxDiagnosticInfo info)
        {
            var ch = this.PeekChar();
            Debug.Assert(ch != InvalidCharacter, "Precondition established by all callers; required for correctness of AdvanceChar() call.");
            if (ch == '\\')
            {
                var ch2 = this.PeekChar(1);
                if (ch2 == 'U' || ch2 == 'u')
                {
                    return this.ScanUnicodeEscape(peek: false, surrogateCharacter: out surrogateCharacter, info: out info);
                }
            }

            surrogateCharacter = InvalidCharacter;
            info = null;
            this.AdvanceChar();
            return ch;
        }

        public char NextUnicodeEscape(out char surrogateCharacter, out SyntaxDiagnosticInfo info)
        {
            return ScanUnicodeEscape(peek: false, surrogateCharacter: out surrogateCharacter, info: out info);
        }

        private char ScanUnicodeEscape(bool peek, out char surrogateCharacter, out SyntaxDiagnosticInfo info)
        {
            surrogateCharacter = InvalidCharacter;
            info = null;

            int start = this.Position;
            char character = this.PeekChar();
            Debug.Assert(character == '\\');
            this.AdvanceChar();

            character = this.PeekChar();
            if (character == 'U')
            {
                uint uintChar = 0;

                this.AdvanceChar();
                if (!SyntaxFacts.IsHexDigit(this.PeekChar()))
                {
                    if (!peek)
                    {
                        info = CreateIllegalEscapeDiagnostic(start);
                    }
                }
                else
                {
                    for (int i = 0; i < 8; i++)
                    {
                        character = this.PeekChar();
                        if (!SyntaxFacts.IsHexDigit(character))
                        {
                            if (!peek)
                            {
                                info = CreateIllegalEscapeDiagnostic(start);
                            }

                            break;
                        }

                        uintChar = (uint)((uintChar << 4) + SyntaxFacts.HexValue(character));
                        this.AdvanceChar();
                    }

                    if (uintChar > 0x0010FFFF)
                    {
                        if (!peek)
                        {
                            info = CreateIllegalEscapeDiagnostic(start);
                        }
                    }
                    else
                    {
                        character = GetCharsFromUtf32(uintChar, out surrogateCharacter);
                    }
                }
            }
            else
            {
                Debug.Assert(character == 'u' || character == 'x');

                int intChar = 0;
                this.AdvanceChar();
                if (!SyntaxFacts.IsHexDigit(this.PeekChar()))
                {
                    if (!peek)
                    {
                        info = CreateIllegalEscapeDiagnostic(start);
                    }
                }
                else
                {
                    for (int i = 0; i < 4; i++)
                    {
                        char ch2 = this.PeekChar();
                        if (!SyntaxFacts.IsHexDigit(ch2))
                        {
                            if (character == 'u')
                            {
                                if (!peek)
                                {
                                    info = CreateIllegalEscapeDiagnostic(start);
                                }
                            }

                            break;
                        }

                        intChar = (intChar << 4) + SyntaxFacts.HexValue(ch2);
                        this.AdvanceChar();
                    }

                    character = (char)intChar;
                }
            }

            return character;
        }

        /// <summary>
        /// Given that the next character is an ampersand ('&amp;'), attempt to interpret the
        /// following characters as an XML entity.  On success, populate the out parameters
        /// with the low and high UTF-16 surrogates for the character represented by the
        /// entity.
        /// </summary>
        /// <param name="ch">e.g. '&lt;' for &amp;lt;.</param>
        /// <param name="surrogate">e.g. '\uDC00' for &amp;#x10000; (ch == '\uD800').</param>
        /// <returns>True if a valid XML entity was consumed.</returns>
        /// <remarks>
        /// NOTE: Always advances, even on failure.
        /// </remarks>
        public bool TryScanXmlEntity(out char ch, out char surrogate)
        {
            Debug.Assert(this.PeekChar() == '&');

            ch = '&';
            this.AdvanceChar();

            surrogate = InvalidCharacter;

            switch (this.PeekChar())
            {
                case 'l':
                    if (AdvanceIfMatches("lt;"))
                    {
                        ch = '<';
                        return true;
                    }
                    break;
                case 'g':
                    if (AdvanceIfMatches("gt;"))
                    {
                        ch = '>';
                        return true;
                    }
                    break;
                case 'a':
                    if (AdvanceIfMatches("amp;"))
                    {
                        ch = '&';
                        return true;
                    }
                    else if (AdvanceIfMatches("apos;"))
                    {
                        ch = '\'';
                        return true;
                    }
                    break;
                case 'q':
                    if (AdvanceIfMatches("quot;"))
                    {
                        ch = '"';
                        return true;
                    }
                    break;
                case '#':
                    {
                        this.AdvanceChar(); //#

                        uint uintChar = 0;

                        if (AdvanceIfMatches("x"))
                        {
                            char digit;
                            while (SyntaxFacts.IsHexDigit(digit = this.PeekChar()))
                            {
                                this.AdvanceChar();

                                // disallow overflow
                                if (uintChar <= 0x7FFFFFF)
                                {
                                    uintChar = (uintChar << 4) + (uint)SyntaxFacts.HexValue(digit);
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }
                        else
                        {
                            char digit;
                            while (SyntaxFacts.IsDecDigit(digit = this.PeekChar()))
                            {
                                this.AdvanceChar();

                                // disallow overflow
                                if (uintChar <= 0x7FFFFFF)
                                {
                                    uintChar = (uintChar << 3) + (uintChar << 1) + (uint)SyntaxFacts.DecValue(digit);
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }

                        if (AdvanceIfMatches(";"))
                        {
                            ch = GetCharsFromUtf32(uintChar, out surrogate);
                            return true;
                        }

                        break;
                    }
            }

            return false;
        }

        /// <summary>
        /// If the next characters in the window match the given string,
        /// then advance past those characters.  Otherwise, do nothing.
        /// </summary>
        private bool AdvanceIfMatches(string desired)
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

        private SyntaxDiagnosticInfo CreateIllegalEscapeDiagnostic(int start)
        {
            return new SyntaxDiagnosticInfo(start - this.LexemeStartPosition,
                this.Position - start,
                ErrorCode.ERR_IllegalEscape);
        }

        public string Intern(StringBuilder text)
        {
            return this.strings.Add(text);
        }

        public string Intern(char[] array, int start, int length)
        {
            return this.strings.Add(array, start, length);
        }

        public string GetInternedText()
        {
            return this.Intern(this.characterWindow, this.lexemeStart, this.Width);
        }

        public string GetText(bool intern)
        {
            return this.GetText(this.LexemeStartPosition, this.Width, intern);
        }

        public string GetText(int position, int length, bool intern)
        {
            int offset = position - this.basis;

            // PERF: Whether interning or not, there are some frequently occurring
            // easy cases we can pick off easily.
            switch (length)
            {
                case 0:
                    return string.Empty;

                case 1:
                    if (this.characterWindow[offset] == ' ')
                    {
                        return " ";
                    }
                    break;

                case 2:
                    char firstChar = this.characterWindow[offset];
                    if (firstChar == '\r' && this.characterWindow[offset + 1] == '\n')
                    {
                        return "\r\n";
                    }
                    if (firstChar == '/' && this.characterWindow[offset + 1] == '/')
                    {
                        return "//";
                    }
                    break;

                case 3:
                    if (this.characterWindow[offset] == '/' && this.characterWindow[offset + 1] == '/' && this.characterWindow[offset + 2] == ' ')
                    {
                        return "// ";
                    }
                    break;
            }

            if (intern)
            {
                return this.Intern(this.characterWindow, offset, length);
            }
            else
            {
                return new string(this.characterWindow, offset, length);
            }
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
    }
}