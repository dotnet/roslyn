// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// The outcome of analyzing a component's primary class body for the decl/impl markup split. One of
/// three cases:
/// <list type="bullet">
/// <item><see cref="NoSplit"/> -- no class-body markup, so the caller keeps the single-file behavior.</item>
/// <item><see cref="SplitPlan"/> -- the class body mixes markup and C# and can be split safely into a
/// markup-free decl half and a markup-bearing impl half; describes how the pieces route.</item>
/// <item><see cref="SplitFallback"/> -- the class body has markup but cannot be split safely (a markup
/// property, an unsupported node, a directive, or an unrecoverable parse), so the caller retains the
/// original class-body layout and can select its fallback pipeline.</item>
/// </list>
/// </summary>
/// <remarks>
/// This is a closed hierarchy, produced once per primary class and shared by both lowering phases. The
/// decision is a pure function of the class body's IR and the parse options, and it never branches on
/// the language version, so all callers reach the same decision for the same document. Split plans are
/// consumed by the lowering phases; an explicit fallback result lets pipeline callers preserve unsplit
/// processing for shapes that cannot be routed safely.
/// </remarks>
internal abstract class SplitDecision
{
    private protected SplitDecision()
    {
    }

    /// <summary>
    /// The class body has no markup (or nothing that needs splitting); the caller keeps the single-file
    /// behavior. Shared singleton.
    /// </summary>
    public static SplitDecision NoSplit { get; } = new NoSplitDecision();

    /// <summary>
    /// The class body has markup but cannot be split safely; the caller retains the original class-body
    /// layout and can select its fallback pipeline.
    /// </summary>
    public static SplitFallback Fallback(FallbackReason reason) => new(reason);

    /// <summary>True when this decision requires the caller to build separate decl and impl halves.</summary>
    public bool RequiresSplit => this is SplitPlan;

    /// <summary>
    /// True when the file has markup but cannot be split, so the caller must retain the original
    /// class-body layout instead of routing members between declaration and implementation output.
    /// </summary>
    public bool IsFallback => this is SplitFallback;

    private sealed class NoSplitDecision : SplitDecision
    {
    }

    /// <summary>
    /// The class body has markup that cannot be split safely, so its original layout must be retained.
    /// This preserves correctness while allowing the surrounding pipeline to choose a fallback path.
    /// </summary>
    public sealed class SplitFallback : SplitDecision
    {
        public SplitFallback(FallbackReason reason)
        {
            Reason = reason;
        }

        /// <summary>Why the file falls back instead of splitting (for diagnostics/telemetry and tests).</summary>
        public FallbackReason Reason { get; }
    }

    /// <summary>
    /// Describes how each class-body member routes between the decl and impl halves. Produced only when
    /// the class body mixes markup and C# and the file can be split safely.
    /// </summary>
    public sealed class SplitPlan : SplitDecision
    {
        public SplitPlan(ImmutableArray<RoutedMember> members)
        {
            Members = members.NullToEmpty();
        }

        /// <summary>The routed class-body members in original order; each drives what its half emits.</summary>
        public ImmutableArray<RoutedMember> Members { get; }
    }
}

/// <summary>
/// A user-authored class-body member after routing, already resolved into the IR pieces each half emits:
/// <see cref="DeclPieces"/> for the decl half and <see cref="ImplPieces"/> for the impl half. Original
/// nodes are shared by reference (keeping their source mappings). A member is either markup-free (all its
/// pieces stay in decl) or a markup-bearing method (all its pieces lift to impl); a markup property
/// never reaches routing because it produces a fallback decision. The lowering phases simply append the
/// pieces for their half.
/// </summary>
internal readonly struct RoutedMember
{
    public RoutedMember(
        ImmutableArray<IntermediateNode> declPieces,
        ImmutableArray<IntermediateNode> implPieces)
    {
        DeclPieces = declPieces.NullToEmpty();
        ImplPieces = implPieces.NullToEmpty();
    }

    /// <summary>The pieces this member contributes to the decl half, in order.</summary>
    public ImmutableArray<IntermediateNode> DeclPieces { get; }

    /// <summary>The pieces this member contributes to the impl half, in order.</summary>
    public ImmutableArray<IntermediateNode> ImplPieces { get; }
}

/// <summary>
/// Why a markup-bearing class body retains its original layout instead of being routed between
/// declaration and implementation output.
/// </summary>
internal enum FallbackReason
{
    /// <summary>
    /// A property/indexer carries markup. A property is tag-helper descriptor surface (a
    /// <c>[Parameter]</c> shapes the component's attributes), so it must stay in the decl half -- but
    /// markup cannot live in the markup-free decl half. Rather than reshape the property, the splitter
    /// reports fallback so the property remains in place.
    /// </summary>
    MarkupProperty,

    /// <summary>
    /// A non-method, non-property member carries markup -- a field/event (whose initializer runs in
    /// declaration order, which splitting across partials would perturb), a nested type, a
    /// constructor/operator, or an incomplete member. It cannot be safely lifted, so the splitter reports
    /// fallback.
    /// </summary>
    UnsupportedMarkupMember,

    /// <summary>
    /// The analysis parse is unrecoverable (brace mismatch, or a markup marker isn't contained by any
    /// member), so member boundaries can't be trusted. Not triggered by ordinary transient typos, which
    /// still recover member boundaries.
    /// </summary>
    UnrecoverableParse,

    /// <summary>
    /// The class body contains a node the splitter can't route -- neither raw C# nor a recognized markup
    /// node -- such as an <c>@inject</c> or another structured/extension member. Retaining the original
    /// layout avoids moving descriptor surface into the impl half.
    /// </summary>
    UnsupportedClassBodyNode,

    /// <summary>
    /// The class body contains a preprocessor directive (<c>#if</c>/<c>#endif</c>, <c>#region</c>,
    /// <c>#pragma</c>, <c>#nullable</c>). Splitting could route a member out of the directive's scope and
    /// orphan it in the other half; retaining the original layout keeps directives balanced.
    /// </summary>
    ClassBodyHasDirectives,
}
