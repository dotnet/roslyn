// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if DEBUG
#define TRACING
#endif

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    /// <summary>
    /// Keeps a sliding buffer over the SourceText of a file for the lexer.  Generally, the sliding buffer will
    /// almost always move forward a 'chunk' (<see cref="DefaultWindowLength"/>) at a time.  However, the buffer
    /// also supports moving backward to a previous location if needed.  
    /// </summary>
    [NonCopyable]
    internal struct SlidingTextWindow
    {
#if TRACING
        public static int GetTextInsideWindowCount = 0;
        public static int GetTextOutsideWindowCount = 0;
        public static long TotalTextSize = 0;
#endif

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

        /// <summary>
        /// This number was picked based on the roslyn codebase.  If we look at parsing all files,
        /// this window ensures that 97.4% of all characters grabbed are within the window.  Note
        /// that this is lower than expected, but heavily influenced by how heavily roslyn uses
        /// large strings in its codebase to represent test content.  With all these files, lexemes
        /// average around 60 characters.  If tests are excluded, then average lexeme length drops
        /// to 25 characters, and the hit rate goes up to 98.4%.  Because this hitrate is so high,
        /// we don't bother to dynamically resize the window.  Instead, when we hit the end, we just
        /// grab in the next chunk of characters from the source text, as only one in every 65 lexemes
        /// will need to go back to the source-text to read the full lexeme.
        /// </summary>
        public const int DefaultWindowLength = 4096;

        private static readonly ObjectPool<char[]> s_windowPool = new ObjectPool<char[]>(() => new char[DefaultWindowLength]);

        /// <summary>
        /// Underlying source text we are lexing.  This is the final truth of all characters.  Chunks of
        /// this text will be read into a character window as needed to allow fast contiguous access to
        /// a portion of the file at a time (as many <see cref="SourceText"/> implementations have logarithmic
        /// access time to characters).
        /// </summary>
        public SourceText Text { get; }

        /// <summary>
        /// Absolute end position of <see cref="Text"/>.  Attempts to read at or past this position will 
        /// produce <see cref="InvalidCharacter"/>.
        /// </summary>
        private readonly int _textEnd;

        /// <summary>
        /// The current position in <see cref="Text"/> that we are reading characters from.  This is the absolute 
        /// position in the source text.  This is not allowed to be negative.  It is allowed to be greater than or
        /// equal to <see cref="_textEnd"/>
        /// </summary>
        private int _positionInText;

        /// <summary>
        /// Current segment of readable characters.  The backing store for this is an array given out by <see cref="s_windowPool"/>.
        /// The length of this will normally be <see cref="DefaultWindowLength"/>, but may be smaller if we are at the end of the file
        /// and there are not enough characters left to fill the window.
        /// </summary>
        private ArraySegment<char> _characterWindow;

        /// <summary>
        /// Where the current character window starts in the source text.  This is the absolute position in the source text.
        /// In other words, if this is 2048, then that means it represents the chunk of characters starting at position 2048
        /// in the source text. <c>_characterWindow.Count</c> represents how large the chunk is.  Characters
        /// <c>[0, _characterWindow.Count)</c> are valid characters within the window, and represent
        /// the chunk <c>[_characterWindowStartPositionInText, CharacterWindowEndPositionInText)</c> in <see cref="Text"/>.
        /// </summary>
        private int _characterWindowStartPositionInText;

#if DEBUG
        private bool _freed;
#endif

        private readonly StringTable _strings;

        public SlidingTextWindow(SourceText text)
        {
            this.Text = text;
            _textEnd = text.Length;
            _strings = StringTable.GetInstance();
            _characterWindow = new ArraySegment<char>(s_windowPool.Allocate());

            // Read the first chunk of the file into the character window.
            this.ReadChunkAt(0);
        }

        public void Free()
        {
#if DEBUG
            Debug.Assert(!_freed);
            _freed = true;
#endif

            s_windowPool.Free(_characterWindow.Array!);
            _strings.Free();
        }

        /// <summary>
        /// Reads a chunk of characters from the underlying <see cref="Text"/> at the given position and places them
        /// at the start of the character window.  The character window's length will be set to the number of characters
        /// read.
        /// <para/>
        /// Note: this does NOT set <see cref="_positionInText"/>.  We are just reading the characters into the window. The actual 
        /// position in the text is unchanged by this, and callers should set that if necessary.
        /// </summary>
        private void ReadChunkAt(int position)
        {
            position = Math.Min(position, _textEnd);

            var amountToRead = Math.Min(_textEnd - position, DefaultWindowLength);
            this.Text.CopyTo(position, _characterWindow.Array!, 0, amountToRead);
            _characterWindowStartPositionInText = position;
            _characterWindow = new(_characterWindow.Array!, 0, amountToRead);
        }

        /// <summary>
        /// The current absolute position in the text file.
        /// </summary>
        public readonly int Position => _positionInText;

        /// <summary>
        /// Gets a view over the underlying characters that this window is currently pointing at.
        /// The view starts at <see cref="Position"/> and contains as many legal characters from
        /// <see cref="Text"/> that are available after that.  This span may be empty.
        /// </summary>
        public readonly ReadOnlySpan<char> CurrentWindowSpan
        {
            get
            {
                var start = _positionInText - _characterWindowStartPositionInText;
                // If we are outside the bounds of the current character window, then we cannot return a span around any of it.
                if (start < 0 || start >= _characterWindow.Count)
                    return default;

                return _characterWindow.AsSpan(start);
            }
        }
        /// <summary>
        /// Similar to <see cref="_characterWindowStartPositionInText"/>, except this represents the index (exclusive) of the last character
        /// that <see cref="_characterWindow"/> encompases in <see cref="Text"/>.  This is equal to <see cref="_characterWindowStartPositionInText"/>
        /// + <see cref="_characterWindow"/>'s <see cref="ArraySegment{T}.Count"/>.
        /// </summary>
        private readonly int CharacterWindowEndPositionInText => _characterWindowStartPositionInText + _characterWindow.Count;

        /// <summary>
        /// Returns true if <paramref name="position"/> is within the current character window, and thus the character at that position
        /// can be read from the character window without going back to the underlying <see cref="Text"/>.
        /// </summary>
        private readonly bool PositionIsWithinWindow(int position)
        {
            return position >= _characterWindowStartPositionInText &&
                   position < this.CharacterWindowEndPositionInText;
        }

        /// <summary>
        /// Returns true if <paramref name="span"/> is within the current character window, and thus the sub-string corresponding to 
        /// that span can be read can be read from the character window without going back to the underlying <see cref="Text"/>.
        /// </summary>
        public readonly bool SpanIsWithinWindow(TextSpan span)
        {
            return span.Start >= _characterWindowStartPositionInText &&
                   span.End <= this.CharacterWindowEndPositionInText;
        }

        /// <summary>
        /// Returns the span of characters corresponding to <paramref name="span"/> from <see cref="Text"/> <em>if</em>
        /// <paramref name="span"/> is entirely within bounds of the current character window (see <see
        /// cref="SpanIsWithinWindow(TextSpan)"/>).  Otherwise, returns <see langword="false"/>.  Used to allow
        /// fast path access to a sequence of characters in the common case where they are in the window, or
        /// having the caller fall back to a slower approach. Note: this does not mutate the window in any way,
        /// it just reads from a segment of the character window if that is available, return <c>default</c> for
        /// <paramref name="textSpan"/> if not.
        /// </summary>
        public readonly bool TryGetTextIfWithinWindow(TextSpan span, out ReadOnlySpan<char> textSpan)
        {
            if (SpanIsWithinWindow(span))
            {
                textSpan = _characterWindow.AsSpan(span.Start - _characterWindowStartPositionInText, span.Length);
                return true;
            }

            textSpan = default;
            return false;
        }

        /// <summary>
        /// Moves this window to point at the given position in the text.  This will ensure that reading characters 
        /// (and normally characters after it) will be fast.
        /// </summary>
        public void Reset(int position)
        {
            // Move us to that position.
            _positionInText = Math.Min(position, _textEnd);

            // if position is within already read character range then just use what we have
            if (PositionIsWithinWindow(_positionInText))
                return;

            // Otherwise, ensure that the character window contains this position so we can read characters directly
            // from there.
            this.ReadChunkAt(_positionInText);
        }

        /// <summary>
        /// After reading <see cref=" InvalidCharacter"/>, a consumer can determine
        /// if the InvalidCharacter was in the user's source or a sentinel.
        /// 
        /// Comments and string literals are allowed to contain any Unicode character.
        /// </summary>
        public readonly bool IsReallyAtEnd()
            => Position >= _textEnd;

        /// <summary>
        /// Advance the current position by one. No guarantee that this position is valid.  This will <em>not</em> change the character window
        /// in any way.  Specifically, it will not create a new character window, nor will it change the contents of the current window.
        /// </summary>
        public void AdvanceChar()
            => AdvanceChar(1);

        /// <summary>
        /// Advances the text window if it currently pointing at the <paramref name="c"/> character.  Returns <see
        /// langword="true"/> if it did advance, <see langword="false"/> otherwise.  This <em>can</em> change the
        /// character window if the <see cref="Position"/> is at the end of the current character window, and
        /// peeking then needs to read in a new chunk of characters to compare to <paramref name="c"/>.
        /// </summary>
        public bool TryAdvance(char c)
        {
            if (PeekChar() != c)
                return false;

            AdvanceChar();
            return true;
        }

        /// <summary>
        /// Advance the current position by n. No guarantee that this position is valid.  This will <em>not</em> change the character window
        /// in any way.  Specifically, it will not create a new character window, nor will it change the contents of the current window.
        /// </summary>
        public void AdvanceChar(int n)
        {
            _positionInText += n;
            Debug.Assert(_positionInText >= 0, "Position in text cannot be negative.");
        }

        /// <summary>
        /// Moves past the newline that the text window is currently pointing at.  The text window must be pointing at a
        /// newline.  If the newline is <c>\r\n</c> then that entire sequence will be skipped.  Otherwise, the text
        /// window will only advance past a single character.
        /// </summary>
        public void AdvancePastNewLine()
        {
            AdvanceChar(GetNewLineWidth());
        }

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
            if (IsReallyAtEnd())
                return InvalidCharacter;

            var position = _positionInText;

            // If the position is outside of the bounds of the current character window, then update its contents to 
            // contain that position.
            if (!PositionIsWithinWindow(position))
                this.ReadChunkAt(position);

            return _characterWindow.Array![position - _characterWindowStartPositionInText];
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
            int position = this.Position;
            this.AdvanceChar(delta);
            var ch = PeekChar();
            this.Reset(position);
            return ch;
        }

        public char PreviousChar()
            => PeekChar(-1);

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

        public readonly string Intern(StringBuilder text)
        {
            return _strings.Add(text);
        }

        public readonly string Intern(char[] array, int start, int length)
            => Intern(array.AsSpan(start, length));

        public readonly string Intern(ReadOnlySpan<char> chars)
            => _strings.Add(chars);

        /// <summary>
        /// Gets the text, in the range <c>[startPosition, this.Position)</c> from <see cref="Text"/>.
        /// This will grab the text from the character window if it is within the bounds of the current
        /// chunk, or from the underlying <see cref="Text"/> if it is not.
        /// </summary>
        public readonly string GetText(int startPosition, bool intern)
            => this.GetText(startPosition, this.Position - startPosition, intern);

        /// <summary>
        /// Gets the text, in the range <c>[position, position + length)</c> from <see cref="Text"/>.
        /// This will grab the text from the character window if it is within the bounds of the current
        /// chunk, or from the underlying <see cref="Text"/> if it is not.
        /// </summary>
        public readonly string GetText(int position, int length, bool intern)
        {
            var span = new TextSpan(position, length);

#if TRACING
            Interlocked.Add(ref TotalTextSize, length);
#endif

            // If the chunk being grabbed is not within the bounds of what is in the character window,
            // then grab it from the source text.  See docs at top of file for details on how common
            // this is.
            if (!SpanIsWithinWindow(span))
            {
#if TRACING
                Interlocked.Increment(ref GetTextOutsideWindowCount);
#endif
                return this.Text.ToString(span);
            }

#if TRACING
            Interlocked.Increment(ref GetTextInsideWindowCount);
#endif

            var offset = position - _characterWindowStartPositionInText;
            var array = _characterWindow.Array!;

            // PERF: Whether interning or not, there are some frequently occurring
            // easy cases we can pick off easily.
            switch (length)
            {
                case 0: return string.Empty;

                case 1:
                    switch (array[offset])
                    {
                        case ' ': return " ";
                        case '\n': return "\n";
                    }

                    break;

                case 2:
                    switch (array[offset], array[offset + 1])
                    {
                        case ('\r', '\n'): return "\r\n";
                        case ('/', '/'): return "//";
                    }

                    break;

                case 3:
                    if (array[offset] == '/' && array[offset + 1] == '/' && array[offset + 2] == ' ')
                    {
                        return "// ";
                    }

                    break;
            }

            return intern
                ? this.Intern(array, offset, length)
                : new string(array, offset, length);
        }

        public static class TestAccessor
        {
            public static int GetOffset(in SlidingTextWindow window) => window._positionInText - window._characterWindowStartPositionInText;
            public static int GetCharacterWindowStartPositionInText(in SlidingTextWindow window) => window._characterWindowStartPositionInText;
            public static ArraySegment<char> GetCharacterWindow(in SlidingTextWindow window) => window._characterWindow;
        }
    }
}
