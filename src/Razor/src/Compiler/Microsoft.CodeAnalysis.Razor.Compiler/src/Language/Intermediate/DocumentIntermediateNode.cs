// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class DocumentIntermediateNode : IntermediateNode
{
    public override IntermediateNodeCollection Children { get => field ??= []; }

    public string DocumentKind { get; set; }

    /// <summary>
    /// For a component document, the markup-free decl subtree that <see cref="DefaultRazorMarkupSplitPhase"/>
    /// captures for <see cref="DefaultRazorDeclCSharpLoweringPhase"/> to lower into the decl C# document
    /// before tag-helper discovery: the full declaration surface when the component splits completely, or
    /// a bodiless type shell when it can't (so its type still resolves in the pre-compilation
    /// compilation). Null for any other document -- a non-component, or a component with a suppressed
    /// primary body or no primary class.
    /// </summary>
    internal DocumentIntermediateNode DeclDocumentNode { get; set; }

    /// <summary>
    /// The namespace-qualified type name of a component document that could not be split completely, so its
    /// tag-helper descriptor must be produced by the separate declaration engine (the fallback discovery
    /// path) rather than read from the pre-compilation compilation; <see langword="null"/> for a component
    /// that split completely or for a non-component. Set by <see cref="DefaultRazorMarkupSplitPhase"/>. A
    /// non-null value both marks the document for fallback discovery and carries the name the source
    /// generator matches to route its descriptor through slow discovery over the augmented compilation
    /// instead of fast discovery, without re-deriving the name from the generated code. Independent of
    /// <see cref="DeclDocumentNode"/>: a fallback component with a primary class also emits a type-shell
    /// decl for resolution.
    /// </summary>
    internal string FallbackComponentTypeName { get; set; }

    public RazorCodeGenerationOptions Options { get; set; }

    public CodeTarget Target { get; set; }

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        if (visitor == null)
        {
            throw new ArgumentNullException(nameof(visitor));
        }

        visitor.VisitDocument(this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(DocumentKind);

        formatter.WriteProperty(nameof(DocumentKind), DocumentKind);
    }

    protected override IntermediateNode CloneNode()
    {
        // The declaration subtree is already lowered and inert during replay, so it is shared by reference.
        var clone = new DocumentIntermediateNode
        {
            DocumentKind = DocumentKind,
            DeclDocumentNode = DeclDocumentNode,
            FallbackComponentTypeName = FallbackComponentTypeName,
            Options = Options,
            Target = Target,
        };

        return clone;
    }
}
