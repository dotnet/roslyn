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
    private const int MaxWidth = 10;
    private const int WidthMask = 0b1111; // 4 bits for width (max 10)
    private const int OffsetShift = 4;    // remaining bits for offset

    /// <summary>
    /// The value of this <see cref="VirtualCharGreen"/> as a <see cref="Rune"/> if such a representation is possible.
    /// <see cref="Rune"/>s can represent Unicode codepoints that can appear in a <see cref="string"/> except for
    /// unpaired surrogates.  If an unpaired high or low surrogate character is present, this value will be <see
    /// cref="Rune.ReplacementChar"/>.  The value of this character can be retrieved from
    /// <see cref="SurrogateChar"/>.
    /// </summary>
    public readonly Rune Rune;

    /// <summary>
    /// The unpaired high or low surrogate character that was encountered that could not be represented in <see
    /// cref="Rune"/>.  If <see cref="Rune"/> is not <see cref="Rune.ReplacementChar"/>, this will be <c>0</c>.
    /// </summary>
    public readonly char SurrogateChar;

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
    /// This can be as low as 1 (for normal characters) or up to 10 (for escape sequences like \UXXXXXXXX).
    /// </summary>
    public int Width => _offsetAndWidth & WidthMask;

    /// <summary>
    /// Creates a new <see cref="VirtualCharGreen"/> from the provided <paramref name="rune"/>.  This operation cannot
    /// fail.
    /// </summary>
    public static VirtualCharGreen Create(Rune rune, int offset, int width)
        => new(rune, surrogateChar: default, offset, width);

    /// <summary>
    /// Creates a new <see cref="VirtualCharGreen"/> from an unpaired high or low surrogate character.  This will throw
    /// if <paramref name="surrogateChar"/> is not actually a surrogate character. The resultant <see cref="Rune"/>
    /// value will be <see cref="Rune.ReplacementChar"/>.
    /// </summary>
    public static VirtualCharGreen Create(char surrogateChar, int offset, int width)
    {
        if (!char.IsSurrogate(surrogateChar))
            throw new ArgumentException("surrogateChar must be a surrogate code unit", nameof(surrogateChar));

        return new VirtualCharGreen(rune: Rune.ReplacementChar, surrogateChar, offset, width);
    }

    private VirtualCharGreen(Rune rune, char surrogateChar, int offset, int width)
    {
        Contract.ThrowIfFalse(surrogateChar == 0 || rune == Rune.ReplacementChar,
            "If surrogateChar is provided then rune must be Rune.ReplacementChar");
        Contract.ThrowIfTrue(width > MaxWidth);

        if (offset < 0)
            throw new ArgumentException("Offset cannot be negative", nameof(offset));

        if (width <= 0)
            throw new ArgumentException("Width must be greater than zero.", nameof(width));

        Rune = rune;
        SurrogateChar = surrogateChar;
        _offsetAndWidth = (offset << OffsetShift) | width;
    }

    /// <summary>
    /// Retrieves the scaler value of this character as an <see cref="int"/>.  If this is an unpaired surrogate
    /// character, this will be the value of that surrogate.  Otherwise, this will be the value of our <see
    /// cref="Rune"/>.
    /// </summary>
    public int Value => SurrogateChar != 0 ? SurrogateChar : Rune.Value;

    public VirtualCharGreen WithOffset(int offset)
        => new(this.Rune, this.SurrogateChar, offset, this.Width);

    public bool IsDigit
        => SurrogateChar != 0 ? char.IsDigit(SurrogateChar) : Rune.IsDigit(Rune);

    public bool IsLetter
        => SurrogateChar != 0 ? char.IsLetter(SurrogateChar) : Rune.IsLetter(Rune);

    public bool IsLetterOrDigit
        => SurrogateChar != 0 ? char.IsLetterOrDigit(SurrogateChar) : Rune.IsLetterOrDigit(Rune);

    public bool IsWhiteSpace
        => SurrogateChar != 0 ? char.IsWhiteSpace(SurrogateChar) : Rune.IsWhiteSpace(Rune);

    /// <inheritdoc cref="Rune.Utf16SequenceLength" />
    public int Utf16SequenceLength => SurrogateChar != 0 ? 1 : Rune.Utf16SequenceLength;

    #region string operations

    /// <inheritdoc/>
    public override string ToString()
        => SurrogateChar != 0 ? SurrogateChar.ToString() : Rune.ToString();

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

    #endregion
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

    private int CompareTo(char other)
        => this.Value - other;

    public static bool operator <(VirtualChar ch1, char ch2)
        => ch1.CompareTo(ch2) < 0;

    public static bool operator <=(VirtualChar ch1, char ch2)
        => ch1.CompareTo(ch2) <= 0;

    public static bool operator >(VirtualChar ch1, char ch2)
        => ch1.CompareTo(ch2) > 0;

    public static bool operator >=(VirtualChar ch1, char ch2)
        => ch1.CompareTo(ch2) >= 0;

    #endregion

    #region string operations

    /// <inheritdoc cref="VirtualCharGreen.ToString"/>
    public override string ToString() => Green.ToString();

    /// <inheritdoc cref="VirtualCharGreen.AppendTo"/>
    public void AppendTo(StringBuilder builder) => Green.AppendTo(builder);

    #endregion
}
