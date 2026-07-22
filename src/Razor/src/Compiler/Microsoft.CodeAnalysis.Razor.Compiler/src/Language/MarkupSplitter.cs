// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// Decides, for a component's primary class body, which parts of the user's <c>@code</c> belong in
/// the markup-free "decl" half (the tag-helper descriptor surface) and which markup-bearing parts
/// belong in the "impl" half (lowered after tag-helper resolution).
/// </summary>
/// <remarks>
/// <para>
/// The <c>@code</c> contents arrive on the primary <see cref="ClassDeclarationIntermediateNode"/> as a
/// flat sequence of raw C# text (<see cref="CSharpCodeIntermediateNode"/> holding
/// <see cref="CSharpIntermediateToken"/>) interleaved with markup nodes. The vast majority of
/// <c>@code</c> is pure C# with no markup, so a cheap structural gate (<see cref="HasClassBodyMarkup"/>)
/// runs first: with no class-body markup there is nothing to route to the impl half, so it reports
/// <see cref="SplitDecision.NoSplit"/> without parsing anything -- the whole class body stays in the
/// decl half.
/// </para>
/// <para>
/// The split decision is a pure function of the class body's IR content and the parse options; it does
/// not branch on the language version, so every caller reaches the same decision for the same document.
/// The markup-split phase computes it once, produces the decl half, and rewrites the working node into
/// the impl half -- all before tag-helper resolution.
/// </para>
/// </remarks>
internal static partial class MarkupSplitter
{
    /// <summary>
    /// Identifier emitted into the throwaway analysis document to stand in for a markup transition, so
    /// the class body parses as ordinary C# without needing resolved tag helpers. It never appears in
    /// generated output. Markup is detected from the analysis document's per-child placements, never by
    /// matching this name (user code may legitimately contain a call of the same name).
    /// </summary>
    public const string MarkerMethodName = "__RazorMarkupTransition";

    /// <summary>
    /// Computes the split decision for the given primary class body: gates on class-body markup, builds
    /// the analysis document, and classifies it. Pure and uncached.
    /// </summary>
    public static SplitDecision Split(
        ClassDeclarationIntermediateNode primaryClass,
        MethodDeclarationIntermediateNode renderMethod,
        RazorParserOptions parserOptions)
    {
        ArgHelper.ThrowIfNull(primaryClass);
        ArgHelper.ThrowIfNull(renderMethod);
        ArgHelper.ThrowIfNull(parserOptions);

        // Fast path: with no class-body markup there is nothing to move to the impl half.
        if (!HasClassBodyMarkup(primaryClass, renderMethod))
        {
            return SplitDecision.NoSplit;
        }

        var children = CollectClassBodyChildren(primaryClass, renderMethod);
        var analysis = BuildAnalysisDocument(children);
        return ClassifyFromAnalysis(analysis, parserOptions.CSharpParseOptions);
    }

    /// <summary>
    /// The routable plan for the given classified class body, or <see langword="null"/> when the body
    /// keeps its unsplit shape. This is the <em>fallback</em> entry point: the primary decl/impl split
    /// runs early (before tag-helper resolution) over the raw <c>@code</c>, but a component whose raw
    /// shape the early analysis can't partition falls through to the late lowering phases, which route
    /// its markup here over the already-classified tree instead.
    /// </summary>
    public static SplitDecision.SplitPlan? GetRoutablePlan(
        ClassDeclarationIntermediateNode primaryClass,
        MethodDeclarationIntermediateNode renderMethod,
        RazorParserOptions parserOptions)
        => Split(primaryClass, renderMethod, parserOptions) as SplitDecision.SplitPlan;

    /// <summary>
    /// True if any line of the analysis text begins (after leading whitespace) with a preprocessor
    /// directive. A line-anchored scan avoids misfiring on a <c>#</c> inside a string or interpolation;
    /// a rare false positive only costs an unnecessary fallback, never a mis-split.
    /// </summary>
    internal static bool HasPreprocessorDirective(string text)
    {
        var atLineStart = true;

        foreach (var c in text)
        {
            if (c is '\n' or '\r')
            {
                atLineStart = true;
            }
            else if (!atLineStart)
            {
                continue;
            }
            else if (c == '#')
            {
                return true;
            }
            else if (!char.IsWhiteSpace(c))
            {
                atLineStart = false;
            }
        }

        return false;
    }

    /// <summary>
    /// True if the primary class body contains a markup transition (a node that can only be lowered
    /// after tag-helper resolution). Runs in O(children) with no parsing.
    /// </summary>
    public static bool HasClassBodyMarkup(
        ClassDeclarationIntermediateNode primaryClass,
        MethodDeclarationIntermediateNode renderMethod)
    {
        foreach (var child in primaryClass.Children)
        {
            if (ReferenceEquals(child, renderMethod) || child.IsSynthesizedHelper)
            {
                continue;
            }

            if (IsClassBodyMarkup(child))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Classifies a class-body child as markup rather than C#. Defined as the complement of the known
    /// C#/structured-declaration node kinds so that an unrecognized (e.g. newly introduced) markup node
    /// is treated as markup: erring toward running the split machinery is a harmless cost, whereas
    /// missing a markup node would let it leak into the resolution-free decl half.
    /// </summary>
    /// <remarks>
    /// This is the deliberately over-eager <em>gate</em> classifier. It can flag a non-markup extension
    /// node (an <c>@inject</c>) as "markup"; that only causes <see cref="Split"/> to run, which then sees
    /// the node isn't a kind it can route (<see cref="IsSupportedClassBodyNode"/>) and falls back. Routing
    /// itself uses the precise allow-list <see cref="IsMarkupNode"/>, never this predicate.
    /// </remarks>
    internal static bool IsClassBodyMarkup(IntermediateNode node)
        => node is not (CSharpCodeIntermediateNode or
                        FieldDeclarationIntermediateNode or
                        PropertyDeclarationIntermediateNode or
                        MethodDeclarationIntermediateNode);

    /// <summary>
    /// The precise allow-list of markup intermediate node kinds the splitter knows how to route to the
    /// impl half: an expression-position <see cref="TemplateIntermediateNode"/> (from <c>@&lt;...&gt;</c>)
    /// and the statement-position markup nodes. Unlike the fail-safe <see cref="IsClassBodyMarkup"/> gate,
    /// this is positive: a class-body node that is neither raw C# nor one of these kinds -- e.g. an
    /// <c>@inject</c> (<c>ComponentInjectIntermediateNode</c>, itself an
    /// <see cref="ExtensionIntermediateNode"/> just like <see cref="TemplateIntermediateNode"/>) or a
    /// structured member declaration -- is not treated as routable markup.
    /// </summary>
    internal static bool IsMarkupNode(IntermediateNode node)
        => node is TemplateIntermediateNode or
                   MarkupElementIntermediateNode or
                   MarkupBlockIntermediateNode or
                   HtmlContentIntermediateNode;

    /// <summary>
    /// A class-body node the splitter can route: raw C# text (which stays in decl or lifts to impl with
    /// its member) or a recognized markup node (which lifts to impl). Any other kind -- a structured or
    /// extension member such as <c>@inject</c> -- means the file can't be split and must fall back.
    /// </summary>
    internal static bool IsSupportedClassBodyNode(IntermediateNode node)
        => node is CSharpCodeIntermediateNode || IsMarkupNode(node);

    /// <summary>
    /// The ordered user-authored class-body children -- everything that isn't the render method or a
    /// synthesized helper -- in source order. This is the flat sequence of raw C# chunks and markup
    /// transitions the analysis document and routing operate over.
    /// </summary>
    internal static ImmutableArray<IntermediateNode> CollectClassBodyChildren(
        ClassDeclarationIntermediateNode primaryClass,
        MethodDeclarationIntermediateNode renderMethod)
    {
        using var builder = new PooledArrayBuilder<IntermediateNode>();

        foreach (var child in primaryClass.Children)
        {
            if (ReferenceEquals(child, renderMethod) || child.IsSynthesizedHelper)
            {
                continue;
            }

            builder.Add(child);
        }

        return builder.ToImmutableAndClear();
    }
}
