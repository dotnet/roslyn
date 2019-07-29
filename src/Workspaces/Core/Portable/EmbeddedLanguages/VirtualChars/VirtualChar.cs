// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
        public readonly char Char;
        public readonly TextSpan Span;

        public VirtualChar(char @char, TextSpan span)
        {
            if (span.IsEmpty)
            {
                throw new ArgumentException("Span should not be empty.", nameof(span));
            }

            Char = @char;
            Span = span;
        }

        public override bool Equals(object obj)
            => obj is VirtualChar vc && Equals(vc);

        public bool Equals(VirtualChar other)
            => Char == other.Char &&
               Span == other.Span;

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 244102310;
                hashCode = hashCode * -1521134295 + Char.GetHashCode();
                hashCode = hashCode * -1521134295 + Span.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(VirtualChar char1, VirtualChar char2)
            => char1.Equals(char2);

        public static bool operator !=(VirtualChar char1, VirtualChar char2)
            => !(char1 == char2);

        public static implicit operator char(VirtualChar vc) => vc.Char;
    }
}
