// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Threading;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.EmbeddedLanguages;

/// <inheritdoc cref="IEmbeddedLanguageDocumentHighlighter"/>
internal interface IAspNetCoreEmbeddedLanguageDocumentHighlighter
{
    /// <inheritdoc cref="IEmbeddedLanguageDocumentHighlighter.GetDocumentHighlights"/>
    ImmutableArray<AspNetCoreDocumentHighlights> GetDocumentHighlights(
        SemanticModel semanticModel,
        SyntaxToken token,
        int position,
        CancellationToken cancellationToken);
}

internal enum AspNetCoreHighlightSpanKind
{
    None,
    Definition,
    Reference,
    WrittenReference,
}

[DataContract]
internal readonly struct AspNetCoreHighlightSpan
{
    [DataMember(Order = 0)]
    public TextSpan TextSpan { get; }

    [DataMember(Order = 1)]
    public AspNetCoreHighlightSpanKind Kind { get; }

    public AspNetCoreHighlightSpan(TextSpan textSpan, AspNetCoreHighlightSpanKind kind) : this()
    {
        TextSpan = textSpan;
        Kind = kind;
    }
}

internal readonly struct AspNetCoreDocumentHighlights
{
    public ImmutableArray<AspNetCoreHighlightSpan> HighlightSpans { get; }

    public AspNetCoreDocumentHighlights(ImmutableArray<AspNetCoreHighlightSpan> highlightSpans)
    {
        HighlightSpans = highlightSpans;
    }
}
