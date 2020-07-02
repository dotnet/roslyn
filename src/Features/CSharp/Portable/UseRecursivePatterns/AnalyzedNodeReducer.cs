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

    internal static class AnalyzedNodeReducer
    {
        public static AnalyzedNode Reduce(AnalyzedNode root)
        {
            return VisitNode(root, sense: true);

            static AnalyzedNode VisitNode(AnalyzedNode node, bool sense)
            {
                return node switch
                {
                    Sequence seq => VisitSequence(seq, sense),
                    Not not => VisitNode(not.Operand, !sense),
                    Test test => VisitTest(test, sense),
                    var n => n
                };
            }

            static AnalyzedNode VisitSequence(Sequence sequence, bool sense)
            {
                var nodes = sequence.Nodes;
                var tests = ArrayBuilder<AnalyzedNode>.GetInstance(nodes.Length);
                foreach (var node in nodes)
                    tests.Add(VisitNode(node, sense));
                return Combine(sequence, tests);
            }

            static AnalyzedNode Combine(Sequence sequence, ArrayBuilder<AnalyzedNode> tests)
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
                        map.MultiAdd(pair.Input, pair.Pattern);
                    else if (item is Not { Operand: Constant op })
                        negativeTests.Add(op);
                    else
                        positiveTests.Add(item);
                }
                tests.Free();
                foreach (var (input, patterns) in map)
                    positiveTests.Add(new Pair(input, Combine(sequence, patterns)));
                map.Free();
                positiveTests.Add(Not.Create(sequence.Negate(negativeTests)));
                return sequence.Update(positiveTests);
            }

            static AnalyzedNode VisitTest(Test test, bool sense)
            {
                var input = test.Input;
                var result = sense ? test : Not.Create(test);
                while (input != null)
                {
                    result = new Pair(input, result);
                    input = input.Input;
                }
                return result;
            }
        }
    }
}
