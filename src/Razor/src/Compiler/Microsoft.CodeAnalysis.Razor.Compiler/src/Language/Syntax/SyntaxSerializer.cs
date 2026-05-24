// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal abstract partial class SyntaxSerializer(StringBuilder builder) : SyntaxWalker
{
    protected const int IndentSize = 4;
    protected const string Separator = " - ";

    protected readonly StringBuilder Builder = builder;
    private bool _visitedRoot;

    private int _depth;

    public sealed override void Visit(SyntaxNode? node)
    {
        if (node == null)
        {
            return;
        }

        WriteNode(node);
        Builder.AppendLine();

        IncreaseIndent();
        base.Visit(node);
        DecreaseIndent();
    }

    public sealed override void VisitToken(SyntaxToken token)
    {
        WriteToken(token);
        Builder.AppendLine();
    }

    private void WriteNode(SyntaxNode node)
    {
        WriteIndent();
        WriteValue(node.Kind);
        WriteSeparator();
        WriteSpan(node.Span);

        switch (node)
        {
            case RazorDirectiveSyntax razorDirective:
                WriteRazorDirective(razorDirective);
                break;

            case MarkupTagHelperElementSyntax tagHelperElement:
                WriteTagHelperElement(tagHelperElement);
                break;

            case MarkupTagHelperAttributeSyntax tagHelperAttribute:
                WriteTagHelperAttributeInfo(tagHelperAttribute.TagHelperAttributeInfo);
                break;

            case MarkupMinimizedTagHelperAttributeSyntax minimizedTagHelperAttribute:
                WriteTagHelperAttributeInfo(minimizedTagHelperAttribute.TagHelperAttributeInfo);
                break;

            case MarkupStartTagSyntax startTag:
                if (startTag.IsMarkupTransition)
                {
                    WriteSeparator();
                    WriteValue("MarkupTransition");
                }

                break;

            case MarkupEndTagSyntax endTag:
                if (endTag.IsMarkupTransition)
                {
                    WriteSeparator();
                    WriteValue("MarkupTransition");
                }

                break;
        }

        if (ShouldDisplayNodeContent(node))
        {
            WriteSeparator();
            WriteValue($"[{node.GetContent()}]");
        }

        WriteChunkGenerator(node);

        WriteSpanEditHandlers(node);

        if (!_visitedRoot)
        {
            WriteSeparator();
            WriteValue($"[{node}]");
            _visitedRoot = true;
        }
    }

    protected virtual void WriteSpan(TextSpan span)
    {
        WriteValue($"[{span.Start}..{span.End}){Separator}Width: {span.End - span.Start}");
    }

    private void WriteRazorDirective(RazorDirectiveSyntax node)
    {
        if (node.DirectiveDescriptor is not { } descriptor)
        {
            return;
        }

        WriteSeparator();
        WriteValue($"Directive:{{{descriptor.Directive};{descriptor.Kind};{descriptor.Usage}}}");

        if (node.GetDiagnostics() is { Length: > 0} diagnostics)
        {
            WriteValue($" [{GetDiagnosticsText(diagnostics)}]");
        }
    }

    private void WriteTagHelperElement(MarkupTagHelperElementSyntax node)
    {
        var tagHelperInfo = node.TagHelperInfo.AssumeNotNull();

        // Write tag name
        WriteSeparator();
        WriteValue($"{tagHelperInfo.TagName}[{tagHelperInfo.TagMode}]");

        // Write descriptors
        foreach (var tagHelper in tagHelperInfo.BindingResult.TagHelpers)
        {
            WriteSeparator();

            // Get the type name without the namespace.
            var typeName = tagHelper.Name[(tagHelper.Name.LastIndexOf('.') + 1)..];
            WriteValue(typeName);
        }
    }

    private void WriteTagHelperAttributeInfo(TagHelperAttributeInfo info)
    {
        // Write attributes
        WriteValue($"{Separator}{info.Name}{Separator}{info.AttributeStructure}{Separator}{(info.Bound ? "Bound" : "Unbound")}");
    }

    private void WriteToken(SyntaxToken token)
    {
        WriteIndent();

        var content = token.IsMissing ? "<Missing>" : token.Content;
        var diagnostics = token.GetDiagnostics().ToArray();
        var diagnosticsText = GetDiagnosticsText(diagnostics);

        WriteValue($"{token.Kind};[{content}];{diagnosticsText}");
    }

    private static string GetDiagnosticsText(RazorDiagnostic[] diagnostics)
    {
        return diagnostics.Length > 0
            ? string.Join(", ", diagnostics.Select(diagnostic => $"{diagnostic.Id}{diagnostic.Span}"))
            : string.Empty;
    }

    private void WriteChunkGenerator(SyntaxNode node)
    {
        if (node.GetChunkGenerator() is { } generator)
        {
            WriteSeparator();
            WriteValue($"Gen<{generator}>");
        }
    }

    protected virtual void WriteSpanEditHandlers(SyntaxNode node)
    {
        if (node.GetEditHandler() is SpanEditHandler handler)
        {
            WriteSeparator();
            WriteValue(handler);
        }
    }

    protected void IncreaseIndent()
    {
        _depth++;
    }

    protected void DecreaseIndent()
    {
        Assumed.True(--_depth >= 0, "Depth can't be less than 0.");
    }

    protected void WriteIndent()
    {
        WriteValue(new string(' ', _depth * IndentSize));
    }

    protected void WriteSeparator()
    {
        WriteValue(Separator);
    }

    protected virtual void WriteValue(string value)
    {
        Builder.Append(value.Replace(Environment.NewLine, "LF"));
    }

    protected virtual void WriteValue(object? value)
    {
        switch (value)
        {
            case string s:
                WriteValue(s);
                break;

            default:
                Builder.Append(value);
                break;
        }
    }

    private static bool ShouldDisplayNodeContent(SyntaxNode node)
    {
        return node.Kind is
            SyntaxKind.MarkupTextLiteral or
            SyntaxKind.MarkupEphemeralTextLiteral or
            SyntaxKind.MarkupStartTag or
            SyntaxKind.MarkupEndTag or
            SyntaxKind.MarkupTagHelperStartTag or
            SyntaxKind.MarkupTagHelperEndTag or
            SyntaxKind.MarkupAttributeBlock or
            SyntaxKind.MarkupMinimizedAttributeBlock or
            SyntaxKind.MarkupTagHelperAttribute or
            SyntaxKind.MarkupMinimizedTagHelperAttribute or
            SyntaxKind.MarkupLiteralAttributeValue or
            SyntaxKind.MarkupDynamicAttributeValue or
            SyntaxKind.CSharpStatementLiteral or
            SyntaxKind.CSharpExpressionLiteral or
            SyntaxKind.CSharpEphemeralTextLiteral or
            SyntaxKind.UnclassifiedTextLiteral;
    }
}
