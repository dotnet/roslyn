using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using RoslynEx.Graphs;

namespace RoslynEx
{
    /// <summary>
    /// Compares and sorts dependency objects.
    /// </summary>
    internal static class TransformerDependencyResolver
    {
        public static void Sort(ref ImmutableArray<ISourceTransformer>.Builder transformers, IReadOnlyList<ImmutableArray<string>> transformerOrders, List<DiagnosticInfo> diagnostics)
        {
            // Build a graph of dependencies between unorderedTransformations.
            int n = transformers.Count;

            Dictionary<string, int> nameToIndexMapping = transformers.Select((t, i) => (t.GetType().FullName, i)).ToDictionary(x => x.FullName, x => x.i);

            AbstractGraph graph = AbstractGraph.CreateGraph(n);
            bool[] hasPredecessor = new bool[n];

            foreach (var order in transformerOrders)
            {
                int? previousIndex = null;

                foreach (var transformerName in order)
                {
                    int? currentIndex = nameToIndexMapping.TryGetValue(transformerName, out int index) ? index : null;
                    if (currentIndex == null)
                    {
                        // HACK: The proper way to do this would be to switch from List<DiagnosticInfo> to DiagnosticBag here and several levels in the call graph above this method.
                        // But using RoslynExMessageProvider requires less changes to Roslyn code, hopefully making future maintanance of the fork easier.
                        diagnostics.Add(new DiagnosticInfo(RoslynExMessageProvider.Instance, RoslynExMessageProvider.ERR_TransformerNotFound, transformerName));
                    }
                    else
                    {
                        if (previousIndex != null)
                        {
                            graph.AddEdge(previousIndex.Value, currentIndex.Value);
                            hasPredecessor[currentIndex.Value] = true;
                        }

                        previousIndex = currentIndex;
                    }
                }
            }

            // Perform a breadth-first search on the graph.
            int[] distances = graph.GetInitialVector();
            int[] predecessors = graph.GetInitialVector();

            int cycle = -1;
            for (int i = 0; i < n; i++)
            {
                if (!hasPredecessor[i])
                {
                    cycle = graph.DoBreadthFirstSearch(i, distances, predecessors);
                    if (cycle >= 0) break;
                }
            }

            // If did not manage to find a cycle, we need to check that we have ordered the whole graph.
            if (cycle < 0)
            {
                for (int i = 0; i < n; i++)
                {
                    if (distances[i] == AbstractGraph.NotDiscovered)
                    {
                        // There is a node that we haven't ordered, which means that there is a cycle.
                        // Force the detection on the node to find the cycle.
                        cycle = graph.DoBreadthFirstSearch(0, distances, predecessors);
                        break;
                    }
                }
            }

            // Detect cycles.
            if (cycle >= 0)
            {
                // Build a string containing the unorderedTransformations of the cycle.
                Stack<int> cycleStack = new Stack<int>(transformers.Count);

                int cursor = cycle;
                do
                {
                    cycleStack.Push(cursor);
                    cursor = predecessors[cursor];
                } while (cursor != cycle && /* Workaround PostSharp bug 25438 */ cursor != AbstractGraph.NotDiscovered);

                var transformersCopy = transformers;
                var cycleNodes = cycleStack.Select(cursor => transformersCopy[cursor].GetDisplayName());
                var cycleNodesString = string.Join(", ", cycleNodes);

                diagnostics.Add(new DiagnosticInfo(RoslynExMessageProvider.Instance, RoslynExMessageProvider.ERR_TransformerCycleFound, cycleNodesString));

                return;
            }

            // Sort the distances vector.
            int[] sortedIndexes = new int[n];
            for (int i = 0; i < n; i++)
                sortedIndexes[i] = i;
            Array.Sort(sortedIndexes, (i, j) => distances[i].CompareTo(distances[j]));

            // Build the ordered list of transformations.
            var orderedTransformers = ImmutableArray.CreateBuilder<ISourceTransformer>(n);
            for (int i = 0; i < n; i++)
            {
                orderedTransformers.Add(transformers[sortedIndexes[i]]);
            }
            transformers = orderedTransformers;

            // Check that all the constraints are respected.
            int lastDistance = -1;

            for (int i = 0; i < n; i++)
            {
                var transformer = orderedTransformers[i];

                // Check that all transformers are strongly ordered.
                int currentDistance = distances[sortedIndexes[i]];
                if (currentDistance == lastDistance)
                {
                    // We discovered a group without strong ordering.

                    // Transformers "{1}" and "{2}" are not strongly ordered. 
                    // Their order of execution is nondeterministic.
                    diagnostics.Add(new DiagnosticInfo(
                        RoslynExMessageProvider.Instance, RoslynExMessageProvider.ERR_TransformersNotOrdered,
                        orderedTransformers[i - 1].GetDisplayName(), orderedTransformers[i].GetDisplayName()));
                }

                lastDistance = currentDistance;
            }
        }

        private static string GetDisplayName(this ISourceTransformer transformer) => transformer.GetType().FullName;
    }
}
