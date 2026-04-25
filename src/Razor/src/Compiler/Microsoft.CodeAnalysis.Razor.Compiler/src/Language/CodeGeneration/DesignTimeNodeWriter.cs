// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

public class DesignTimeNodeWriter : IntermediateNodeWriter
{
    public static readonly DesignTimeNodeWriter Instance = new DesignTimeNodeWriter();

    private DesignTimeNodeWriter()
    {
    }

    public override void WriteUsingDirective(CodeRenderingContext context, UsingDirectiveIntermediateNode node)
    {
        if (node.Source is { FilePath: not null } sourceSpan)
        {
            using (context.BuildLinePragma(sourceSpan, suppressLineDefaultAndHidden: !node.AppendLineDefaultAndHidden))
            {
                context.AddSourceMappingFor(node);
                context.CodeWriter.WriteUsing(node.Content);
            }
        }
        else
        {
            context.CodeWriter.WriteUsing(node.Content);

            if (node.AppendLineDefaultAndHidden)
            {
                context.CodeWriter.WriteLine("#line default");
                context.CodeWriter.WriteLine("#line hidden");
            }
        }
    }

    public override void WriteCSharpExpression(CodeRenderingContext context, CSharpExpressionIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (node.Children.Count == 0)
        {
            return;
        }

        if (node.Source != null)
        {
            using (context.BuildLinePragma(node.Source.Value))
            {
                var offset = DesignTimeDirectivePass.DesignTimeVariable.Length + " = ".Length;
                context.CodeWriter.WritePadding(offset, node.Source, context);
                context.CodeWriter.WriteStartAssignment(DesignTimeDirectivePass.DesignTimeVariable);

                foreach (var child in node.Children)
                {
                    if (child is CSharpIntermediateToken token)
                    {
                        context.AddSourceMappingFor(token);
                        context.CodeWriter.Write(token.Content);
                    }
                    else
                    {
                        // There may be something else inside the expression like a Template or another extension node.
                        context.RenderNode(child);
                    }
                }

                context.CodeWriter.WriteLine(";");
            }
        }
        else
        {
            context.CodeWriter.WriteStartAssignment(DesignTimeDirectivePass.DesignTimeVariable);

            foreach (var child in node.Children)
            {
                if (child is CSharpIntermediateToken token)
                {
                    context.CodeWriter.Write(token.Content);
                }
                else
                {
                    // There may be something else inside the expression like a Template or another extension node.
                    context.RenderNode(child);
                }
            }

            context.CodeWriter.WriteLine(";");
        }
    }

    public override void WriteCSharpCode(CodeRenderingContext context, CSharpCodeIntermediateNode node)
    {
        var writer = context.CodeWriter;

        if (node.Source is SourceSpan nodeSource)
        {
            using (context.BuildLinePragma(nodeSource))
            {
                writer.WritePadding(0, nodeSource, context);
                RenderCSharpCode(context, node);
            }
        }
        else
        {
            RenderCSharpCode(context, node);
            writer.WriteLine();
        }
    }

    public override void WriteHtmlAttribute(CodeRenderingContext context, HtmlAttributeIntermediateNode node)
    {
        context.RenderChildren(node);
    }

    public override void WriteHtmlAttributeValue(CodeRenderingContext context, HtmlAttributeValueIntermediateNode node)
    {
        context.RenderChildren(node);
    }

    public override void WriteCSharpExpressionAttributeValue(CodeRenderingContext context, CSharpExpressionAttributeValueIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (node.Children.Count == 0)
        {
            return;
        }

        var firstChild = node.Children[0];
        if (firstChild.Source != null)
        {
            using (context.BuildLinePragma(firstChild.Source.Value))
            {
                var offset = DesignTimeDirectivePass.DesignTimeVariable.Length + " = ".Length;
                context.CodeWriter.WritePadding(offset, firstChild.Source, context);
                context.CodeWriter.WriteStartAssignment(DesignTimeDirectivePass.DesignTimeVariable);

                foreach (var child in node.Children)
                {
                    if (child is CSharpIntermediateToken token)
                    {
                        context.AddSourceMappingFor(token);
                        context.CodeWriter.Write(token.Content);
                    }
                    else
                    {
                        // There may be something else inside the expression like a Template or another extension node.
                        context.RenderNode(child);
                    }
                }

                context.CodeWriter.WriteLine(";");
            }
        }
        else
        {
            context.CodeWriter.WriteStartAssignment(DesignTimeDirectivePass.DesignTimeVariable);
            for (var i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is CSharpIntermediateToken token)
                {
                    if (token.Source != null)
                    {
                        context.AddSourceMappingFor(token);
                    }

                    context.CodeWriter.Write(token.Content);
                }
                else
                {
                    // There may be something else inside the expression like a Template or another extension node.
                    context.RenderNode(node.Children[i]);
                }
            }
            context.CodeWriter.WriteLine(";");
        }
    }

    public override void WriteCSharpCodeAttributeValue(CodeRenderingContext context, CSharpCodeAttributeValueIntermediateNode node)
    {
        var writer = context.CodeWriter;

        foreach (var child in node.Children)
        {
            if (child is CSharpIntermediateToken token)
            {
                var isWhiteSpace = token.Content.IsNullOrWhiteSpace();

                if (token.Source is not SourceSpan tokenSource)
                {
                    // Just write non-whitespace tokens when there isn't a source mapping.
                    if (!isWhiteSpace)
                    {
                        Debug.Fail("Why do we have non-whitespace tokens without source mappings?");
                        writer.WriteLine(token.Content);
                    }

                    continue;
                }

                if (!isWhiteSpace)
                {
                    // Only use a line pragma for non-whitespace content.
                    using (context.BuildLinePragma(tokenSource))
                    {
                        writer.WritePadding(0, tokenSource, context);

                        context.AddSourceMappingFor(tokenSource);
                        writer.Write(token.Content);
                    }
                }
                else
                {
                    writer.WritePadding(0, tokenSource, context);

                    context.AddSourceMappingFor(tokenSource);
                    writer.WriteLine(token.Content);
                }
            }
            else
            {
                // There may be something else inside the statement like an extension node.
                context.RenderNode(child);
            }
        }
    }

    public override void WriteHtmlContent(CodeRenderingContext context, HtmlContentIntermediateNode node)
    {
        // Do nothing
    }

    public override void BeginWriterScope(CodeRenderingContext context, string writer)
    {
        // Do nothing
    }

    public override void EndWriterScope(CodeRenderingContext context)
    {
        // Do nothing
    }
}
