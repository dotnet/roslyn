// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

internal readonly struct VSTypeScriptDocumentSpan(Document document, TextSpan sourceSpan)
{
    public Document Document { get; } = document;
    public TextSpan SourceSpan { get; } = sourceSpan;

    internal VSTypeScriptDocumentSpan(DocumentSpan span)
        : this(span.Document, span.SourceSpan)
    {
    }

    internal DocumentSpan ToDocumentSpan()
        => new(Document, SourceSpan);
}
