// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.LanguageServer.Protocol;

internal static partial class LspExtensions
{
    public static RazorTextSpan ToRazorTextSpan(this LspRange range, SourceText sourceText)
    {
        var textSpan = sourceText.GetTextSpan(range);
        return new()
        {
            Start = textSpan.Start,
            Length = textSpan.Length,
        };
    }

    public static void Deconstruct(this LspRange range, out Position start, out Position end)
        => (start, end) = (range.Start, range.End);

    public static void Deconstruct(this LspRange range, out int startLine, out int startCharacter, out int endLine, out int endCharacter)
        => (startLine, startCharacter, endLine, endCharacter) = (range.Start.Line, range.Start.Character, range.End.Line, range.End.Character);

    public static LinePositionSpan ToLinePositionSpan(this LspRange range)
        => new(range.Start.ToLinePosition(), range.End.ToLinePosition());

    public static bool IntersectsOrTouches(this LspRange range, LspRange other)
    {
        if (range.IsBefore(other))
        {
            return false;
        }

        if (range.IsAfter(other))
        {
            return false;
        }

        return true;
    }

    private static bool IsBefore(this LspRange range, LspRange other) =>
        range.End.Line < other.Start.Line || (range.End.Line == other.Start.Line && range.End.Character < other.Start.Character);

    private static bool IsAfter(this LspRange range, LspRange other) =>
        other.End.Line < range.Start.Line || (other.End.Line == range.Start.Line && other.End.Character < range.Start.Character);

    public static bool OverlapsWith(this LspRange range, LspRange other)
    {
        return range.ToLinePositionSpan().OverlapsWith(other.ToLinePositionSpan());
    }

    public static bool LineOverlapsWith(this LspRange range, LspRange other)
    {
        var overlapStart = range.Start.Line;
        if (range.Start.Line.CompareTo(other.Start.Line) < 0)
        {
            overlapStart = other.Start.Line;
        }

        var overlapEnd = range.End.Line;
        if (range.End.Line.CompareTo(other.End.Line) > 0)
        {
            overlapEnd = other.End.Line;
        }

        return overlapStart.CompareTo(overlapEnd) <= 0;
    }

    public static bool Contains(this LspRange range, LspRange other)
    {
        return range.Start.CompareTo(other.Start) <= 0 && range.End.CompareTo(other.End) >= 0;
    }

    public static bool SpansMultipleLines(this LspRange range)
    {
        return range.Start.Line != range.End.Line;
    }

    public static bool IsSingleLine(this LspRange range)
    {
        return range.Start.Line == range.End.Line;
    }

    public static bool IsUndefined(this LspRange range)
    {
        return range == LspFactory.UndefinedRange;
    }

    public static bool IsZeroWidth(this LspRange range)
    {
        return range.Start == range.End;
    }

    public static int CompareTo(this LspRange range1, LspRange range2)
    {
        var result = range1.Start.CompareTo(range2.Start);

        if (result == 0)
        {
            result = range1.End.CompareTo(range2.End);
        }

        return result;
    }

    public static LspRange? Overlap(this LspRange range, LspRange other)
    {
        var overlapStart = range.Start;
        if (range.Start.CompareTo(other.Start) < 0)
        {
            overlapStart = other.Start;
        }

        var overlapEnd = range.End;
        if (range.End.CompareTo(other.End) > 0)
        {
            overlapEnd = other.End;
        }

        // Empty ranges do not overlap with any range.
        if (overlapStart.CompareTo(overlapEnd) < 0)
        {
            return LspFactory.CreateRange(overlapStart, overlapEnd);
        }

        return null;
    }

    public static string ToDisplayString(this LspRange range)
        => $"{range.Start.ToDisplayString()}-{range.End.ToDisplayString()}";
}
