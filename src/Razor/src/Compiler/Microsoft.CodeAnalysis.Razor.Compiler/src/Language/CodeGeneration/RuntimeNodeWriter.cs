// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

public class RuntimeNodeWriter : IntermediateNodeWriter
{
    public static readonly RuntimeNodeWriter Instance = new RuntimeNodeWriter();

    public virtual string WriteCSharpExpressionMethod => "Write";

    public virtual string WriteHtmlContentMethod => "WriteLiteral";

    public virtual string BeginWriteAttributeMethod => "BeginWriteAttribute";

    public virtual string EndWriteAttributeMethod => "EndWriteAttribute";

    public virtual string WriteAttributeValueMethod => "WriteAttributeValue";

    public virtual string PushWriterMethod => "PushWriter";

    public virtual string PopWriterMethod => "PopWriter";

    public const string TemplateTypeName = "Microsoft.AspNetCore.Mvc.Razor.HelperResult";

    protected RuntimeNodeWriter()
    {
    }

    public override void WriteUsingDirective(CodeRenderingContext context, UsingDirectiveIntermediateNode node)
    {
        if (node.Source is { FilePath: not null } sourceSpan)
        {
            using (context.BuildEnhancedLinePragma(sourceSpan, suppressLineDefaultAndHidden: true))
            {
                context.CodeWriter.WriteUsing(node.Content, endLine: node.HasExplicitSemicolon);
            }
            if (!node.HasExplicitSemicolon)
            {
                context.CodeWriter.WriteLine(";");
            }
            if (node.AppendLineDefaultAndHidden)
            {
                context.CodeWriter.WriteLine("#line default");
                context.CodeWriter.WriteLine("#line hidden");
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

        // Offset past the "Write(" prefix so the #line directive maps to the expression itself while
        // still wrapping the entire method invocation. Without this wrapping, the C# compiler cannot
        // emit a usable (non-hidden) sequence point for exceptions thrown from the expression, which
        // causes stack traces to report the last non-hidden location (typically an unrelated @{ } block)
        // instead of the actual expression line. See ComponentRuntimeNodeWriter.WriteCSharpExpression
        // for the analogous pattern used by Razor components.
        var characterOffset = WriteCSharpExpressionMethod.Length
            + 1; // for '('

        // Sequence points can only be emitted when the eval stack is empty. Map just the first C# child
        // by placing the pragma before the method invocation and offsetting it past "Write(". This mirrors
        // the approach in ComponentRuntimeNodeWriter. It is not a perfect mapping, but generally works:
        // - Common case: there is only a single C# node, so it maps correctly.
        // - Error cases: there are no C# children, so no pragma is emitted.
        var firstCSharpChild = node.Children.OfType<CSharpIntermediateToken>().FirstOrDefault();
        using (context.BuildEnhancedLinePragma(firstCSharpChild?.Source, characterOffset))
        {
            context.CodeWriter.WriteStartMethodInvocation(WriteCSharpExpressionMethod);

            if (firstCSharpChild is not null)
            {
                context.CodeWriter.Write(firstCSharpChild.Content);
            }
        }

        // Render the remaining children. We still emit #line pragmas for the remaining C# tokens but
        // these won't actually generate any sequence points for debugging.
        foreach (var child in node.Children)
        {
            if (child == firstCSharpChild)
            {
                continue;
            }

            if (child is CSharpIntermediateToken csharpToken)
            {
                using (context.BuildEnhancedLinePragma(csharpToken.Source))
                {
                    context.CodeWriter.Write(csharpToken.Content);
                }
            }
            else
            {
                // There may be something else inside the expression like an extension node.
                context.RenderNode(child);
            }
        }

        context.CodeWriter.WriteEndMethodInvocation();
    }

    public override void WriteCSharpCode(CodeRenderingContext context, CSharpCodeIntermediateNode node)
    {
        var isWhitespaceStatement = true;
        for (var i = 0; i < node.Children.Count; i++)
        {
            var token = node.Children[i] as IntermediateToken;
            if (token == null || !string.IsNullOrWhiteSpace(token.Content))
            {
                isWhitespaceStatement = false;
                break;
            }
        }

        if (isWhitespaceStatement)
        {
            return;
        }

        WriteCSharpChildren(node.Children, context);
        context.CodeWriter.WriteLine();
    }

    private static void WriteCSharpChildren(IntermediateNodeCollection children, CodeRenderingContext context)
    {
        for (var i = 0; i < children.Count; i++)
        {
            if (children[i] is CSharpIntermediateToken token)
            {
                using (context.BuildEnhancedLinePragma(token.Source))
                {
                    context.CodeWriter.Write(token.Content);
                }
            }
            else
            {
                // There may be something else inside the statement like an extension node.
                context.RenderNode(children[i]);
            }
        }
    }

    public override void WriteHtmlAttribute(CodeRenderingContext context, HtmlAttributeIntermediateNode node)
    {
        var valuePieceCount = node
            .Children
            .Count(child =>
                child is HtmlAttributeValueIntermediateNode ||
                child is CSharpExpressionAttributeValueIntermediateNode ||
                child is CSharpCodeAttributeValueIntermediateNode ||
                child is ExtensionIntermediateNode);

        var prefixLocation = node.Source.Value.AbsoluteIndex;
        var suffixLocation = node.Source.Value.AbsoluteIndex + node.Source.Value.Length - node.Suffix.Length;
        context.CodeWriter
            .WriteStartMethodInvocation(BeginWriteAttributeMethod)
            .WriteStringLiteral(node.AttributeName)
            .WriteParameterSeparator()
            .WriteStringLiteral(node.Prefix)
            .WriteParameterSeparator()
            .WriteIntegerLiteral(prefixLocation)
            .WriteParameterSeparator()
            .WriteStringLiteral(node.Suffix)
            .WriteParameterSeparator()
            .WriteIntegerLiteral(suffixLocation)
            .WriteParameterSeparator()
            .WriteIntegerLiteral(valuePieceCount)
            .WriteEndMethodInvocation();

        context.RenderChildren(node);

        context.CodeWriter
            .WriteStartMethodInvocation(EndWriteAttributeMethod)
            .WriteEndMethodInvocation();
    }

    public override void WriteHtmlAttributeValue(CodeRenderingContext context, HtmlAttributeValueIntermediateNode node)
    {
        var prefixLocation = node.Source.Value.AbsoluteIndex;
        var valueLocation = node.Source.Value.AbsoluteIndex + node.Prefix.Length;
        var valueLength = node.Source.Value.Length;
        context.CodeWriter
            .WriteStartMethodInvocation(WriteAttributeValueMethod)
            .WriteStringLiteral(node.Prefix)
            .WriteParameterSeparator()
            .WriteIntegerLiteral(prefixLocation)
            .WriteParameterSeparator();

        // Write content
        for (var i = 0; i < node.Children.Count; i++)
        {
            if (node.Children[i] is HtmlIntermediateToken token)
            {
                context.CodeWriter.WriteStringLiteral(token.Content);
            }
            else
            {
                // There may be something else inside the attribute value like an extension node.
                context.RenderNode(node.Children[i]);
            }
        }

        context.CodeWriter
            .WriteParameterSeparator()
            .WriteIntegerLiteral(valueLocation)
            .WriteParameterSeparator()
            .WriteIntegerLiteral(valueLength)
            .WriteParameterSeparator()
            .WriteBooleanLiteral(true)
            .WriteEndMethodInvocation();
    }

    public override void WriteCSharpExpressionAttributeValue(CodeRenderingContext context, CSharpExpressionAttributeValueIntermediateNode node)
    {
        var prefixLocation = node.Source.Value.AbsoluteIndex;
        context.CodeWriter
            .WriteStartMethodInvocation(WriteAttributeValueMethod)
            .WriteStringLiteral(node.Prefix)
            .WriteParameterSeparator()
            .WriteIntegerLiteral(prefixLocation)
            .WriteParameterSeparator();

        WriteCSharpChildren(node.Children, context);

        var valueLocation = node.Source.Value.AbsoluteIndex + node.Prefix.Length;
        var valueLength = node.Source.Value.Length - node.Prefix.Length;
        context.CodeWriter
            .WriteParameterSeparator()
            .WriteIntegerLiteral(valueLocation)
            .WriteParameterSeparator()
            .WriteIntegerLiteral(valueLength)
            .WriteParameterSeparator()
            .WriteBooleanLiteral(false)
            .WriteEndMethodInvocation();
    }

    public override void WriteCSharpCodeAttributeValue(CodeRenderingContext context, CSharpCodeAttributeValueIntermediateNode node)
    {
        const string ValueWriterName = "__razor_attribute_value_writer";

        var prefixLocation = node.Source.Value.AbsoluteIndex;
        var valueLocation = node.Source.Value.AbsoluteIndex + node.Prefix.Length;
        var valueLength = node.Source.Value.Length - node.Prefix.Length;
        context.CodeWriter
            .WriteStartMethodInvocation(WriteAttributeValueMethod)
            .WriteStringLiteral(node.Prefix)
            .WriteParameterSeparator()
            .WriteIntegerLiteral(prefixLocation)
            .WriteParameterSeparator();

        context.CodeWriter.WriteStartNewObject(TemplateTypeName);

        using (context.CodeWriter.BuildAsyncLambda(ValueWriterName))
        {
            BeginWriterScope(context, ValueWriterName);
            WriteCSharpChildren(node.Children, context);
            EndWriterScope(context);
        }

        context.CodeWriter.WriteEndMethodInvocation(false);

        context.CodeWriter
            .WriteParameterSeparator()
            .WriteIntegerLiteral(valueLocation)
            .WriteParameterSeparator()
            .WriteIntegerLiteral(valueLength)
            .WriteParameterSeparator()
            .WriteBooleanLiteral(false)
            .WriteEndMethodInvocation();
    }

    public override void WriteHtmlContent(CodeRenderingContext context, HtmlContentIntermediateNode node)
    {
        const int MaxStringLiteralLength = 1024;

        using var htmlContentBuilder = new PooledArrayBuilder<ReadOnlyMemory<char>>();

        var length = 0;
        foreach (var child in node.Children)
        {
            if (child is HtmlIntermediateToken token)
            {
                var htmlContent = token.Content.AsMemory();

                htmlContentBuilder.Add(htmlContent);
                length += htmlContent.Length;
            }
        }

        // Can't use a pooled builder here as the memory will be stored in the context.
        var content = new char[length];
        var contentIndex = 0;
        foreach (var htmlContent in htmlContentBuilder)
        {
            htmlContent.Span.CopyTo(content.AsSpan(contentIndex));
            contentIndex += htmlContent.Length;
        }

        WriteHtmlLiteral(context, MaxStringLiteralLength, content.AsMemory());
    }

    // Internal for testing
    internal void WriteHtmlLiteral(CodeRenderingContext context, int maxStringLiteralLength, ReadOnlyMemory<char> literal)
    {
        while (literal.Length > maxStringLiteralLength)
        {
            // String is too large, render the string in pieces to avoid Roslyn OOM exceptions at compile time: https://github.com/aspnet/External/issues/54
            var lastCharBeforeSplit = literal.Span[maxStringLiteralLength - 1];

            // If character at splitting point is a high surrogate, take one less character this iteration
            // as we're attempting to split a surrogate pair. This can happen when something like an
            // emoji sits on the barrier between splits; if we were to split the emoji we'd end up with
            // invalid bytes in our output.
            var renderCharCount = char.IsHighSurrogate(lastCharBeforeSplit) ? maxStringLiteralLength - 1 : maxStringLiteralLength;

            WriteLiteral(literal[..renderCharCount]);

            literal = literal[renderCharCount..];
        }

        WriteLiteral(literal);
        return;

        void WriteLiteral(ReadOnlyMemory<char> content)
        {
            context.CodeWriter
                .WriteStartMethodInvocation(WriteHtmlContentMethod)
                .WriteStringLiteral(content)
                .WriteEndMethodInvocation();
        }
    }

    public override void BeginWriterScope(CodeRenderingContext context, string writer)
    {
        context.CodeWriter.WriteMethodInvocation(PushWriterMethod, writer);
    }

    public override void EndWriterScope(CodeRenderingContext context)
    {
        context.CodeWriter.WriteMethodInvocation(PopWriterMethod);
    }
}
