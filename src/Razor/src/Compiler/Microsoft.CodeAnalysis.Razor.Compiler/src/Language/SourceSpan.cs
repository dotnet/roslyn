// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Globalization;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language;

public readonly struct SourceSpan : IEquatable<SourceSpan>
{
    public static readonly SourceSpan Undefined = new SourceSpan(SourceLocation.Undefined, 0);

    public SourceSpan(int absoluteIndex, int length)
        : this(null, absoluteIndex, -1, -1, length)
    {
    }

    public SourceSpan(SourceLocation location, int contentLength)
        : this(location.FilePath, location.AbsoluteIndex, location.LineIndex, location.CharacterIndex, contentLength, lineCount: 1, endCharacterIndex: 0)
    {
    }

    public SourceSpan(string filePath, int absoluteIndex, int lineIndex, int characterIndex, int length)
        : this(filePath: filePath, absoluteIndex: absoluteIndex, lineIndex: lineIndex, characterIndex: characterIndex, length: length, lineCount: 0, endCharacterIndex: 0)
    {
    }

    public SourceSpan(string filePath, int absoluteIndex, int lineIndex, int characterIndex, int length, int lineCount, int endCharacterIndex)
    {
        AbsoluteIndex = absoluteIndex;
        LineIndex = lineIndex;
        CharacterIndex = characterIndex;
        Length = length;
        FilePath = filePath;
        LineCount = lineCount;
        EndCharacterIndex = endCharacterIndex;
    }

    public SourceSpan(int absoluteIndex, int lineIndex, int characterIndex, int length)
        : this(filePath: null, absoluteIndex: absoluteIndex, lineIndex: lineIndex, characterIndex: characterIndex, length: length)
    {
    }

    public int Length { get; }

    public int AbsoluteIndex { get; }

    public int LineIndex { get; }

    public int CharacterIndex { get; }

    public int LineCount { get; }

    public int EndCharacterIndex { get; }

    public string FilePath { get; }

    public bool Equals(SourceSpan other)
    {
        return
            string.Equals(FilePath, other.FilePath, StringComparison.Ordinal) &&
            AbsoluteIndex == other.AbsoluteIndex &&
            LineIndex == other.LineIndex &&
            CharacterIndex == other.CharacterIndex &&
            Length == other.Length;
    }

    public override bool Equals(object obj)
    {
        return obj is SourceSpan span && Equals(span);
    }

    public override int GetHashCode()
    {
        var hash = HashCodeCombiner.Start();
        hash.Add(FilePath, StringComparer.Ordinal);
        hash.Add(AbsoluteIndex);
        hash.Add(LineIndex);
        hash.Add(CharacterIndex);
        hash.Add(Length);

        return hash;
    }

    public override string ToString()
    {
        return string.Format(
            CultureInfo.CurrentCulture, "({0}:{1},{2} [{3}] {4})",
            AbsoluteIndex,
            LineIndex,
            CharacterIndex,
            Length,
            FilePath);
    }

    internal readonly SourceSpan GetZeroWidthEndSpan()
    {
        return new SourceSpan(FilePath, AbsoluteIndex + Length, LineIndex, characterIndex: EndCharacterIndex, length: 0, lineCount: 0, EndCharacterIndex);
    }

    internal readonly SourceSpan Slice(int startIndex, int length)
    {
        return new SourceSpan(FilePath, AbsoluteIndex + startIndex, LineIndex, CharacterIndex + startIndex, length, LineCount, endCharacterIndex: CharacterIndex + startIndex + length);
    }

    public static bool operator ==(SourceSpan left, SourceSpan right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SourceSpan left, SourceSpan right)
    {
        return !left.Equals(right);
    }

    public readonly int CompareByStartThenLength(SourceSpan other)
    {
        var cmpByStart = AbsoluteIndex.CompareTo(other.AbsoluteIndex);
        if (cmpByStart != 0)
        {
            return cmpByStart;
        }

        return Length.CompareTo(other.Length);
    }

    public readonly SourceSpan WithFilePath(string filePath) => new(filePath, AbsoluteIndex, LineIndex, CharacterIndex, Length, LineCount, EndCharacterIndex);
    public readonly SourceSpan WithAbsoluteIndex(int absoluteIndex) => new(FilePath, absoluteIndex, LineIndex, CharacterIndex, Length, LineCount, EndCharacterIndex);
    public readonly SourceSpan WithLineIndex(int lineIndex) => new(FilePath, AbsoluteIndex, lineIndex, CharacterIndex, Length, LineCount, EndCharacterIndex);
    public readonly SourceSpan WithCharacterIndex(int characterIndex) => new(FilePath, AbsoluteIndex, LineIndex, characterIndex, Length, LineCount, EndCharacterIndex);
    public readonly SourceSpan WithLength(int length) => new(FilePath, AbsoluteIndex, LineIndex, CharacterIndex, length, LineCount, EndCharacterIndex);
    public readonly SourceSpan WithLineCount(int lineCount) => new(FilePath, AbsoluteIndex, LineIndex, CharacterIndex, Length, lineCount, EndCharacterIndex);
    public readonly SourceSpan WithEndCharacterIndex(int endCharacterIndex) => new(FilePath, AbsoluteIndex, LineIndex, CharacterIndex, Length, LineCount, endCharacterIndex);
}
