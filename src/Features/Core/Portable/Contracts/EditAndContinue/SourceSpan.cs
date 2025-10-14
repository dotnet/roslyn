// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Contracts.EditAndContinue;

/// <summary>
/// The start/end line/column ranges for a contiguous span of text. These should be all zero-indexed.
/// This is an alias for TextSpan structures.
/// </summary>
[DataContract]
[DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
internal readonly struct SourceSpan : IEquatable<SourceSpan>
{
    /// <summary>
    /// Creates a TextSpan.
    /// </summary>
    /// <param name="startLine">Start line.</param>
    /// <param name="startColumn">Start column. -1 if column information is missing.</param>
    /// <param name="endLine">End line.</param>
    /// <param name="endColumn">End column. -1 if column information is missing.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// If <paramref name="startLine"/> or <paramref name="endLine"/> is less than zero.
    /// If <paramref name="startColumn"/> or <paramref name="endColumn"/> is less than -1.
    /// If only <paramref name="startColumn"/> or <paramref name="endColumn"/> is -1.
    /// </exception>
    public SourceSpan(
        int startLine,
        int startColumn,
        int endLine,
        int endColumn)
    {
        if (startLine < 0)
            throw new ArgumentOutOfRangeException(nameof(startLine));
        if (startColumn < -1)
            throw new ArgumentOutOfRangeException(nameof(startColumn));
        if (endLine < 0)
            throw new ArgumentOutOfRangeException(nameof(endLine));
        if (endColumn < -1)
            throw new ArgumentOutOfRangeException(nameof(endColumn));

        if ((startColumn == -1 || endColumn == -1) && startColumn != endColumn)
            throw new ArgumentOutOfRangeException(startColumn == -1 ? nameof(endColumn) : nameof(startColumn));

        StartLine = startLine;
        StartColumn = startColumn;
        EndLine = endLine;
        EndColumn = endColumn;
    }

    /// <summary>
    /// Zero-based integer for the starting source line.
    /// </summary>
    [DataMember(Name = "startLine")]
    public int StartLine { get; }

    /// <summary>
    /// Zero-based integer for the starting source column. If column information is missing (e.g. language service doesn't support it), 
    /// this value should be treated as -1.
    /// </summary>
    [DataMember(Name = "startColumn")]
    public int StartColumn { get; }

    /// <summary>
    /// Zero-based integer for the ending source line.
    /// </summary>
    [DataMember(Name = "endLine")]
    public int EndLine { get; }

    /// <summary>
    /// Zero-based integer for the ending source column. If column information is missing (e.g. language service doesn't support it), 
    /// this value should be treated as -1.
    /// </summary>
    [DataMember(Name = "endColumn")]
    public int EndColumn { get; }

    public bool Equals(SourceSpan other)
    {
        return StartLine == other.StartLine &&
            StartColumn == other.StartColumn &&
            EndLine == other.EndLine &&
            EndColumn == other.EndColumn;
    }

    public override bool Equals(object? obj) => obj is SourceSpan span && Equals(span);

    public override int GetHashCode()
    {
        return
            ((StartLine & 0xffff) << 16) |       // bytes 3, 2 are a hash for start line
            ((StartColumn & 0xff) << 8) |        // byte 1 is a hash for start column
            ((EndLine ^ EndColumn) % 255);  // byte 0 is a hash for end line and column
    }

    public static bool operator ==(SourceSpan left, SourceSpan right) => left.Equals(right);

    public static bool operator !=(SourceSpan left, SourceSpan right) => !(left == right);

    internal string GetDebuggerDisplay()
        => (StartColumn >= 0)
            ? $"({StartLine},{StartColumn})-({EndLine},{EndColumn})"
            : $"{StartLine}-{EndLine}";
}
