// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

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
        /// Can represent any unicode code point.  Values are represented in Utf32.
        /// </summary>
        public readonly uint CodePoint;
        public readonly TextSpan Span;

        public VirtualChar(uint codePoint, TextSpan span)
        {
            if (span.IsEmpty)
                throw new ArgumentException("Span should not be empty.", nameof(span));

            CodePoint = codePoint;
            Span = span;
        }

        public override string ToString()
        {
            using var _ = PooledStringBuilder.GetInstance(out var builder);
            this.AppendTo(builder);
            return builder.ToString();
        }

        public void AppendTo(StringBuilder builder)
        {
            if (CodePoint <= char.MaxValue)
            {
                builder.Append((char)CodePoint);
                return;
            }

            // taken from SlidingTextWindow.GetCharsFromUtf32
            var highSurrogate = ((CodePoint - 0x00010000) / 0x0400) + 0xD800;
            var lowSurrogate = ((CodePoint - 0x00010000) % 0x0400) + 0xDC00;

            builder.Append((char)highSurrogate);
            builder.Append((char)lowSurrogate);
        }

        public override bool Equals(object obj)
            => obj is VirtualChar vc && Equals(vc);

        public bool Equals(VirtualChar other)
            => CodePoint == other.CodePoint &&
               Span == other.Span;

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 244102310;
                hashCode = hashCode * -1521134295 + CodePoint.GetHashCode();
                hashCode = hashCode * -1521134295 + Span.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(VirtualChar char1, VirtualChar char2)
            => char1.Equals(char2);

        public static bool operator !=(VirtualChar char1, VirtualChar char2)
            => !(char1 == char2);

        public static implicit operator uint(VirtualChar vc) => vc.CodePoint;
    }
}
