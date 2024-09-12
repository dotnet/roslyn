// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.MSBuild.Logging
{
    internal class DiagnosticLog : IEnumerable<DiagnosticLogItem>
    {
        private readonly List<DiagnosticLogItem> _items;

        public int Count => _items.Count;
        public DiagnosticLogItem this[int index] => _items[index];
        public bool IsEmpty => _items.Count == 0;

        public bool HasFailure
            => _items.Any(i => i.Kind == WorkspaceDiagnosticKind.Failure);

        public DiagnosticLog()
            => _items = [];

        public IEnumerator<DiagnosticLogItem> GetEnumerator()
            => _items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public void Add(DiagnosticLogItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            _items.Add(item);
        }

        public void Add(string message, string projectFilePath, WorkspaceDiagnosticKind kind = WorkspaceDiagnosticKind.Failure)
            => _items.Add(new DiagnosticLogItem(kind, message, projectFilePath));

        public void Add(Exception exception, string projectFilePath, WorkspaceDiagnosticKind kind = WorkspaceDiagnosticKind.Failure)
            => _items.Add(new DiagnosticLogItem(kind, exception.Message, projectFilePath));
    }
}
