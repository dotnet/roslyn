// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

/// <summary>
/// <see cref="VirtualChar"/> provides a uniform view of a language's string token characters regardless if they
/// were written raw in source, or are the production of a language escape sequence.  For example, in C#, in a
/// normal <c>""</c> string a <c>Tab</c> character can be written either as the raw tab character (value <c>9</c> in
/// ASCII),  or as <c>\t</c>.  The format is a single character in the source, while the latter is two characters
/// (<c>\</c> and <c>t</c>).  <see cref="VirtualChar"/> will represent both, providing the raw <see cref="char"/>
/// value of <c>9</c> as well as what <see cref="TextSpan"/> in the original <see cref="SourceText"/> they occupied.
/// </summary>
/// <remarks>
/// A core consumer of this system is the Regex parser.  That parser wants to work over an array of characters,
/// however this array of characters is not the same as the array of characters a user types into a string in C# or
/// VB. For example In C# someone may write: @"\z".  This should appear to the user the same as if they wrote "\\z"
/// and the same as "\\\u007a".  However, as these all have wildly different presentations for the user, there needs
/// to be a way to map back the characters it sees ( '\' and 'z' ) back to the  ranges of characters the user wrote.
/// </remarks>
internal readonly record struct VirtualChar
{
    public VirtualChar(VirtualCharGreen green, int tokenStart)
    {
        if (tokenStart < 0)
            throw new ArgumentException("Token start must be non-negative", nameof(tokenStart));
        Green = green;
        TokenStart = tokenStart;
    }

    internal VirtualCharGreen Green { get; }
    internal int TokenStart { get; }

    /// <inheritdoc cref="VirtualCharGreen.Rune"/>
    public Rune Rune => Green.Rune;

    /// <inheritdoc cref="VirtualCharGreen.SurrogateChar"/>
    public char SurrogateChar => Green.SurrogateChar;

    public TextSpan Span => new(TokenStart + Green.Offset, Green.Width);

    /// <inheritdoc cref="VirtualCharGreen.Value"/>
    public int Value => Green.Value;

    /// <inheritdoc cref="VirtualCharGreen.IsDigit"/>
    public bool IsDigit => Green.IsDigit;

    /// <inheritdoc cref="VirtualCharGreen.IsLetter"/>
    public bool IsLetter => Green.IsLetter;

    /// <inheritdoc cref="VirtualCharGreen.IsLetterOrDigit"/>
    public bool IsLetterOrDigit => Green.IsLetterOrDigit;

    /// <inheritdoc cref="VirtualCharGreen.IsWhiteSpace"/>
    public bool IsWhiteSpace => Green.IsWhiteSpace;

    /// <inheritdoc cref="VirtualCharGreen.Utf16SequenceLength"/>
    public int Utf16SequenceLength => Green.Utf16SequenceLength;

    #region equality

    public static bool operator ==(VirtualChar ch1, char ch2)
        => ch1.Green.Value == ch2;

    public static bool operator !=(VirtualChar ch1, char ch2)
        => !(ch1 == ch2);

    public static bool operator <(VirtualChar ch1, char ch2)
        => ch1.Green.CompareTo(ch2) < 0;

    public static bool operator <=(VirtualChar ch1, char ch2)
        => ch1.Green.CompareTo(ch2) <= 0;

    public static bool operator >(VirtualChar ch1, char ch2)
        => ch1.Green.CompareTo(ch2) > 0;

    public static bool operator >=(VirtualChar ch1, char ch2)
        => ch1.Green.CompareTo(ch2) >= 0;

    #endregion

    #region string operations

    /// <inheritdoc cref="VirtualCharGreen.ToString"/>
    public override string ToString() => Green.ToString();

    /// <inheritdoc cref="VirtualCharGreen.AppendTo"/>
    public void AppendTo(StringBuilder builder) => Green.AppendTo(builder);

    #endregion
}
