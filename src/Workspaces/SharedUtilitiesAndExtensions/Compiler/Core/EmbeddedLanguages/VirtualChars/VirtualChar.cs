// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

/// <summary>
/// <see cref="VirtualCharGreen"/> provides a uniform view of a language's string token characters regardless if they
/// were written raw in source, or are the production of a language escape sequence.  For example, in C#, in a normal
/// <c>""</c> string a <c>Tab</c> character can be written either as the raw tab character (value <c>9</c> in ASCII),
/// or as <c>\t</c>.  The format is a single character in the source, while the latter is two characters (<c>\</c> and
/// <c>t</c>).  <see cref="VirtualCharGreen"/> will represent both, providing the raw <see cref="char"/> value of
/// <c>9</c> as well as what offset and width within original <see cref="SyntaxToken"/> the character was found at.
/// </summary>
internal readonly record struct VirtualCharGreen
{
    private const int MaxWidth = 12;
    private const int WidthMask = 0b1111; // 4 bits for width (max 10)
    private const int OffsetShift = 4;    // remaining bits for offset

    public readonly char Char;

    /// <summary>
    /// The offset and width combined into a single integer.  Because the width of a VirtualChar can't be more than
    /// 10 (for <c>\UXXXXXXX</c>), we can store the width in the lower 4 bits, and the offset in the upper 28.
    /// </summary>
    private readonly int _offsetAndWidth;

    /// <summary>
    /// Offset in the original token that this character was found at.
    /// </summary>
    public int Offset => _offsetAndWidth >> OffsetShift;

    /// <summary>
    /// The width of characters in the original <see cref="SourceText"/> that represent this <see cref="VirtualCharGreen"/>.
    /// This can be as low as 1 (for normal characters) or up to 12 (for escape sequences like <c>\u1234\uABCD</c>).
    /// </summary>
    public int Width => _offsetAndWidth & WidthMask;

    public VirtualCharGreen(char ch, int offset, int width)
    {
        Contract.ThrowIfTrue(width > MaxWidth);

        if (offset < 0)
            throw new ArgumentException("Offset cannot be negative", nameof(offset));

        if (width <= 0)
            throw new ArgumentException("Width must be greater than zero.", nameof(width));

        Char = ch;
        _offsetAndWidth = (offset << OffsetShift) | width;
    }

    public VirtualCharGreen WithOffset(int offset)
        => new(this.Char, offset, this.Width);
}

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

    public static implicit operator char(VirtualChar ch)
        => ch.Value;

    /// <inheritdoc cref="VirtualCharGreen.Char"/>
    public char Value => Green.Char;

    public TextSpan Span => new(TokenStart + Green.Offset, Green.Width);

    /// <inheritdoc/>
    public override string ToString()
        => Value.ToString();

    #region equality

    public static bool operator ==(VirtualChar ch1, char ch2)
        => ch1.Green.Char == ch2;

    public static bool operator !=(VirtualChar ch1, char ch2)
        => !(ch1 == ch2);

    #endregion
}
