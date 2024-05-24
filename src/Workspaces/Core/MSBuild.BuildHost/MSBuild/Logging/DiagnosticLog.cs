// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.MSBuild
{
    internal readonly struct DiagnosticLog() : IEnumerable<DiagnosticLogItem>
    {
        private readonly List<DiagnosticLogItem> _items = [];

        public int Count => _items.Count;
        public DiagnosticLogItem this[int index] => _items[index];
        public bool IsEmpty => _items.Count == 0;

        public IEnumerator<DiagnosticLogItem> GetEnumerator()
            => _items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public void Add(DiagnosticLogItem item)
            => _items.Add(item);

        public void Add(string message, string projectFilePath, DiagnosticLogItemKind kind = DiagnosticLogItemKind.Error)
            => _items.Add(new DiagnosticLogItem(kind, message, projectFilePath));

        public void Add(Exception exception, string projectFilePath, DiagnosticLogItemKind kind = DiagnosticLogItemKind.Error)
            => _items.Add(new DiagnosticLogItem(kind, exception.Message, projectFilePath));
    }
}
