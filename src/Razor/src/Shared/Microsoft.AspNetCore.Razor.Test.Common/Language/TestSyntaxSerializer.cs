// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed class TestSyntaxSerializer : SyntaxSerializer
{
    private readonly bool _allowSpanEditHandlers;

    private TestSyntaxSerializer(StringBuilder builder, bool allowSpanEditHandlers)
        : base(builder)
    {
        _allowSpanEditHandlers = allowSpanEditHandlers;
    }

    public static string Serialize(SyntaxNode node, bool allowSpanEditHandlers = false)
    {
        using var _ = StringBuilderPool.GetPooledObject(out var builder);
        var serializer = new TestSyntaxSerializer(builder, allowSpanEditHandlers);
        serializer.Visit(node);

        return builder.ToString();
    }

    public static string Serialize(SyntaxToken token, bool allowSpanEditHandlers = false)
    {
        using var _ = StringBuilderPool.GetPooledObject(out var builder);
        var serializer = new TestSyntaxSerializer(builder, allowSpanEditHandlers);
        serializer.VisitToken(token);

        return builder.ToString();
    }

    protected override void WriteSpan(TextSpan span)
    {
        WriteValue($"[{span.Start}..{span.End})::{span.End - span.Start}");
    }

    protected override void WriteValue(string value)
    {
        Builder.Append(value.Replace("\r\n", "LF").Replace("\n", "LF"));
    }

    protected override void WriteSpanEditHandlers(SyntaxNode node)
    {
        if (_allowSpanEditHandlers)
        {
            base.WriteSpanEditHandlers(node);
            return;
        }

        // If we don't allow SpanEditHandlers, assert that there aren't any.
        Assert.Null(node.GetEditHandler());
    }
}
