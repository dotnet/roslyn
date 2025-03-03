// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Scope for analyzing a document for computing local syntax/semantic diagnostics.
/// </summary>
internal sealed class DocumentAnalysisScope
{
    private readonly Lazy<AdditionalText> _lazyAdditionalFile;

    public DocumentAnalysisScope(
        TextDocument document,
        TextSpan? span,
        ImmutableArray<DiagnosticAnalyzer> projectAnalyzers,
        ImmutableArray<DiagnosticAnalyzer> hostAnalyzers,
        AnalysisKind kind)
    {
        Debug.Assert(kind is AnalysisKind.Syntax or AnalysisKind.Semantic);
        Debug.Assert(!projectAnalyzers.IsDefault);
        Debug.Assert(!hostAnalyzers.IsDefault);
        Debug.Assert(!projectAnalyzers.IsEmpty || !hostAnalyzers.IsEmpty);

        TextDocument = document;
        Span = span;
        ProjectAnalyzers = projectAnalyzers;
        HostAnalyzers = hostAnalyzers;
        Kind = kind;

        _lazyAdditionalFile = new Lazy<AdditionalText>(ComputeAdditionalFile);
    }

    public TextDocument TextDocument { get; }
    public TextSpan? Span { get; }
    public ImmutableArray<DiagnosticAnalyzer> ProjectAnalyzers { get; }
    public ImmutableArray<DiagnosticAnalyzer> HostAnalyzers { get; }
    public AnalysisKind Kind { get; }

    /// <summary>
    /// Gets the <see cref="AdditionalText"/> corresponding to the <see cref="TextDocument"/>.
    /// NOTE: Throws an exception if <see cref="TextDocument"/> is not an <see cref="AdditionalDocument"/>.
    /// </summary>
    public AdditionalText AdditionalFile => _lazyAdditionalFile.Value;

    private AdditionalText ComputeAdditionalFile()
    {
        Contract.ThrowIfFalse(TextDocument is AdditionalDocument);

        var filePath = TextDocument.FilePath ?? TextDocument.Name;
        return TextDocument.Project.AnalyzerOptions.AdditionalFiles.First(a => PathUtilities.Comparer.Equals(a.Path, filePath));
    }

    public DocumentAnalysisScope WithSpan(TextSpan? span)
        => new(TextDocument, span, ProjectAnalyzers, HostAnalyzers, Kind);

    public DocumentAnalysisScope WithAnalyzers(ImmutableArray<DiagnosticAnalyzer> projectAnalyzers, ImmutableArray<DiagnosticAnalyzer> hostAnalyzers)
        => new(TextDocument, Span, projectAnalyzers, hostAnalyzers, Kind);
}
