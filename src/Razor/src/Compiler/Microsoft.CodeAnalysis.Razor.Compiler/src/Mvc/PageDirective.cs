// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

public class PageDirective
{
    public static readonly DirectiveDescriptor Directive = DirectiveDescriptor.CreateDirective(
        "page",
        DirectiveKind.SingleLine,
        builder =>
        {
            builder.AddOptionalStringToken(RazorExtensionsResources.PageDirective_RouteToken_Name, RazorExtensionsResources.PageDirective_RouteToken_Description);
            builder.Usage = DirectiveUsage.FileScopedSinglyOccurring;
            builder.Description = RazorExtensionsResources.PageDirective_Description;
        });

    private PageDirective(string routeTemplate, IntermediateNode directiveNode, SourceSpan? source)
    {
        RouteTemplate = routeTemplate;
        DirectiveNode = directiveNode;
        Source = source;
    }

    public string RouteTemplate { get; }

    public IntermediateNode DirectiveNode { get; }

    public SourceSpan? Source { get; }

    public static RazorProjectEngineBuilder Register(RazorProjectEngineBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.AddDirective(Directive);
        return builder;
    }

    public static bool TryGetPageDirective(DocumentIntermediateNode documentNode, out PageDirective pageDirective)
    {
        var visitor = new Visitor();
        for (var i = 0; i < documentNode.Children.Count; i++)
        {
            visitor.Visit(documentNode.Children[i]);
        }

        if (visitor.DirectiveTokens == null)
        {
            pageDirective = null;
            return false;
        }

        var tokens = visitor.DirectiveTokens.ToList();
        string routeTemplate = null;
        SourceSpan? sourceSpan = null;
        if (tokens.Count > 0)
        {
            routeTemplate = TrimQuotes(tokens[0].Content);
            sourceSpan = tokens[0].Source;
        }

        pageDirective = new PageDirective(routeTemplate, visitor.DirectiveNode, sourceSpan);
        return true;
    }

    private static string TrimQuotes(string content)
    {
        // Tokens aren't captured if they're malformed. Therefore, this method will
        // always be called with a valid token content.
        Debug.Assert(content.Length >= 2);
        Debug.Assert(content.StartsWith("\"", StringComparison.Ordinal));
        Debug.Assert(content.EndsWith("\"", StringComparison.Ordinal));

        return content.Substring(1, content.Length - 2);
    }

    private class Visitor : IntermediateNodeWalker
    {
        public IntermediateNode DirectiveNode { get; private set; }

        public IEnumerable<DirectiveTokenIntermediateNode> DirectiveTokens { get; private set; }

        public override void VisitDirective(DirectiveIntermediateNode node)
        {
            if (node.Directive == Directive)
            {
                DirectiveNode = node;
                DirectiveTokens = node.Tokens;
            }
        }

        public override void VisitMalformedDirective(MalformedDirectiveIntermediateNode node)
        {
            if (DirectiveTokens == null && node.Directive == Directive)
            {
                DirectiveNode = node;
                DirectiveTokens = node.Tokens;
            }
        }
    }
}
