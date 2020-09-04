// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.UseRecursivePatterns
{
    using static AnalyzedNode;

    // We replace pattern variables with their original input in order to find and unify identical nodes.
    // For instance:
    //
    //     `e.x.y is var v && v is p`
    //
    // Becomes:
    //
    //     `e.x.y is var v && e.x.y is p`
    //
    // Which will be in turn reduced to:
    //
    //     `e is { x: { y: p } }`
    //
    // TODO: Unused pattern variables should be elided in a later pass.
    // TODO: Can we use an annotation + reducers for this purpose?
    //
    internal static class AnalyzedNodeExpander
    {
        public static AnalyzedNode Expand(AnalyzedNode root)
        {
            // A map from pattern variables to their input.
            // Note the input to a top-level pattern for case clauses and switch arms is set to `null`.
            var variables = PooledDictionary<ISymbol, Evaluation?>.GetInstance();
            var result = VisitNode(root, variables);
            variables.Free();
            return result;
        }

        private static AnalyzedNode VisitNode(AnalyzedNode node, PooledDictionary<ISymbol, Evaluation?> variables)
        {
            return node switch
            {
                Sequence n => VisitSequence(n, variables),
                Not n => Not.Create(VisitNode(n.Operand, variables)),
                Test test => VisitTest(test, variables),
                var n => n
            };
        }

        private static AnalyzedNode VisitSequence(Sequence sequence, PooledDictionary<ISymbol, Evaluation?> variables)
        {
            var nodes = sequence.Nodes;
            var tests = ArrayBuilder<AnalyzedNode>.GetInstance(nodes.Length);

            foreach (var node in nodes)
                tests.Add(VisitNode(node, variables));

            return sequence.Update(tests);
        }

        private static AnalyzedNode VisitTest(Test test, PooledDictionary<ISymbol, Evaluation?> variables)
        {
            var rewrittenInput = VisitInput(test.Input, variables);

            if (test is Variable variable)
                variables.Add(variable.DeclaredSymbol, rewrittenInput);

            return test.WithInput(rewrittenInput);
        }

        private static Evaluation? VisitInput(Evaluation? originalInput, PooledDictionary<ISymbol, Evaluation?> variables)
        {
            if (originalInput is null)
                return null;

            return originalInput.WithInput(VisitInput(originalInput.Input, variables)) switch
            {
                // We might find a field in the map since pattern variables are lowered to fields in scripting.
                MemberEvaluation member when member.Symbol.Kind == SymbolKind.Field &&
                                             variables.TryGetValue(member.Symbol, out var input) => input,
                Variable variable when variables.TryGetValue(variable.DeclaredSymbol, out var input) => input,
                var input => input
            };
        }
    }
}
