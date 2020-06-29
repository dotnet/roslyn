// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Scope for analyzing a document for computing local syntax/semantic diagnostics.
    /// Used by <see cref="DocumentAnalysisExecutor"/>.
    /// </summary>
    internal sealed class DocumentAnalysisScope
    {
        public DocumentAnalysisScope(
            Document document,
            TextSpan? span,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            AnalysisKind kind)
        {
            Debug.Assert(kind == AnalysisKind.Syntax || kind == AnalysisKind.Semantic);
            Debug.Assert(!analyzers.IsDefaultOrEmpty);

            Document = document;
            Span = span;
            Analyzers = analyzers;
            Kind = kind;
        }

        public Document Document { get; }
        public TextSpan? Span { get; }
        public ImmutableArray<DiagnosticAnalyzer> Analyzers { get; }
        public AnalysisKind Kind { get; }
    }
}
