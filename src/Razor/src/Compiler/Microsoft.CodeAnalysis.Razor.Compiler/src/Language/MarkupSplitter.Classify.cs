// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Language;

internal static partial class MarkupSplitter
{
    /// <summary>
    /// Turns a prepared <see cref="AnalysisDocument"/> into a <see cref="SplitDecision"/> in a single
    /// pass: it applies the pre-parse safety gates (unsupported node, preprocessor directive), parses the
    /// throwaway class, validates that every marker and every C# character is covered by a member, then
    /// classifies and routes each member together -- bailing on the first member whose markup can't be
    /// lifted, otherwise emitting the routed pieces. Classification and routing share the one parse and
    /// the one set of member spans, so there is no intermediate classified-member table. The
    /// analysis-building half runs in an earlier phase, so this is the resolution-independent decision the
    /// split-classification phase drives; it never returns <see cref="SplitDecision.NoSplit"/> because a
    /// document with no class-body markup never reaches analysis.
    /// </summary>
    internal static SplitDecision ClassifyFromAnalysis(
        AnalysisDocument analysis,
        CSharpParseOptions parseOptions,
        CancellationToken cancellationToken = default)
    {
        // Every routable node is raw C# or recognized markup. A structured/extension member that reached
        // the class body (e.g. an @inject) can't be placed, so leave the class body unrouted.
        foreach (var child in analysis.Children)
        {
            if (!IsSupportedClassBodyNode(child.Node))
            {
                return SplitDecision.Fallback(FallbackReason.UnsupportedClassBodyNode);
            }
        }

        // A preprocessor directive scopes across members; routing one to the other half would orphan it.
        if (HasPreprocessorDirective(analysis.Text))
        {
            return SplitDecision.Fallback(FallbackReason.ClassBodyHasDirectives);
        }

        // Parse from a SourceText (the string-based ParseText overload is banned in this project). The
        // analysis document is throwaway and never emitted, so its encoding is irrelevant.
        var tree = CSharpSyntaxTree.ParseText(SourceText.From(analysis.Text), parseOptions, cancellationToken: cancellationToken);
        var root = tree.GetCompilationUnitRoot(cancellationToken);

        var markerClass = root.Members.OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (markerClass is null || markerClass.OpenBraceToken.IsMissing || markerClass.CloseBraceToken.IsMissing)
        {
            return SplitDecision.Fallback(FallbackReason.UnrecoverableParse);
        }

        // Member spans, in source order. These and the child spans both live in analysis-document
        // coordinates (a markup node contributes its marker's span), so intersecting them below never has
        // to reconcile the markup/marker length difference.
        var members = markerClass.Members;
        var memberSpans = new TextSpan[members.Count];
        for (var i = 0; i < members.Count; i++)
        {
            memberSpans[i] = members[i].FullSpan;
        }

        // Every markup marker and every non-whitespace C# character must land inside some member. A leak
        // -- brace imbalance let a marker escape a member (or the class), or skipped tokens fell outside
        // every member -- can't be routed without dropping content or leaking markup into decl, so it is
        // unrecoverable. This, not an ordinary transient syntax error (which still recovers member
        // boundaries), is what the catastrophic safety net exists for.
        if (!AllMarkupCovered(analysis.Children, memberSpans) ||
            !AllCSharpContentCovered(analysis, memberSpans))
        {
            return SplitDecision.Fallback(FallbackReason.UnrecoverableParse);
        }

        // Classify each member and, in the same pass, note whether it lifts to impl. A markup-bearing
        // plain method lifts wholesale; markup anywhere else fails the whole file, so bail on the first
        // such member before doing any routing work.
        var liftToImpl = new bool[members.Count];
        for (var i = 0; i < members.Count; i++)
        {
            if (!MemberCoversMarkup(analysis.Children, memberSpans[i]))
            {
                continue;
            }

            switch (members[i])
            {
                // Only a plain method can be lifted wholesale to impl: it has no field-initializer
                // ordering to preserve and isn't descriptor surface, so its absence from decl is invisible.
                case MethodDeclarationSyntax:
                    liftToImpl[i] = true;
                    break;

                // A property/indexer is descriptor surface -- it must stay in decl, where markup can't
                // live -- so markup in one forces fallback.
                case PropertyDeclarationSyntax or IndexerDeclarationSyntax:
                    return SplitDecision.Fallback(FallbackReason.MarkupProperty);

                // Anything else with markup -- a field/event (whose initializer runs in declaration
                // order), a nested type (which may be referenced from decl, or itself contain markup
                // members), a constructor/operator, or an incomplete member -- isn't safe to lift.
                default:
                    return SplitDecision.Fallback(FallbackReason.UnsupportedMarkupMember);
            }
        }

        return new SplitDecision.SplitPlan(RouteMembers(analysis, memberSpans, liftToImpl));
    }

    /// <summary>
    /// Groups the analysis children under their owning parsed members, slicing straddling C# chunks at
    /// member boundaries, to produce one <see cref="RoutedMember"/> per member in original order --
    /// already resolved into the pieces each half emits. Parser member spans and IR nodes both live in
    /// analysis-document offsets (a markup node contributes its marker span), so intersecting them here
    /// means the markup/marker length difference never matters. A member either stays wholly in decl or
    /// (a markup method) lifts wholly to impl, per <paramref name="liftToImpl"/>.
    /// </summary>
    private static ImmutableArray<RoutedMember> RouteMembers(
        AnalysisDocument analysis,
        TextSpan[] memberSpans,
        bool[] liftToImpl)
    {
        // Accumulate each member's pieces (sliced C# chunks and markup nodes) in source order.
        var pieceBuilders = new List<IntermediateNode>[memberSpans.Length];
        for (var i = 0; i < memberSpans.Length; i++)
        {
            pieceBuilders[i] = [];
        }

        foreach (var child in analysis.Children)
        {
            if (child.Node is CSharpCodeIntermediateNode csharp)
            {
                RouteCSharpChild(csharp, child, memberSpans, pieceBuilders);
            }
            else
            {
                // A markup marker or a zero-length synthesized declaration lives entirely inside one
                // member; route the original node there by reference (keeping its source mappings).
                var owner = FindMemberIndex(memberSpans, child.Start);
                if (owner >= 0)
                {
                    pieceBuilders[owner].Add(child.Node);
                }
            }
        }

        var result = ImmutableArray.CreateBuilder<RoutedMember>(memberSpans.Length);
        for (var i = 0; i < memberSpans.Length; i++)
        {
            var pieces = pieceBuilders[i].ToImmutableArray();

            // A markup-free member stays in decl; a markup-bearing method lifts wholesale to impl.
            // (Markup properties produced a fallback decision before this runs.)
            result.Add(liftToImpl[i]
                ? new RoutedMember(declPieces: [], implPieces: pieces)
                : new RoutedMember(declPieces: pieces, implPieces: []));
        }

        return result.ToImmutable();
    }

    // Slices a raw C# chunk at any member boundaries that fall within it and routes each slice to the
    // member that owns its start. A single class-body C# chunk commonly straddles several members (a
    // field immediately followed by a markup-bearing method), so it can't be routed as a unit.
    private static void RouteCSharpChild(
        CSharpCodeIntermediateNode node,
        ChildSpan child,
        TextSpan[] memberSpans,
        List<IntermediateNode>[] pieceBuilders)
    {
        // Member boundaries strictly inside the child become node-local cut offsets. Members are in
        // source order with contiguous, increasing spans, so the cuts come out strictly increasing.
        var cuts = ImmutableArray.CreateBuilder<int>();
        foreach (var span in memberSpans)
        {
            var boundary = span.End;
            if (boundary > child.Start && boundary < child.End)
            {
                cuts.Add(boundary - child.Start);
            }
        }

        var cutOffsets = cuts.ToImmutable();
        var slices = SplitCSharpNode(node, cutOffsets);

        for (var i = 0; i < slices.Length; i++)
        {
            var localStart = i == 0 ? 0 : cutOffsets[i - 1];
            var owner = FindMemberIndex(memberSpans, child.Start + localStart);
            if (owner >= 0)
            {
                pieceBuilders[owner].Add(slices[i]);
            }
        }
    }

    // The index of the member whose analysis-document span contains the offset. Members partition the
    // class body contiguously, so any interior offset has exactly one owner; a boundary offset belongs to
    // the following member (spans are half-open), which is the member that content begins.
    private static int FindMemberIndex(TextSpan[] memberSpans, int offset)
    {
        for (var i = 0; i < memberSpans.Length; i++)
        {
            if (memberSpans[i].Contains(offset))
            {
                return i;
            }
        }

        return -1;
    }

    // True when every non-whitespace character of every C# child falls within some member's span. Members
    // partition the class body contiguously in the common case, so this only fails on real gaps (leading/
    // trailing skipped tokens or brace imbalance), which routing must not silently drop.
    private static bool AllCSharpContentCovered(AnalysisDocument analysis, TextSpan[] memberSpans)
    {
        var text = analysis.Text;

        foreach (var child in analysis.Children)
        {
            if (child.Node is not CSharpCodeIntermediateNode)
            {
                continue;
            }

            for (var index = child.Start; index < child.End; index++)
            {
                if (!char.IsWhiteSpace(text[index]) && !IsCoveredByMember(memberSpans, index))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool IsCoveredByMember(TextSpan[] memberSpans, int index)
    {
        foreach (var span in memberSpans)
        {
            if (span.Contains(index))
            {
                return true;
            }
        }

        return false;
    }

    // True when every markup marker starts within some member's span. A marker outside every member means
    // brace imbalance let it leak out (unrecoverable): routing it would drop it or leak markup into decl.
    private static bool AllMarkupCovered(ImmutableArray<ChildSpan> children, TextSpan[] memberSpans)
    {
        foreach (var child in children)
        {
            if (IsMarkupNode(child.Node) && !IsCoveredByMember(memberSpans, child.Start))
            {
                return false;
            }
        }

        return true;
    }

    // A member carries markup when a markup child's marker starts within the member's span. Detection is
    // by node kind over the child spans, never by matching the marker identifier name -- user code may
    // itself call a method of that name.
    private static bool MemberCoversMarkup(ImmutableArray<ChildSpan> children, TextSpan memberSpan)
    {
        foreach (var child in children)
        {
            if (IsMarkupNode(child.Node) && memberSpan.Contains(child.Start))
            {
                return true;
            }
        }

        return false;
    }
}
