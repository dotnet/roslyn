// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        public bool HasFailure =>
            _items.Any(i => i.Kind == WorkspaceDiagnosticKind.Failure);

        public DiagnosticLog()
            => _items = new List<DiagnosticLogItem>();

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
