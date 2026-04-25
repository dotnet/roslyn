// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

internal class MetadataAttributeTargetExtension : IMetadataAttributeTargetExtension
{
    public string CompiledItemAttributeName { get; set; } = "global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute";

    public string SourceChecksumAttributeName { get; set; } = "global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute";

    public string CompiledItemMetadataAttributeName { get; set; } = "global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemMetadataAttribute";


    public void WriteRazorCompiledItemAttribute(CodeRenderingContext context, RazorCompiledItemAttributeIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        // [assembly: global::...RazorCompiledItem(typeof({node.TypeName}), @"{node.Kind}", @"{node.Identifier}")]
        context.CodeWriter.Write("[assembly: ");
        context.CodeWriter.Write(CompiledItemAttributeName);
        context.CodeWriter.Write("(typeof(");
        context.CodeWriter.Write(node.TypeName);
        context.CodeWriter.Write("), @\"");
        context.CodeWriter.Write(node.Kind);
        context.CodeWriter.Write("\", @\"");
        context.CodeWriter.Write(node.Identifier);
        context.CodeWriter.WriteLine("\")]");
    }

    public void WriteRazorCompiledItemMetadataAttribute(CodeRenderingContext context, RazorCompiledItemMetadataAttributeIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        // [assembly: global::...RazorCompiledItemAttribute(@"{node.Key}", @"{node.Value}")]
        context.CodeWriter.Write("[");
        context.CodeWriter.Write(CompiledItemMetadataAttributeName);
        context.CodeWriter.Write("(");
        context.CodeWriter.WriteStringLiteral(node.Key);
        context.CodeWriter.Write(", ");
        if (node.Source.HasValue && !context.Options.DesignTime)
        {
            context.CodeWriter.WriteLine();
            if (node.ValueStringSyntax is not null)
            {
                context.CodeWriter.Write("// language=");
                context.CodeWriter.WriteLine(node.ValueStringSyntax);
            }
            using (context.BuildEnhancedLinePragma(node.Source))
            {
                context.AddSourceMappingFor(node);
                context.CodeWriter.WriteStringLiteral(node.Value);
            }
        }
        else
        {
            context.CodeWriter.WriteStringLiteral(node.Value);
        }
        context.CodeWriter.WriteLine(")]");
    }

    public void WriteRazorSourceChecksumAttribute(CodeRenderingContext context, RazorSourceChecksumAttributeIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        // [global::...RazorSourceChecksum(@"{node.ChecksumAlgorithm}", @"{node.Checksum}", @"{node.Identifier}")]
        context.CodeWriter.Write("[");
        context.CodeWriter.Write(SourceChecksumAttributeName);
        context.CodeWriter.Write("(@\"");
        context.CodeWriter.Write(node.ChecksumAlgorithm.ToString());
        context.CodeWriter.Write("\", @\"");
        context.CodeWriter.Write(ChecksumUtilities.BytesToString(node.Checksum));
        context.CodeWriter.Write("\", @\"");
        context.CodeWriter.Write(node.Identifier);
        context.CodeWriter.WriteLine("\")]");
    }
}
