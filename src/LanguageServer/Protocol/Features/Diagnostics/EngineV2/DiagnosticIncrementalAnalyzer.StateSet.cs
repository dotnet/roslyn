// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal partial class DiagnosticAnalyzerService
{
    private partial class DiagnosticIncrementalAnalyzer
    {
        /// <summary>
        /// this contains all states regarding a <see cref="DiagnosticAnalyzer"/>
        /// </summary>
        private sealed class StateSet
        {
            public readonly DiagnosticAnalyzer Analyzer;
            public readonly bool IsHostAnalyzer;

            private readonly ConcurrentSet<DocumentId> _activeDocuments;

            public StateSet(DiagnosticAnalyzer analyzer, bool isHostAnalyzer)
            {
                Analyzer = analyzer;
                IsHostAnalyzer = isHostAnalyzer;

                _activeDocuments = [];
            }

            public bool IsActiveFile(DocumentId documentId)
                => _activeDocuments.Contains(documentId);

            public void AddActiveDocument(DocumentId documentId)
                => _activeDocuments.Add(documentId);
        }
    }
}
