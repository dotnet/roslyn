// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.UseRecursivePatterns
{
    using static AnalyzedNode;

    internal static class AnalyzedNodeNormalizer
    {
        public static AnalyzedNode Normalize(AnalyzedNode root)
        {
            var variables = PooledDictionary<ISymbol, Evaluation?>.GetInstance();
            var result = VisitNode(root);
            variables.Free();
            return result;

            AnalyzedNode VisitNode(AnalyzedNode node)
            {
                var tests = ArrayBuilder<AnalyzedNode>.GetInstance();
                tests.Add(VisitCore(node));
                return AndSequence.Create(tests);

                AnalyzedNode VisitCore(AnalyzedNode node)
                {
                    return node switch
                    {
                        Sequence n => VisitSequence(n),
                        Not n => Not.Create(VisitNode(n.Operand)),
                        Test test => VisitTest(test),
                        var n => n
                    };
                }

                AnalyzedNode VisitSequence(Sequence sequence)
                {
                    var tests = ArrayBuilder<AnalyzedNode>.GetInstance();
                    foreach (var node in sequence.Nodes)
                        tests.Add(VisitCore(node));
                    return sequence.Update(tests);
                }

                AnalyzedNode VisitTest(Test test)
                {
                    var rewrittenInput = VisitInput(test.Input);
                    if (test is Variable v)
                        variables.Add(v.DeclaredSymbol, rewrittenInput);
                    return test.WithInput(rewrittenInput);
                }

                Evaluation? VisitInput(Evaluation? originalInput)
                {
                    if (originalInput is null)
                        return originalInput;
                    var rewrittenInput = originalInput.WithInput(VisitInput(originalInput.Input));
                    switch (rewrittenInput)
                    {
                        case Variable v when variables.TryGetValue(v.DeclaredSymbol, out var newInput):
                            return newInput;
                        case NotNull _:
                        case Type _:
                            tests.Add(rewrittenInput);
                            return rewrittenInput.Input;
                        default:
                            return rewrittenInput;
                    }
                }
            }
        }
    }
}
