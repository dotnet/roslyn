// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class MarkupElementIntermediateNode : IntermediateNode
{
    public IEnumerable<HtmlAttributeIntermediateNode> Attributes => Children.OfType<HtmlAttributeIntermediateNode>();

    public IEnumerable<ReferenceCaptureIntermediateNode> Captures => Children.OfType<ReferenceCaptureIntermediateNode>();

    public IEnumerable<SetKeyIntermediateNode> SetKeys => Children.OfType<SetKeyIntermediateNode>();

    public IEnumerable<IntermediateNode> Body => Children.Where(c =>
    {
        return c is not (ComponentAttributeIntermediateNode or
            HtmlAttributeIntermediateNode or
            SplatIntermediateNode or
            SetKeyIntermediateNode or
            ReferenceCaptureIntermediateNode or
            FormNameIntermediateNode);
    });

    public override IntermediateNodeCollection Children { get => field ??= []; }

    public string TagName { get; set; }

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        if (visitor == null)
        {
            throw new ArgumentNullException(nameof(visitor));
        }

        visitor.VisitMarkupElement(this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        if (formatter == null)
        {
            throw new ArgumentNullException(nameof(formatter));
        }

        formatter.WriteContent(TagName);

        formatter.WriteProperty(nameof(TagName), TagName);
    }
}
