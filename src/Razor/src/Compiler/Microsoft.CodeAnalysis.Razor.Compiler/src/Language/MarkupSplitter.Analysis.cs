// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language;

internal static partial class MarkupSplitter
{
    // A markup transition in expression position (an `@<...>` / `@:` template, which lowers to a
    // RenderFragment value) is replaced by an expression; one in statement position (bare markup
    // enabled by AllowRazorInAllCodeBlocks) is replaced by a statement. Substituting the right form
    // for the markup's span yields text that parses iff the user's original C# was valid.
    private const string ExpressionMarker = MarkerMethodName + "()";
    private const string StatementMarker = MarkerMethodName + "();";

    // The class-body children are wrapped in a throwaway class so they parse as member declarations.
    // Recorded child offsets are relative to the full wrapped text so they line up with the parse tree.
    private const string AnalysisClassHeader = "class __C {\n";
    private const string AnalysisClassFooter = "\n}\n";

    /// <summary>
    /// Renders the collected class-body children into a parse-only C# document, replacing each markup
    /// node with a position-aware marker, and records where each child landed (a <see cref="ChildSpan"/>)
    /// so the parser's member boundaries can be mapped back to the original IR nodes. The document is
    /// never emitted; it exists only to recover member boundaries and detect which members carry markup.
    /// </summary>
    internal static AnalysisDocument BuildAnalysisDocument(ImmutableArray<IntermediateNode> children)
    {
        var builder = new StringBuilder();
        builder.Append(AnalysisClassHeader);

        var spans = ImmutableArray.CreateBuilder<ChildSpan>(children.Length);

        foreach (var child in children)
        {
            var start = builder.Length;

            switch (child)
            {
                case CSharpCodeIntermediateNode csharp:
                    AppendCSharpText(builder, csharp);
                    break;

                case var markup when IsMarkupNode(markup):
                    builder.Append(IsExpressionPositionMarkup(markup) ? ExpressionMarker : StatementMarker);
                    break;

                default:
                    // A synthesized structured declaration (e.g. an injected property). It carries no
                    // markup and is surface, so it contributes no analysis text -- but is still recorded
                    // (as a zero-length span) so routing can place it.
                    break;
            }

            spans.Add(new ChildSpan(start, builder.Length - start, child));
        }

        builder.Append(AnalysisClassFooter);

        return new AnalysisDocument(builder.ToString(), spans.ToImmutable());
    }

    /// <summary>
    /// Expression-position markup is exactly a <see cref="TemplateIntermediateNode"/> (from <c>@&lt;...&gt;</c>
    /// / <c>@:</c>); every other class-body markup node sits in statement position. Keying on this single
    /// node kind -- rather than an enumerated list of the statement-position kinds -- keeps the rule
    /// correct as new markup node kinds are introduced.
    /// </summary>
    internal static bool IsExpressionPositionMarkup(IntermediateNode node)
        => node is TemplateIntermediateNode;

    private static void AppendCSharpText(StringBuilder builder, CSharpCodeIntermediateNode node)
    {
        foreach (var child in node.Children)
        {
            if (child is IntermediateToken token)
            {
                builder.Append(token.Content);
            }
        }
    }
}

/// <summary>
/// Where one class-body child landed in the throwaway analysis text, so member boundaries the parser
/// reports (in analysis-document coordinates) can be mapped back to the original IR node regardless of
/// the length difference between a markup node and its marker. A child's role -- raw C#, markup, or a
/// zero-length surface declaration -- is read from <see cref="Node"/> directly rather than stored.
/// </summary>
internal readonly struct ChildSpan
{
    public ChildSpan(int start, int length, IntermediateNode node)
    {
        Start = start;
        Length = length;
        Node = node;
    }

    public int Start { get; }

    public int Length { get; }

    public int End => Start + Length;

    public IntermediateNode Node { get; }
}

/// <summary>The throwaway analysis text plus the placements mapping its spans back to IR nodes.</summary>
internal sealed class AnalysisDocument
{
    public AnalysisDocument(string text, ImmutableArray<ChildSpan> children)
    {
        Text = text;
        Children = children;
    }

    public string Text { get; }

    public ImmutableArray<ChildSpan> Children { get; }
}
