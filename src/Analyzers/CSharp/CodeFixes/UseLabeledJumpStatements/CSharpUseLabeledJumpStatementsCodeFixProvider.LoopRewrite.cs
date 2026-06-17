// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseLabeledJumpStatements;

internal sealed partial class CSharpUseLabeledJumpStatementsCodeFixProvider
{
    /// <summary>
    /// Synthesizes a label for <paramref name="loop"/> (<c>loop_i</c>/<c>loop_x</c> from a for/foreach variable, else
    /// <c>outer</c>), uniquified against the labels in scope at the loop.
    /// </summary>
    private static string SynthesizeLabelName(SemanticModel semanticModel, StatementSyntax loop)
    {
        var baseName = loop switch
        {
            ForStatementSyntax { Declaration.Variables: [var variable, ..] } => "loop_" + variable.Identifier.ValueText,
            ForEachStatementSyntax forEachStatement => "loop_" + forEachStatement.Identifier.ValueText,
            _ => "outer",
        };

        using var _ = PooledHashSet<string>.GetInstance(out var inScope);
        foreach (var label in semanticModel.LookupLabels(loop.SpanStart))
            inScope.Add(label.Name);

        return NameGenerator.GenerateUniqueName(baseName, name => !inScope.Contains(name));
    }

    /// <summary>
    /// Everything that must happen to a single loop/switch so it is relabeled exactly once.
    /// </summary>
    private sealed class LoopRewrite
    {
        /// <summary>Jumps (gotos / inner flag breaks) inside the loop to rewrite, and whether each becomes a break.</summary>
        public required ImmutableArray<(SyntaxNode Jump, bool IsBreak)> Jumps { get; init; }

        /// <summary>Dead code inside the loop to delete (flag assignments/guards, the trailing continue label).</summary>
        public required ImmutableArray<SyntaxNode> InnerRemovals { get; init; }

        /// <summary>Labels that sat after the loop (removed if an empty pad, otherwise just un-labeled).</summary>
        public required ImmutableArray<LabeledStatementSyntax> OuterLabels { get; init; }

        /// <summary>Dead code before the loop to delete (flag declarations).</summary>
        public required ImmutableArray<SyntaxNode> OuterRemovals { get; init; }

        /// <summary>Existing labels whose name may be reused for the loop.</summary>
        public required ImmutableArray<LabeledStatementSyntax> ReuseLabels { get; init; }

        /// <summary>
        /// Reuses the lexically-first existing label's name, or synthesizes one when the loop had no labels (flag case).
        /// </summary>
        public string GetLabelName(SemanticModel semanticModel, StatementSyntax loop)
            => ReuseLabels.OrderBy(l => l.SpanStart).FirstOrDefault()?.Identifier.Text
                ?? SynthesizeLabelName(semanticModel, loop);
    }
}
