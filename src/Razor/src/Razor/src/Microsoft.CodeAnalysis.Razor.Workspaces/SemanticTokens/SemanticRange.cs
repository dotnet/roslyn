// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.SemanticTokens;

internal readonly struct SemanticRange : IComparable<SemanticRange>
{
    public SemanticRange(int kind, int startLine, int startCharacter, int endLine, int endCharacter, int modifier, bool fromRazor, bool isCSharpWhitespace)
    {
        Kind = kind;
        StartLine = startLine;
        StartCharacter = startCharacter;
        EndLine = endLine;
        EndCharacter = endCharacter;
        Modifier = modifier;
        FromRazor = fromRazor;
        IsCSharpWhitespace = isCSharpWhitespace;
    }

    public SemanticRange(int kind, LinePositionSpan range, int modifier, bool fromRazor)
        : this(kind, range.Start, range.End, modifier, fromRazor)
    {
    }

    public SemanticRange(int kind, LinePosition start, LinePosition end, int modifier, bool fromRazor)
        : this(kind, start.Line, start.Character, end.Line, end.Character, modifier, fromRazor, isCSharpWhitespace: false)
    {
    }

    public SemanticRange(int kind, int startLine, int startCharacter, int endLine, int endCharacter, int modifier, bool fromRazor)
        : this(kind, startLine, startCharacter, endLine, endCharacter, modifier, fromRazor, isCSharpWhitespace: false)
    {
    }

    public int Kind { get; }

    public int StartLine { get; }
    public int EndLine { get; }
    public int StartCharacter { get; }
    public int EndCharacter { get; }

    public int Modifier { get; }

    /// <summary>
    /// If we produce a token, and a delegated server produces a token, we want to prefer ours, so we use this flag to help our
    /// sort algorithm, that way we can avoid the perf hit of actually finding duplicates, and just take the first instance that
    /// covers a range.
    /// </summary>
    public bool FromRazor { get; }

    public bool IsCSharpWhitespace { get; }

    public LinePositionSpan AsLinePositionSpan()
        => new(new(StartLine, StartCharacter), new(EndLine, EndCharacter));

    public int CompareTo(SemanticRange other)
    {
        var result = StartLine.CompareTo(other.StartLine);
        if (result != 0)
        {
            return result;
        }

        result = StartCharacter.CompareTo(other.StartCharacter);
        if (result != 0)
        {
            return result;
        }

        // If the start positions are the same, and one is Razor and one isn't, we prefer Razor. This allows a Razor
        // produced token to win over multiple C# tokens, for example the tag name in "<My.Cool.Component>" is one
        // Razor classification (for component) but multiple C# classifications (2 namespaces and a type name)
        if (FromRazor && !other.FromRazor)
        {
            return -1;
        }
        else if (other.FromRazor && !FromRazor)
        {
            return 1;
        }

        result = EndLine.CompareTo(other.EndLine);
        if (result != 0)
        {
            return result;
        }

        result = EndCharacter.CompareTo(other.EndCharacter);
        if (result != 0)
        {
            return result;
        }

        return 0;
    }

    public override string ToString()
        => $"[Kind: {Kind}, StartLine: {StartLine}, StartCharacter: {StartCharacter}, EndLine: {EndLine}, EndCharacter: {EndCharacter}]";
}
