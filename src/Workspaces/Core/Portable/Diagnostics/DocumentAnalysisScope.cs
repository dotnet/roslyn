// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Scope for analyzing a document for computing local syntax/semantic diagnostics.
    /// </summary>
    internal sealed class DocumentAnalysisScope
    {
        private readonly Lazy<AdditionalText> _lazyAdditionalFile;

        public DocumentAnalysisScope(
            TextDocument document,
            TextSpan? span,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            AnalysisKind kind)
        {
            Debug.Assert(kind is AnalysisKind.Syntax or AnalysisKind.Semantic);
            Debug.Assert(!analyzers.IsDefaultOrEmpty);

            TextDocument = document;
            Span = span;
            Analyzers = analyzers;
            Kind = kind;

            _lazyAdditionalFile = new Lazy<AdditionalText>(ComputeAdditionalOrAnalyzerConfigFile);
        }

        public TextDocument TextDocument { get; }
        public TextSpan? Span { get; }
        public ImmutableArray<DiagnosticAnalyzer> Analyzers { get; }
        public AnalysisKind Kind { get; }

        /// <summary>
        /// Gets the <see cref="AdditionalText"/> corresponding to the <see cref="TextDocument"/>.
        /// NOTE: Throws an exception if <see cref="TextDocument"/> is neither an <see cref="AdditionalDocument"/>
        /// nor an <see cref="AnalyzerConfigDocument"/>.
        /// </summary>
        public AdditionalText AdditionalFile => _lazyAdditionalFile.Value;

        private AdditionalText ComputeAdditionalOrAnalyzerConfigFile()
        {
            Contract.ThrowIfFalse(TextDocument is AdditionalDocument or AnalyzerConfigDocument);

            var filePath = TextDocument.FilePath ?? TextDocument.Name;
            var compare = (AdditionalText a) => PathUtilities.Comparer.Equals(a.Path, filePath);
            return TextDocument.Project.AnalyzerOptions.AdditionalFiles.FirstOrDefault(compare)
                ?? TextDocument.Project.AnalyzerOptions.AnalyzerConfigFiles.First(compare);
        }

        public DocumentAnalysisScope WithSpan(TextSpan? span)
            => new(TextDocument, span, Analyzers, Kind);

        public DocumentAnalysisScope WithAnalyzers(ImmutableArray<DiagnosticAnalyzer> analyzers)
            => new(TextDocument, Span, analyzers, Kind);
    }
}
