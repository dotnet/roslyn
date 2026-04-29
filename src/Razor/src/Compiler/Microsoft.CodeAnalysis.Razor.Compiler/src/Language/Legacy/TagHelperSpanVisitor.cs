// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal sealed class TagHelperSpanVisitor : SyntaxWalker
{
    private readonly RazorSourceDocument _source;
    private readonly ImmutableArray<TagHelperSpanInternal>.Builder _spans;

    private TagHelperSpanVisitor(RazorSourceDocument source, ImmutableArray<TagHelperSpanInternal>.Builder spans)
    {
        _source = source;
        _spans = spans;
    }

    public static ImmutableArray<TagHelperSpanInternal> VisitRoot(RazorSyntaxTree syntaxTree)
    {
        using var _ = ArrayBuilderPool<TagHelperSpanInternal>.GetPooledObject(out var builder);

        var visitor = new TagHelperSpanVisitor(syntaxTree.Source, builder);
        visitor.Visit(syntaxTree.Root);

        return builder.ToImmutableAndClear();
    }

    public override void VisitMarkupTagHelperElement(MarkupTagHelperElementSyntax node)
    {
        var span = new TagHelperSpanInternal(node.GetSourceSpan(_source), node.TagHelperInfo.BindingResult);
        _spans.Add(span);

        base.VisitMarkupTagHelperElement(node);
    }
}
