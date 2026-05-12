// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Protocol.DocumentHighlight;

[DataContract]
internal readonly record struct RemoteDocumentHighlight(
    [property: DataMember(Order = 0)] LinePositionSpan Span,
    [property: DataMember(Order = 1)] DocumentHighlightKind Kind)
{
    public static RemoteDocumentHighlight FromLspDocumentHighlight(LspDocumentHighlight highlight)
        => new(highlight.Range.ToLinePositionSpan(), highlight.Kind);

    public static LspDocumentHighlight ToLspDocumentHighlight(RemoteDocumentHighlight highlight)
        => new()
        {
            Range = highlight.Span.ToRange(),
            Kind = highlight.Kind
        };
}
