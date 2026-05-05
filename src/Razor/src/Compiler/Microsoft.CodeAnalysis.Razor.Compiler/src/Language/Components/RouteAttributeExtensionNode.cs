// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal sealed class RouteAttributeExtensionNode(string template) : ExtensionIntermediateNode
{
    public string Template { get; } = template;

    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    public override void Accept(IntermediateNodeVisitor visitor) => AcceptExtensionNode(this, visitor);

    public override void WriteNode(CodeTarget target, CodeRenderingContext context)
    {
        context.CodeWriter.Write("[global::");
        context.CodeWriter.Write(ComponentsApi.RouteAttribute.FullTypeName);
        if (context.Options.DesignTime)
        {
            context.CodeWriter.Write("(");
            context.CodeWriter.Write(Template);
        }
        else
        {
            context.CodeWriter.WriteLine("(");
            context.CodeWriter.WriteLine("// language=Route,Component");
            using (context.BuildEnhancedLinePragma(Source))
            {
                context.CodeWriter.WriteLine(Template);
            }
        }
        context.CodeWriter.WriteLine(")]");
    }
}
