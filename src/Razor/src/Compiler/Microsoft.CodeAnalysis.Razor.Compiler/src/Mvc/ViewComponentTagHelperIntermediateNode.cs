// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

public sealed class ViewComponentTagHelperIntermediateNode(string className, TagHelperDescriptor tagHelper) : ExtensionIntermediateNode
{
    public string ClassName { get; } = className;
    public TagHelperDescriptor TagHelper { get; } = tagHelper;

    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    public override void Accept(IntermediateNodeVisitor visitor)
        => AcceptExtensionNode(this, visitor);

    public override void WriteNode(CodeTarget target, CodeRenderingContext context)
    {
        var extension = target.GetExtension<IViewComponentTagHelperTargetExtension>();
        if (extension == null)
        {
            ReportMissingCodeTargetExtension<IViewComponentTagHelperTargetExtension>(context);
            return;
        }

        extension.WriteViewComponentTagHelper(context, this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(ClassName);

        formatter.WriteProperty(nameof(ClassName), ClassName);
        formatter.WriteProperty(nameof(TagHelper), TagHelper?.DisplayName);
    }
}
