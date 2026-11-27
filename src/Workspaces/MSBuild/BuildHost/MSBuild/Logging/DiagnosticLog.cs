// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.MSBuild;

internal readonly struct DiagnosticLog() : IEnumerable<DiagnosticLogItem>
{
    private readonly List<DiagnosticLogItem> _items = [];

    // The reads below are intentionally not locked: a given DiagnosticLog is only read (e.g. via
    // GetDiagnosticLogItems) once its build has completed, so no Add can be in flight concurrently
    // with a read.
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
        => Add(new DiagnosticLogItem(kind, exception.Message, projectFilePath));
}
