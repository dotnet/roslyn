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
    /// Set by <see cref="DefaultRazorMarkupSplitPhase"/> once it has produced the decl C# document and
    /// rewritten this node into the impl half (before tag-helper resolution). Signals the final C#
    /// lowering phase to emit this node directly as the impl half instead of deriving an impl spine from
    /// a single classified tree.
    /// </summary>
    public bool IsSplitImplDocument { get; set; }

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
