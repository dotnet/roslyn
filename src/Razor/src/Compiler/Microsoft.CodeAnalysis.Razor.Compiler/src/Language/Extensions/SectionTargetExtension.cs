// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

public sealed class SectionTargetExtension : ISectionTargetExtension
{
    public static readonly string DefaultSectionMethodName = "DefineSection";

    public string SectionMethodName { get; set; } = DefaultSectionMethodName;

    public void WriteSection(CodeRenderingContext context, SectionIntermediateNode node)
    {
        context.CodeWriter
            .WriteStartMethodInvocation(SectionMethodName)
            .Write("\"")
            .Write(node.SectionName)
            .Write("\", ");

        using (context.CodeWriter.BuildAsyncLambda())
        {
            context.RenderChildren(node);
        }

        context.CodeWriter.WriteEndMethodInvocation(endLine: true);
    }
}
