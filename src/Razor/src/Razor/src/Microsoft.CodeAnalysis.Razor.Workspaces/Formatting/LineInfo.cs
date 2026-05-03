// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis.Razor.Formatting;

/// <summary>
/// Represents the state of a line in the generated C# document.
/// </summary>
/// <param name="ProcessIndentation">Whether the formatted document text to the left the first non-whitespace character should be applied to the origin document</param>
/// <param name="ProcessFormatting">Whether the formatted document text to the right of the first non-whitespace character should be applied to the origin document</param>
/// <param name="CheckForNewLines">Whether the origin document text could have overflowed to multiple lines in the formatted document</param>
/// <param name="SkippedPreviousLineOriginOffset">The offset into the previous original line of a skipped formatted line, or <see langword="null"/> if there is no skipped previous line.</param>
/// <param name="SkipNextLine">Whether to skip the next line in the formatted document, since it doesn't represent anything in the origin document</param>
/// <param name="SkipNextLineIfBrace">Whether to skip the next line in the formatted document, like <see cref="SkipNextLine" />, but only skips if the next line is a brace</param>
/// <param name="FixedIndentLevel">The indent level that the Html formatter applied to this line</param>
/// <param name="OriginOffset">How many characters after the first non-whitespace character of the origin line should be skipped before applying formatting</param>
/// <param name="FormattedLength">How many characters of the origin line the formatted line represents</param>
/// <param name="FormattedOffset">How many characters after the first non-whitespace character of the formatted line should be skipped before applying formatting</param>
/// <param name="FormattedOffsetFromEndOfLine">How many characters before the end of the formatted line should be skipped before applying formatting</param>
/// <param name="AdditionalIndentation">Additional indentation width to apply to this line, in columns. Can be negative.</param>
internal readonly record struct LineInfo(
    bool ProcessIndentation,
    bool ProcessFormatting,
    bool CheckForNewLines,
    int? SkippedPreviousLineOriginOffset,
    bool SkipNextLine,
    bool SkipNextLineIfBrace,
    int FixedIndentLevel,
    int OriginOffset,
    int FormattedLength,
    int FormattedOffset,
    int FormattedOffsetFromEndOfLine,
    int? AdditionalIndentation);
