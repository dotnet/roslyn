// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseRecursivePatterns
{
    using static AnalyzedNode;

    // Simplify input tree by finding and unifying identical nodes.
    // For instance:
    //
    //      `x != null && x.y != null && x.y.z == 0`
    //
    // Becomes:
    //
    //      `x is { y: { z: 0 } }`
    //
    // In doing so we need to make "pairs" out of each test together with its input.
    // This structure is needed for rewriting subpatterns and `is` expressions.
    //
    // Note that unnecessary null checks will be elided in a later pass.
    //
    internal static class AnalyzedNodeReducer
    {
        public static AnalyzedNode Reduce(AnalyzedNode root)
        {
            return VisitNode(root, sense: true);
        }

        private static AnalyzedNode VisitNode(AnalyzedNode node, bool sense)
        {
            return node switch
            {
                Sequence seq => VisitSequence(seq, sense),
                Test test => VisitTest(test, sense),
                // Using the `sense` parameter, we capture a negated pattern context.
                // Note that the operand here could be a standalone "evaluation" node.
                // This is later used to produce `true` and `false` constant patterns.
                // For instance:
                //
                //      `x.y && !x.z`
                //
                // Becomes:
                //
                //      `x is { y: true, z: false }`
                //
                Not not => VisitNode(not.Operand, !sense),
                var n => n
            };
        }

        private static AnalyzedNode VisitSequence(Sequence sequence, bool sense)
        {
            var nodes = sequence.Nodes;

            var tests = ArrayBuilder<AnalyzedNode>.GetInstance(nodes.Length);
            foreach (var node in nodes)
                tests.Add(VisitNode(node, sense));

            return Combine(sequence, tests);
        }

        /// <summary>
        /// NOTE: this will free the builder after we're done with it.
        /// </summary>
        private static AnalyzedNode Combine(Sequence sequence, ArrayBuilder<AnalyzedNode> tests)
        {
            Debug.Assert(tests.Count > 0);

            if (tests.Count == 1)
            {
                var result = tests[0];
                tests.Free();
                return result;
            }

            var map = PooledDictionary<Evaluation, ArrayBuilder<AnalyzedNode>>.GetInstance();
            var positiveTests = ArrayBuilder<AnalyzedNode>.GetInstance();
            var negativeTests = ArrayBuilder<AnalyzedNode>.GetInstance();
            foreach (var item in tests)
            {
                if (item is Pair pair)
                {
                    // Collect common inputs in a multi-dictionary to identify identical nodes.
                    // For instance, the two constant tests here:
                    //
                    //      `x.p == 25 && x.q == 52`
                    //
                    // Will be added with `x` as the key. This will result in the following tree:
                    //
                    //      `x is { p: 25, q: 52 }`
                    //
                    // We will continue to do so until all tests are combined in a sequence node.
                    //
                    map.MultiAdd(pair.Input, pair.Pattern);
                }
                else if (item is Not { Operand: Constant op })
                {
                    // Record negative constant tests separately to apply demorgan's,
                    // wherever they appeared in the source.
                    negativeTests.Add(op);
                }
                else
                {
                    positiveTests.Add(item);
                }
            }

            // This method uses the pool in each recursion. We free
            // before calling it again to avoid exhausting the pool.
            tests.Free();

            foreach (var (input, children) in map)
            {
                // Make a pair with each input in the map while recursively combining child nodes,
                // until there is only one child per input which is either a sequence or a single test.
                positiveTests.Add(new Pair(input, Combine(sequence, children)));
            }

            map.Free();

            // Apply demorgan's
            positiveTests.Add(Not.Create(sequence.Negate(negativeTests)));

            return sequence.Update(positiveTests);
        }

        private static AnalyzedNode VisitTest(Test test, bool sense)
        {
            // Using `sense` we bring a parent `not` node
            // closer to the test it is negating. See below.
            var result = sense ? test : Not.Create(test);

            var input = test.Input;
            while (input != null)
            {
                // Make pairs out of each test together with its input.
                //
                // This turns the graph inside-out, moving expressions to the surface
                // so that we can then rewrite it as subpatterns or `is` expressions.
                //
                // For instance in the following node, the target expression is deep inside the graph:
                //
                //      `NOT(x <- y <- 0)`
                //
                // We make another graph using pairs:
                //
                //      `x -> y -> NOT(0)`
                //
                // Now the rewrite pass became trivial:
                //
                //      `x is { y: not 0 }`
                //
                result = new Pair(input, result);
                input = input.Input;
            }

            return result;
        }
    }
}
