// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.MSBuild;

internal readonly struct DiagnosticLog() : IEnumerable<DiagnosticLogItem>
{
    private readonly List<DiagnosticLogItem> _items = [];

    // The reads below are intentionally not locked: a given DiagnosticLog is only read once the evaluation or
    // build it records has completed (e.g. via GetEvaluationDiagnosticLogItems, or when the results of building
    // a target are returned), so no Add can be in flight concurrently with a read.
    public int Count => _items.Count;
    public DiagnosticLogItem this[int index] => _items[index];
    public bool IsEmpty => _items.Count == 0;

    public IEnumerator<DiagnosticLogItem> GetEnumerator()
        => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public void Add(DiagnosticLogItem item)
    {
        lock (_items)
        {
            _items.Add(item);
        }
    }

    public void Add(string message, string projectFilePath, DiagnosticLogItemKind kind = DiagnosticLogItemKind.Error)
        => Add(new DiagnosticLogItem(kind, message, projectFilePath));

    public void Add(Exception exception, string projectFilePath, DiagnosticLogItemKind kind = DiagnosticLogItemKind.Error)
    {
        if (exception is MSB.Exceptions.InvalidProjectFileException invalidProjectFile)
        {
            // MSBuild reports an empty file, an empty code and a position of 0 when they're unknown.
            Add(new DiagnosticLogItem(
                kind,
                exception.Message,
                projectFilePath,
                string.IsNullOrEmpty(invalidProjectFile.ProjectFile) ? null : invalidProjectFile.ProjectFile,
                invalidProjectFile.LineNumber > 0 ? invalidProjectFile.LineNumber : null,
                invalidProjectFile.ColumnNumber > 0 ? invalidProjectFile.ColumnNumber : null,
                string.IsNullOrEmpty(invalidProjectFile.ErrorCode) ? null : invalidProjectFile.ErrorCode));
        }
        else
        {
            Add(new DiagnosticLogItem(kind, exception.Message, projectFilePath));
        }
    }
}
