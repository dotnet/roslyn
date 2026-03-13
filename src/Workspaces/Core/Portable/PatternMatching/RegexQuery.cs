// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.PatternMatching;

/// <summary>
/// A boolean query tree compiled from a regex AST, used to pre-filter documents before running
/// the full (expensive) regex match. Each node evaluates against a document's indexed
/// bigrams/trigrams to quickly reject documents that cannot possibly contain a match.
/// <para/>
/// The tree mirrors the boolean structure of the regex: concatenation becomes <see cref="All"/>
/// (AND), alternation becomes <see cref="Any"/> (OR), and opaque constructs (wildcards, character
/// classes) become <see cref="None"/> (passthrough). A pattern like <c>(Read|Write)Line</c> compiles
/// to <c>All(Any(Literal("Read"), Literal("Write")), Literal("Line"))</c>, requiring "Line"'s
/// bigrams to be present and at least one of "Read" or "Write"'s bigrams.
/// <para/>
/// When the entire tree reduces to <see cref="None"/> (e.g. for <c>.*</c> which has no extractable
/// literals), <see cref="HasLiterals"/> is <see langword="false"/> and callers skip pre-filtering —
/// every document becomes a candidate and must be checked with the full regex.
/// </summary>
internal abstract class RegexQuery
{
    private RegexQuery() { }

    /// <summary>
    /// Whether this tree contains any <see cref="Literal"/> nodes whose bigrams/trigrams
    /// can be checked against the document index. When <see langword="false"/>, the query
    /// cannot reject any documents and pre-filtering should be skipped entirely.
    /// </summary>
    public abstract bool HasLiterals { get; }

    /// <summary>
    /// Conjunction: all children must be satisfied. Produced from regex concatenation
    /// (<c>AB</c> = A followed by B).
    /// </summary>
    public sealed class All(ImmutableArray<RegexQuery> children) : RegexQuery
    {
        public readonly ImmutableArray<RegexQuery> Children = children;
        public override bool HasLiterals => Children.Any(static c => c.HasLiterals);
    }

    /// <summary>
    /// Disjunction: at least one child must be satisfied. Produced from regex alternation
    /// (<c>A|B</c>).
    /// </summary>
    public sealed class Any(ImmutableArray<RegexQuery> children) : RegexQuery
    {
        public readonly ImmutableArray<RegexQuery> Children = children;
        public override bool HasLiterals => Children.Any(static c => c.HasLiterals);
    }

    /// <summary>
    /// A literal string that must appear somewhere in the document's symbol names.
    /// At pre-filter time, the literal's lowercased bigrams and trigrams are checked
    /// against the document's indexed bitset and Bloom filter. The text is always
    /// lowercase and at least two characters long, guaranteeing that every literal
    /// contributes at least one bigram to the pre-filter check.
    /// </summary>
    public sealed class Literal : RegexQuery
    {
        public readonly string Text;
        public override bool HasLiterals => true;

        public Literal(string text)
        {
            Debug.Assert(text.Length >= 2);
            Debug.Assert(text == text.ToLowerInvariant());
            Text = text;
        }
    }

    /// <summary>
    /// An opaque node that cannot contribute to pre-filtering (e.g. <c>.</c>, <c>\d</c>,
    /// character classes). Always evaluates to <see langword="true"/> in the pre-filter,
    /// meaning "I can't tell — don't reject on my account."
    /// </summary>
    public sealed class None : RegexQuery
    {
        public static readonly None Instance = new();
        private None() { }
        public override bool HasLiterals => false;
    }

    /// <summary>
    /// Simplifies the query tree by flattening nested <see cref="All"/>/<see cref="Any"/> nodes,
    /// removing <see cref="None"/> where safe, and collapsing single-child wrappers.
    /// <para/>
    /// The key asymmetry: <see cref="None"/> means "anything could match here." In an
    /// <see cref="All"/> (AND), that's vacuously true and can be dropped — the remaining children
    /// still constrain the match. In an <see cref="Any"/> (OR), it poisons the whole disjunction —
    /// if one branch can match anything, the entire <see cref="Any"/> can match anything, so it
    /// collapses to <see cref="None"/>.
    /// <para/>
    /// <b>Post-condition:</b> The returned tree contains <see cref="None"/> only as the top-level
    /// result (meaning the entire regex is opaque). If the result is an <see cref="All"/>,
    /// <see cref="Any"/>, or <see cref="Literal"/>, no <see cref="None"/> nodes exist anywhere
    /// in the subtree. This allows consumers to assume only those three types appear when
    /// traversing a non-None result.
    /// </summary>
    public static RegexQuery Optimize(RegexQuery query)
    {
        return query switch
        {
            All all => OptimizeAll(all),
            Any any => OptimizeAny(any),
            _ => query,
        };

        static RegexQuery OptimizeAll(All all)
        {
            using var _ = Microsoft.CodeAnalysis.PooledObjects.ArrayBuilder<RegexQuery>.GetInstance(out var children);

            foreach (var child in all.Children)
            {
                var optimized = Optimize(child);

                // None in an All is vacuously true — drop it. For example, in the regex `Goo.*Bar`,
                // `.*` compiles to None (matches anything). The All becomes All(Literal("Goo"), None,
                // Literal("Bar")). Since we AND all children, None (= "anything could be here") doesn't
                // restrict the match, so dropping it yields All(Literal("Goo"), Literal("Bar")), which
                // correctly requires both "Goo" and "Bar" bigrams to be present.
                if (optimized is None)
                    continue;

                // Flatten nested All: All(All(a, b), c) -> All(a, b, c).
                if (optimized is All inner)
                    children.AddRange(inner.Children);
                else
                    children.Add(optimized);
            }

            return children.Count switch
            {
                0 => None.Instance,
                1 => children[0],
                _ => new All(children.ToImmutable()),
            };
        }

        static RegexQuery OptimizeAny(Any any)
        {
            using var _ = Microsoft.CodeAnalysis.PooledObjects.ArrayBuilder<RegexQuery>.GetInstance(out var children);

            foreach (var child in any.Children)
            {
                var optimized = Optimize(child);

                // None in an Any means "anything could match" — the whole Any is opaque.
                if (optimized is None)
                    return None.Instance;

                // Flatten nested Any: Any(Any(a, b), c) -> Any(a, b, c).
                if (optimized is Any inner)
                    children.AddRange(inner.Children);
                else
                    children.Add(optimized);
            }

            return children.Count switch
            {
                0 => None.Instance,
                1 => children[0],
                _ => new Any(children.ToImmutable()),
            };
        }
    }
}
