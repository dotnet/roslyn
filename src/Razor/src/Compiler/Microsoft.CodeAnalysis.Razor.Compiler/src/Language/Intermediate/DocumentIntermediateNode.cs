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
    /// The markup-free decl subtree captured by <see cref="DefaultRazorMarkupSplitPhase"/> when it split
    /// this component, for <see cref="DefaultRazorDeclCSharpLoweringPhase"/> to lower into the decl C#
    /// document before tag-helper discovery. Null when the document wasn't split.
    /// </summary>
    internal DocumentIntermediateNode DeclDocumentNode { get; set; }

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
}
