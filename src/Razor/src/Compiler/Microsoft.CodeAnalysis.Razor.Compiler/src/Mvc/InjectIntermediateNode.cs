// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

public class InjectIntermediateNode : ExtensionIntermediateNode
{
    public string TypeName { get; set; }

    public SourceSpan? TypeSource { get; set; }

    public string MemberName { get; set; }

    public SourceSpan? MemberSource { get; set; }

    public bool IsMalformed { get; set; }

    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        if (visitor == null)
        {
            throw new ArgumentNullException(nameof(visitor));
        }

        AcceptExtensionNode<InjectIntermediateNode>(this, visitor);
    }

    public override void WriteNode(CodeTarget target, CodeRenderingContext context)
    {
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var extension = target.GetExtension<IInjectTargetExtension>();
        if (extension == null)
        {
            ReportMissingCodeTargetExtension<IInjectTargetExtension>(context);
            return;
        }

        extension.WriteInjectProperty(context, this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(MemberName);

        formatter.WriteProperty(nameof(MemberName), MemberName);
        formatter.WriteProperty(nameof(TypeName), TypeName);
        formatter.WriteProperty(nameof(IsMalformed), IsMalformed.ToString());
    }
}
