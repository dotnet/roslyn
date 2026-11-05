// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal sealed class EmptyLayoutIntermediateNode : ExtensionIntermediateNode
{
    public const string TypeName = "__EmptyLayout";

    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    public override void Accept(IntermediateNodeVisitor visitor) => AcceptExtensionNode(this, visitor);

    protected override IntermediateNode CloneNode()
        => new EmptyLayoutIntermediateNode { IsSynthesizedHelper = IsSynthesizedHelper };

    public override void WriteNode(CodeTarget target, CodeRenderingContext context)
    {
        var writer = context.CodeWriter;
        writer.WriteLine($"file sealed class {TypeName} : global::{ComponentsApi.LayoutComponentBase.FullTypeName}");
        using (writer.BuildScope())
        {
            writer.WriteLine("#pragma warning disable 1998");
            writer.WriteLine($"protected override void {ComponentsApi.ComponentBase.BuildRenderTree}(global::{ComponentsApi.RenderTreeBuilder.FullTypeName} {ComponentsApi.RenderTreeBuilder.BuilderParameter})");
            using (writer.BuildScope())
            {
                // Forward Body unchanged so the explicit layout adds no wrapper around the page.
                writer.WriteLine($"{ComponentsApi.RenderTreeBuilder.BuilderParameter}.{ComponentsApi.RenderTreeBuilder.AddContent}(0, {ComponentsApi.LayoutComponentBase.Body});");
            }
            writer.WriteLine("#pragma warning restore 1998");
        }
    }
}
