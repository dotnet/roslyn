// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.PatternMatching;

/// <summary>
/// A boolean query tree compiled from a regex AST. Used to pre-filter documents
/// before running the full regex match. Each node evaluates against a document's
/// indexed bigrams/trigrams to quickly reject documents that cannot possibly contain
/// a match.
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
    internal sealed class All(ImmutableArray<RegexQuery> children) : RegexQuery
    {
        public ImmutableArray<RegexQuery> Children { get; } = children;
        public override bool HasLiterals => Children.Any(static c => c.HasLiterals);
        public override string ToString() => $"All({string.Join(", ", Children.Select(c => c.ToString()))})";
    }

    /// <summary>
    /// Disjunction: at least one child must be satisfied. Produced from regex alternation
    /// (<c>A|B</c>).
    /// </summary>
    internal sealed class Any(ImmutableArray<RegexQuery> children) : RegexQuery
    {
        public ImmutableArray<RegexQuery> Children { get; } = children;
        public override bool HasLiterals => Children.Any(static c => c.HasLiterals);
        public override string ToString() => $"Any({string.Join(", ", Children.Select(c => c.ToString()))})";
    }

    /// <summary>
    /// A literal string that must appear somewhere in the document's symbol names.
    /// At pre-filter time, the literal's lowercased bigrams and trigrams are checked
    /// against the document's indexed bitset and Bloom filter.
    /// </summary>
    internal sealed class Literal(string text) : RegexQuery
    {
        public string Text { get; } = text;
        public override bool HasLiterals => true;
        public override string ToString() => $"Literal(\"{Text}\")";
    }

    /// <summary>
    /// An opaque node that cannot contribute to pre-filtering (e.g. <c>.</c>, <c>\d</c>,
    /// character classes). Always evaluates to <see langword="true"/> in the pre-filter,
    /// meaning "I can't tell — don't reject on my account."
    /// </summary>
    internal sealed class None : RegexQuery
    {
        public static readonly None Instance = new();
        private None() { }
        public override bool HasLiterals => false;
        public override string ToString() => "None";
    }

    /// <summary>
    /// Simplifies the query tree by flattening nested <see cref="All"/>/<see cref="Any"/> nodes,
    /// removing <see cref="None"/> where safe, and collapsing single-child wrappers.
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

                // None in an All is vacuously true — drop it.
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
                _ => new All([.. children]),
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
                _ => new Any([.. children]),
            };
        }
    }
}
