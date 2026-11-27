// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

public readonly record struct LinePragma
{
    public bool IsEnhanced { get; }
    public string FilePath { get; }
    public int StartLineIndex { get; }
    public int EndLineIndex { get; }

    public int LineCount => EndLineIndex - StartLineIndex;

    public LinePragma(bool isEnhanced, string filePath, int startLineIndex, int endLineIndex)
    {
        ArgHelper.ThrowIfNullOrWhiteSpace(filePath);
        ArgHelper.ThrowIfNegative(startLineIndex);
        ArgHelper.ThrowIfNegative(endLineIndex);

        IsEnhanced = isEnhanced;
        FilePath = filePath;
        StartLineIndex = startLineIndex;
        EndLineIndex = endLineIndex;
    }

    public override string ToString()
        => $"Line index {StartLineIndex}, Count {LineCount} - {FilePath}";
}
