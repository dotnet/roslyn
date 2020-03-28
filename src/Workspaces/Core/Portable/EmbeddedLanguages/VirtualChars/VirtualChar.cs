// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
{
    /// <summary>
    /// The Regex and Json parsers wants to work over an array of characters, however this array of
    /// characters is not the same as the array of characters a user types into a string in C# or
    /// VB. For example In C# someone may write: @"\z".  This should appear to the user the same as
    /// if they wrote "\\z" and the same as "\\\u007a".  However, as these all have wildly different
    /// presentations for the user, there needs to be a way to map back the characters it sees ( '\'
    /// and 'z' ) back to the  ranges of characters the user wrote.  
    ///
    /// VirtualChar serves this purpose.  It contains the interpreted value of any language
    /// character/character-escape-sequence, as well as the original SourceText span where that
    /// interpreted character was created from.  This allows the regex and json parsers to both
    /// process input from any language uniformly, but then also produce trees and diagnostics that
    /// map back properly to the original source text locations that make sense to the user.
    /// </summary>
    internal readonly struct VirtualChar : IEquatable<VirtualChar>
    {
        /// <summary>
        /// The value of this <see cref="VirtualChar"/> as a <see cref="Rune"/> if its possible to be represented as a
        /// <see cref="Rune"/>.  <see cref="Rune"/>s can represent Unicode codepoints that can appear in a <see
        /// cref="string"/> except for unpaired surrogates.  If an unpaired high or low surrogate character is present,
        /// this value will be <see cref="Rune.ReplacementChar"/>.  The value of this character can be retrieved from
        /// <see cref="SurrogateChar"/>.
        /// </summary>
        public readonly Rune Rune;

        /// <summary>
        /// The unpaired high or low surrogate character that was encountered that could not be represented in <see
        /// cref="Rune"/>.
        /// </summary>
        public readonly char SurrogateChar;

        /// <summary>
        /// The span of characters in the original <see cref="SourceText"/> that represent this <see
        /// cref="VirtualChar"/>.
        /// </summary>
        public readonly TextSpan Span;

        public static VirtualChar Create(Rune rune, TextSpan span)
            => new VirtualChar(rune, surrogateChar: default, span);

        /// <summary>
        /// Creates a new <see cref="VirtualChar"/> from an unpaired high or low surrogate character.
        /// </summary>
        public static VirtualChar Create(char surrogateChar, TextSpan span)
        {
            if (!char.IsSurrogate(surrogateChar))
                throw new ArgumentException(nameof(surrogateChar));

            return new VirtualChar(rune: Rune.ReplacementChar, surrogateChar, span);
        }

        private VirtualChar(Rune rune, char surrogateChar, TextSpan span)
        {
            Contract.ThrowIfFalse(surrogateChar == 0 || rune == Rune.ReplacementChar,
                "If surrogateChar is provided then rune must be Rune.ReplacementChar");

            if (span.IsEmpty)
                throw new ArgumentException("Span should not be empty.", nameof(span));

            Rune = rune;
            SurrogateChar = surrogateChar;
            Span = span;
        }

        /// <summary>
        /// Retrieves the scaler value of this character as an <see cref="int"/>.  If this is an unpaired surrogate
        /// character, this will be the value of that surrogate.  Otherwise, this will be the value of our <see
        /// cref="Rune"/>.
        /// </summary>
        public int Value => SurrogateChar != 0 ? SurrogateChar : Rune.Value;

        public static bool operator ==(VirtualChar ch1, char ch2)
            => ch1.Value == ch2;

        public static bool operator !=(VirtualChar ch1, char ch2)
            => ch1.Value != ch2;

        public static bool operator <(VirtualChar ch1, char ch2)
            => ch1.Value < ch2;

        public static bool operator <=(VirtualChar ch1, char ch2)
            => ch1.Value <= ch2;

        public static bool operator >(VirtualChar ch1, char ch2)
            => ch1.Value > ch2;

        public static bool operator >=(VirtualChar ch1, char ch2)
            => ch1.Value >= ch2;

        public override string ToString()
            => Rune.ToString();

        public void AppendTo(StringBuilder builder)
        {
            if (SurrogateChar != 0)
            {
                builder.Append(SurrogateChar);
                return;
            }

            Span<char> chars = stackalloc char[2];

            var length = Rune.EncodeToUtf16(chars);
            builder.Append(chars[0]);
            if (length == 2)
                builder.Append(chars[1]);
        }

        public override bool Equals(object obj)
            => obj is VirtualChar vc && Equals(vc);

        public bool Equals(VirtualChar other)
            => Rune == other.Rune &&
               SurrogateChar == other.SurrogateChar &&
               Span == other.Span;

        public override int GetHashCode()
        {
            var hashCode = 1985253839;
            hashCode = hashCode * -1521134295 + Rune.GetHashCode();
            hashCode = hashCode * -1521134295 + SurrogateChar.GetHashCode();
            hashCode = hashCode * -1521134295 + Span.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(VirtualChar char1, VirtualChar char2)
            => char1.Equals(char2);

        public static bool operator !=(VirtualChar char1, VirtualChar char2)
            => !(char1 == char2);
    }
}
