// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Roslyn.Compilers.CSharp.InternalSyntax
{
    // seperate out text windowing implementation (keeps scanning & lexing functions from abusing details)
    internal class LexerBase
    {
        private const int DefaultWindowLength = 2048;

        private IText text;             // source of text to parse
        private int basis;              // # of characters shifted past in buffer (basis + offset == position)
        private int start;              // start of current lexeme within chars buffer
        private int offset;             // offset from start of chars buffer
        private int textEnd;            // absolute end position
        private char[] chars;           // moveable window of chars from source text
        private int charCount;          // # of valid characters in chars buffer
        private LexerBaseCache cache;

        private List<SyntaxDiagnosticInfo> errors;

        public LexerBase(IText text, LexerBaseCache cache)
        {
            Debug.Assert(cache != null);
            this.text = text;
            this.basis = 0;
            this.start = 0;
            this.offset = 0;
            this.textEnd = text.Length;
            this.cache = cache;
            this.chars = new char[DefaultWindowLength];
        }

        public IText Text
        {
            get { return this.text; }
        }

        public int Position
        {
            get { return this.offset + this.basis; }
        }

        protected int Offset
        {
            get { return this.offset; }
        }

        protected char[] CharacterWindow
        {
            get { return this.chars; }
        }

        protected int CharacterWindowStart
        {
            get { return this.start; }
        }

        protected int CharacterWindowCount
        {
            get { return this.charCount; }
        }

        protected void Start()
        {
            this.start = this.offset;
            this.errors = null;
        }

        protected int StartPosition
        {
            get { return this.start + this.basis; }
        }

        protected int Width
        {
            get { return this.offset - this.start; }
        }

        protected bool HasErrors
        {
            get { return this.errors != null; }
        }

        protected IList<SyntaxDiagnosticInfo> Errors
        {
            get { return this.errors; }
        }

        internal int BufferSize
        {
            get { return this.chars.Length; }
        }

        public void Reset(int position)
        {
            // if position is within already read character range then just use what we have
            int relative = position - this.basis;
            if (relative >= 0 && relative <= this.charCount)
            {
                this.offset = relative;
            }
            else
            {
                // we need to reread text buffer
                int amountToRead = Math.Min(this.text.Length, position + this.chars.Length) - position;
                amountToRead = Math.Max(amountToRead, 0);
                if (amountToRead > 0)
                {
                    this.text.CopyTo(position, this.chars, 0, amountToRead);
                }

                this.start = 0;
                this.offset = 0;
                this.basis = position;
                this.charCount = amountToRead;
            }
        }

        private bool MoreChars()
        {
            if (this.offset >= this.charCount)
            {
                if (this.offset + this.basis >= this.textEnd)
                {
                    return false;
                }

                // if we are sufficiently into the char buffer, then shift chars to the left
                if (this.start > (this.charCount >> 2))
                {
                    Array.Copy(this.chars, this.start, this.chars, 0, charCount - start);
                    this.charCount -= start;
                    this.offset -= start;
                    this.basis += this.start;
                    this.start = 0;
                }

                if (this.charCount >= this.chars.Length)
                {
                    // grow char array, since we need more contiguous space
                    var tmp = new char[this.chars.Length * 2];
                    Array.Copy(this.chars, 0, tmp, 0, this.charCount);
                    this.chars = tmp;
                }

                int amountToRead = Math.Min(this.text.Length - (this.basis + this.charCount), this.chars.Length - this.charCount);
                this.text.CopyTo(this.basis + this.charCount, this.chars, this.charCount, amountToRead);
                this.charCount += amountToRead;
                return amountToRead > 0;
            }

            return true;
        }

        protected void AddError(int position, int width, ErrorCode code, params object[] args)
        {
            this.AddError(this.MakeError(position, width, code, args));
        }

        protected void AddError(ErrorCode code, params object[] args)
        {
            this.AddError(this.MakeError(code, args));
        }

        protected void AddError(SyntaxDiagnosticInfo error)
        {
            if (this.errors == null)
            {
                this.errors = new List<SyntaxDiagnosticInfo>(8);
            }

            this.errors.Add(error);
        }

        protected SyntaxDiagnosticInfo MakeError(int position, int width, ErrorCode code, params object[] args)
        {
            int offset = position >= this.StartPosition ? position - this.StartPosition : position;
            return new SyntaxDiagnosticInfo(offset, width, code, args);
        }

        protected SyntaxDiagnosticInfo MakeError(ErrorCode code, params object[] args)
        {
            return new SyntaxDiagnosticInfo(code, args);
        }

        protected void AdvanceChar()
        {
            this.offset++;
        }

        protected void AdvanceChar(int n)
        {
            this.offset += n;
        }

        protected char NextChar()
        {
            if (this.offset < this.charCount || MoreChars())
            {
                var c = this.chars[this.offset];
                this.AdvanceChar();
                return c;
            }

            return '\0';
        }

        protected char PeekChar()
        {
            var o = this.offset;
            if (o >= this.charCount)
            {
                return this.PeekChar2();
            }

            return this.chars[o];
        }

        private char PeekChar2()
        {
            if (MoreChars())
            {
                return this.chars[this.offset];
            }

            return '\0';
        }

        protected char PeekChar(int delta)
        {
            int position = this.Position;
            this.AdvanceChar(delta);
            char ch;
            if (this.offset < this.charCount || MoreChars())
            {
                ch = this.chars[this.offset];
            }
            else
            {
                ch = '\0';
            }

            this.Reset(position);
            return ch;
        }

        protected bool IsUnicodeEscape()
        {
            char ch2;
            return this.PeekChar() == '\\' && ((ch2 = this.PeekChar(1)) == 'U' || ch2 == 'u');
        }

        protected char PeekChar(out char surrogateCharacter)
        {
            if (this.IsUnicodeEscape())
            {
                return this.PeekUnicodeEscape(out surrogateCharacter);
            }
            else
            {
                surrogateCharacter = '\0';
                return this.PeekChar();
            }
        }

        protected char PeekUnicodeEscape(out char surrogateCharacter)
        {
            int position = this.Position;

            // if we're peeking, then we don't want to change the position
            var ch = this.ScanUnicodeEscape(out surrogateCharacter, true);
            this.Reset(position);
            return ch;
        }

        protected char NextChar(out char surrogateCharacter)
        {
            char ch = this.PeekChar();
            char ch2;
            surrogateCharacter = '\0';
            if (ch == '\\' && ((ch2 = this.PeekChar(1)) == 'U' || ch2 == 'u'))
            {
                return this.ScanUnicodeEscape(out surrogateCharacter, false);
            }
            else
            {
                this.AdvanceChar();
                return ch;
            }
        }

        protected char ScanUnicodeEscape(out char surrogateCharacter, bool peek)
        {
            int start = this.Position;
            surrogateCharacter = '\0';
            char character = this.PeekChar();
            System.Diagnostics.Debug.Assert(character == '\\');
            this.AdvanceChar();

            character = this.PeekChar();
            if (character == 'U')
            {
                uint uintChar = 0;

                this.AdvanceChar();
                if (!IsHexDigit(this.PeekChar()))
                {
                    if (!peek)
                    {
                        this.AddError(start, this.Position - start, ErrorCode.ERR_IllegalEscape);
                    }
                }
                else
                {
                    for (int i = 0; i < 8; i++)
                    {
                        character = this.PeekChar();
                        if (!IsHexDigit(character))
                        {
                            if (!peek)
                            {
                                this.AddError(start, this.Position - start, ErrorCode.ERR_IllegalEscape);
                            }

                            break;
                        }

                        uintChar = (uint)((uintChar << 4) + HexValue(character));
                        this.AdvanceChar();
                    }

                    if (uintChar > 0x0010FFFF)
                    {
                        if (!peek)
                        {
                            this.AddError(start, this.Position - start, ErrorCode.ERR_IllegalEscape);
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
                System.Diagnostics.Debug.Assert(character == 'u' || character == 'x');

                int intChar = 0;
                this.AdvanceChar();
                if (!IsHexDigit(this.PeekChar()))
                {
                    if (!peek)
                    {
                        this.AddError(start, this.Position - start, ErrorCode.ERR_IllegalEscape);
                    }
                }
                else
                {
                    for (int i = 0; i < 4; i++)
                    {
                        char ch2 = this.PeekChar();
                        if (!IsHexDigit(ch2))
                        {
                            if (character == 'u')
                            {
                                if (!peek)
                                {
                                    this.AddError(start, this.Position - start, ErrorCode.ERR_IllegalEscape);
                                }
                            }

                            break;
                        }

                        intChar = (intChar << 4) + HexValue(ch2);
                        this.AdvanceChar();
                    }

                    character = (char)intChar;
                }
            }

            return character;
        }

        protected static char GetCharsFromUtf32(uint codepoint, out char lowSurrogate)
        {
            if (codepoint < (uint)0x00010000)
            {
                lowSurrogate = '\0';
                return (char)codepoint;
            }
            else
            {
                Debug.Assert(codepoint > 0x0000FFFF && codepoint <= 0x0010FFFF);
                lowSurrogate = (char)((codepoint - 0x00010000) % 0x0400 + 0xDC00);
                return (char)((codepoint - 0x00010000) / 0x0400 + 0xD800);
            }
        }

        protected string Intern(string text)
        {
            return this.cache.Intern(text);
        }

        protected string Intern(StringBuilder text)
        {
            return this.cache.Intern(text);
        }

        protected string Intern(char[] array, int start, int length)
        {
            return this.cache.Intern(array, start, length);
        }

        protected string GetInternedText()
        {
            return this.Intern(this.chars, this.start, this.offset - this.start);
        }

        protected string GetText(bool intern)
        {
            return this.GetText(this.StartPosition, this.Width, intern);
        }

        protected string GetText(int position, int length, bool intern)
        {
            int offset = position - this.basis;
            if (intern)
            {
                return this.Intern(this.chars, offset, length);
            }
            else
            {
                return new string(this.chars, offset, length);
            }
        }

        public static bool IsHexDigit(char c)
        {
            return (c >= '0' && c <= '9') ||
                   (c >= 'A' && c <= 'F') ||
                   (c >= 'a' && c <= 'f');
        }

        public static bool IsDecDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        public static int HexValue(char c)
        {
            Debug.Assert(IsHexDigit(c));
            return (c >= '0' && c <= '9') ? c - '0' : (c & 0xdf) - 'A' + 10;
        }

        public static int DecValue(char c)
        {
            Debug.Assert(IsDecDigit(c));
            return c - '0';
        }

        // UnicodeCategory value | Unicode designation
        // -----------------------+-----------------------
        // UppercaseLetter         "Lu" (letter, uppercase)
        // LowercaseLetter         "Ll" (letter, lowercase)
        // TitlecaseLetter         "Lt" (letter, titlecase)
        // ModifierLetter          "Lm" (letter, modifier)
        // OtherLetter             "Lo" (letter, other)
        // NonSpacingMark          "Mn" (mark, nonspacing)
        // SpacingCombiningMark    "Mc" (mark, spacing combining)
        // EnclosingMark           "Me" (mark, enclosing)
        // DecimalDigitNumber      "Nd" (number, decimal digit)
        // LetterNumber            "Nl" (number, letter)
        // OtherNumber             "No" (number, other)
        // SpaceSeparator          "Zs" (separator, space)
        // LineSeparator           "Zl" (separator, line)
        // ParagraphSeparator      "Zp" (separator, paragraph)
        // Control                 "Cc" (other, control)
        // Format                  "Cf" (other, format)
        // Surrogate               "Cs" (other, surrogate)
        // PrivateUse              "Co" (other, private use)
        // ConnectorPunctuation    "Pc" (punctuation, connector)
        // DashPunctuation         "Pd" (punctuation, dash)
        // OpenPunctuation         "Ps" (punctuation, open)
        // ClosePunctuation        "Pe" (punctuation, close)
        // InitialQuotePunctuation "Pi" (punctuation, initial quote)
        // FinalQuotePunctuation   "Pf" (punctuation, final quote)
        // OtherPunctuation        "Po" (punctuation, other)
        // MathSymbol              "Sm" (symbol, math)
        // CurrencySymbol          "Sc" (symbol, currency)
        // ModifierSymbol          "Sk" (symbol, modifier)
        // OtherSymbol             "So" (symbol, other)
        // OtherNotAssigned        "Cn" (other, not assigned)

        //////////////////////////////////////////////////////////////////////

        public static bool IsWhitespaceChar(char ch)
        {
            // whitespace:
            //   Any character with Unicode class Zs
            //   Horizontal tab character (U+0009)
            //   Vertical tab character (U+000B)
            //   Form feed character (U+000C)

            // Space and no-break space are the only space separators (Zs) in ASCII range

            return ch == ' '
                || ch == '\t'
                || ch == '\v'
                || ch == '\f'
                || ch == '\u00A0' // NO-BREAK SPACE
                // The native compiler, in ScanToken, recognized both the byte-order
                // marker '\uFEFF' as well as ^Z '\u001A' as whitespace, although
                // this is not to spec since neither of these are in Zs. For the
                // sake of compatibility, we recognize them both here. Note: '\uFEFF'
                // also happens to be a formatting character (class Cf), which means
                // that it is a legal non-initial identifier character. So it's
                // especially funny, because it will be whitespace UNLESS we happen
                // to be scanning an identifier or keyword, in which case it winds
                // up in the identifier or keyword.
                // TODO: This is really ugly, but we care about compat, right?
                || ch == '\uFEFF'
                || ch == '\u001A'
                || (ch > 255 && char.GetUnicodeCategory(ch) == UnicodeCategory.SpaceSeparator);
        }

        //////////////////////////////////////////////////////////////////////

        public static bool IsNewLineChar(char ch)
        {
            // new-line-character:
            //   Carriage return character (U+000D)
            //   Line feed character (U+000A)
            //   Next line character (U+0085)
            //   Line separator character (U+2028)
            //   Paragraph separator character (U+2029)

            return ch == '\r'
                || ch == '\n'
                || ch == '\u0085'
                || ch == '\u2028'
                || ch == '\u2029';
        }

        //////////////////////////////////////////////////////////////////////

        public static bool IsIdentifierStartChar(char ch)
        {
            // identifier-start-character:
            //   letter-character
            //   _ (the underscore character U+005F)

            return (ch >= 'a' && ch <= 'z')
                || (ch >= 'A' && ch <= 'Z')
                || (ch == '_')
                || IsLetterChar(char.GetUnicodeCategory(ch));
        }

        //////////////////////////////////////////////////////////////////////

        public static bool IsIdentifierPartChar(char ch)
        {
            // identifier-part-character:
            //   letter-character
            //   decimal-digit-character
            //   connecting-character
            //   combining-character
            //   formatting-character

            UnicodeCategory cat;

            return (ch >= 'a' && ch <= 'z')
                || (ch >= 'A' && ch <= 'Z')
                || (ch >= '0' && ch <= '9')
                || (ch == '_')
                || IsLetterChar(cat = char.GetUnicodeCategory(ch))
                || IsDecimalDigitChar(cat)
                || IsConnectingChar(cat)
                || IsCombiningChar(cat)
                || IsFormattingChar(cat);
        }

        //////////////////////////////////////////////////////////////////////

        public static bool IsLetterChar(char ch)
        {
            return (ch >= 'a' && ch <= 'z')
                || (ch >= 'A' && ch <= 'Z')
                || IsLetterChar(char.GetUnicodeCategory(ch));
        }

        public static bool IsLetterChar(UnicodeCategory cat)
        {
            // letter-character:
            //   A Unicode character of classes Lu, Ll, Lt, Lm, Lo, or Nl 
            //   A unicode-escape-sequence representing a character of classes Lu, Ll, Lt, Lm, Lo, or Nl

            switch (cat)
            {
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.TitlecaseLetter:
                case UnicodeCategory.ModifierLetter:
                case UnicodeCategory.OtherLetter:
                case UnicodeCategory.LetterNumber:
                    return true;
            }

            return false;
        }

        //////////////////////////////////////////////////////////////////////

        public static bool IsCombiningChar(char ch)
        {
            // There are no CombiningChars in ASCII range

            return ch > 255 && IsCombiningChar(char.GetUnicodeCategory(ch));
        }

        public static bool IsCombiningChar(UnicodeCategory cat)
        {
            // combining-character:
            //   A Unicode character of classes Mn or Mc 
            //   A unicode-escape-sequence representing a character of classes Mn or Mc

            switch (cat)
            {
                case UnicodeCategory.NonSpacingMark:
                case UnicodeCategory.SpacingCombiningMark:
                    return true;
            }

            return false;
        }

        //////////////////////////////////////////////////////////////////////

        public static bool IsDecimalDigitChar(char ch)
        {
            // '0' through '9' are the only DecimalDigitChars in ASCII range

            return (ch >= '0' && ch <= '9')
                || (ch > 255 && IsDecimalDigitChar(char.GetUnicodeCategory(ch)));
        }

        public static bool IsDecimalDigitChar(UnicodeCategory cat)
        {
            // decimal-digit-character:
            //   A Unicode character of the class Nd 
            //   A unicode-escape-sequence representing a character of the class Nd

            return cat == UnicodeCategory.DecimalDigitNumber;
        }

        //////////////////////////////////////////////////////////////////////

        public static bool IsConnectingChar(char ch)
        {
            // '_' is the only ConnectingChar in ASCII range

            return ch == '_'
                || (ch > 255 && IsConnectingChar(char.GetUnicodeCategory(ch)));
        }

        public static bool IsConnectingChar(UnicodeCategory cat)
        {
            // connecting-character:  
            //   A Unicode character of the class Pc
            //   A unicode-escape-sequence representing a character of the class Pc

            return cat == UnicodeCategory.ConnectorPunctuation;
        }

        //////////////////////////////////////////////////////////////////////

        public static bool IsFormattingChar(char ch)
        {
            // There are no FormattingChars in ASCII range

            return ch > 255 && IsFormattingChar(char.GetUnicodeCategory(ch));
        }

        public static bool IsFormattingChar(UnicodeCategory cat)
        {
            // formatting-character:  
            //   A Unicode character of the class Cf
            //   A unicode-escape-sequence representing a character of the class Cf

            return cat == UnicodeCategory.Format;
        }

        //////////////////////////////////////////////////////////////////////

        public static bool IsXmlNameStartChar(char ch)
        {
            // 2.3 Common Syntactic Constructs
            //   Names and Tokens
            //   NameStartChar ::= ":" | [A-Z] | "_" | [a-z] | [#xC0-#xD6] | [#xD8-#xF6] | [#xF8-#x2FF] | [#x370-#x37D] |
            //                     [#x37F-#x1FFF] | [#x200C-#x200D] | [#x2070-#x218F] | [#x2C00-#x2FEF] | [#x3001-#xD7FF] |
            //                     [#xF900-#xFDCF] | [#xFDF0-#xFFFD] | [#x10000-#xEFFFF]

            if (ch == ' ' || ch == '\t' || ch == '/' || ch == '>' || ch == '=')
            {
                return false;
            }

            return (ch >= 'a' && ch <= 'z')
                || (ch >= 'A' && ch <= 'Z')
                || (ch == '_')
                || (ch == ':')
                || (ch >= '\u00C0' && ch <= '\u00D6')
                || (ch >= '\u00D8' && ch <= '\u00F6')
                || (ch >= '\u00F8' && ch <= '\u02FF')
                || (ch >= '\u0370' && ch <= '\u037D')
                || (ch >= '\u037F' && ch <= '\u1FFF')
                || (ch >= '\u200C' && ch <= '\u200D')
                || (ch >= '\u2070' && ch <= '\u218F')
                || (ch >= '\u2C00' && ch <= '\u2FEF')
                || (ch >= '\u3001' && ch <= '\uD7FF')
                || (ch >= '\uF900' && ch <= '\uFDCF')
                || (ch >= '\uFDF0' && ch <= '\uFFFD')

                // TODO: The lexer right now simply doesn't recognize unicode surrogate pairs that aren't
                // in \u escape sequences. So instead of recognizing the final "#x10000-#xEFFFF" in XML, we
                // just let all high and low surrogates through. We aren't going to do anything with them
                // anyway. We might like to revisit this decision.

                || (ch >= '\uD800' && ch <= '\uDBFF')  // high surrogates
                || (ch >= '\uDC00' && ch <= '\uDFFF'); // low surrogates
        }

        public static bool IsXmlNameChar(char ch)
        {
            // 2.3 Common Syntactic Constructs
            //   Names and Tokens
            //   NameChar ::= NameStartChar | "-" | "." | [0-9] | #xB7 | [#x0300-#x036F] | [#x203F-#x2040]

            if (ch == ' ' || ch == '\t' || ch == '/' || ch == '>' || ch == '=')
            {
                return false;
            }

            return ch == '-'
                || ch == '.'
                || (ch >= '0' && ch <= '9')
                || IsXmlNameStartChar(ch)
                || ch == '\u00B7'
                || (ch >= '\u0300' && ch <= '\u036F')
                || (ch >= '\u203F' && ch <= '\u2040');
        }
    }
}
