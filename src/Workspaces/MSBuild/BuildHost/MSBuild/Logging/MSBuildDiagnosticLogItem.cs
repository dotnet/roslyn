// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed class MSBuildDiagnosticLogItem(
    DiagnosticLogItemKind kind,
    string projectFilePath,
    string message,
    string fileName,
    int lineNumber,
    int columnNumber)
    : DiagnosticLogItem(kind, message, projectFilePath)
{
    public string FileName { get; } = fileName;
    public int LineNumber { get; } = lineNumber;
    public int ColumnNumber { get; } = columnNumber;

    public override string ToString()
        => $"{FileName}: ({LineNumber}, {ColumnNumber}): {Message}";
}
