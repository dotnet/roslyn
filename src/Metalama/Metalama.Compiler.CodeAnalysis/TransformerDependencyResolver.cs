using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Metalama.Compiler.Graphs;

namespace Metalama.Compiler;

/// <summary>
/// Compares and sorts dependency objects.
/// </summary>
internal static class TransformerDependencyResolver
{
    public static bool Sort(ref ImmutableArray<ISourceTransformer>.Builder transformers,
        IReadOnlyList<ImmutableArray<string?>> transformerOrders, List<DiagnosticInfo> diagnostics)
    {
        // HACK: The proper way to report an error would be to switch from List<DiagnosticInfo> to DiagnosticBag here and several levels in the call graph above this method.
        // But using MetalamaCompilerMessageProvider requires less changes to Roslyn code, hopefully making future maintenance of the fork easier.

        // Build a graph of dependencies between unorderedTransformations.
        var n = transformers.Count;

        // Check that transformers are unique by full name.
        var transformersByName = transformers.Select(t => (Name: t.GetType().FullName!, Transformer: t))
            .GroupBy(t => t.Name)
            .ToList();

        foreach (var transformerGroup in transformersByName.Where(g => g.Count() > 1))
        {
            diagnostics.Add(new DiagnosticInfo(MetalamaCompilerMessageProvider.Instance,
                (int)MetalamaErrorCode.ERR_TransformerNotUnique, transformerGroup.Key,
                string.Join(", ", transformerGroup.Select(t => $"'{t.Transformer.GetType().AssemblyQualifiedName}'"))));
            return false;
        }

        var nameToIndexMapping = transformers.Select((t, i) => (t.GetType().FullName, i))
            .ToDictionary(x => x.FullName!, x => x.i);

        var graph = new Graph(n);
        var hasPredecessor = new bool[n];

        foreach (var order in transformerOrders)
        {
            int? previousIndex = null;

            foreach (var transformerName in order)
            {
                if (transformerName == null)
                {
                    continue;
                }

                int? currentIndex = nameToIndexMapping.TryGetValue(transformerName, out var index) ? index : null;
                if (currentIndex == null)
                {
                      diagnostics.Add(new DiagnosticInfo(MetalamaCompilerMessageProvider.Instance,
                        (int)MetalamaErrorCode.ERR_TransformerNotFound, transformerName));
                    return false;
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
        var distances = graph.GetInitialVector();
        var predecessors = graph.GetInitialVector();

        var cycle = -1;
        for (var i = 0; i < n; i++)
        {
            if (!hasPredecessor[i])
            {
                cycle = graph.DoBreadthFirstSearch(i, distances, predecessors);
                if (cycle >= 0)
                {
                    break;
                }
            }
        }

        // If did not manage to find a cycle, we need to check that we have ordered the whole graph.
        if (cycle < 0)
        {
            for (var i = 0; i < n; i++)
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
            var cycleStack = new Stack<int>(transformers.Count);

            var cursor = cycle;
            do
            {
                cycleStack.Push(cursor);
                cursor = predecessors[cursor];
            } while (cursor != cycle && /* Workaround PostSharp bug 25438 */ cursor != AbstractGraph.NotDiscovered);

            var transformersCopy = transformers;
            var cycleNodes = cycleStack.Select(i => transformersCopy[i].GetDisplayName());
            var cycleNodesString = string.Join(", ", cycleNodes);

            diagnostics.Add(new DiagnosticInfo(MetalamaCompilerMessageProvider.Instance,
                (int)MetalamaErrorCode.ERR_TransformerCycleFound, cycleNodesString));

            return true;
        }

        // Sort the distances vector.
        var sortedIndexes = new int[n];
        for (var i = 0; i < n; i++)
        {
            sortedIndexes[i] = i;
        }

        Array.Sort(sortedIndexes, (i, j) => distances[i].CompareTo(distances[j]));

        // Build the ordered list of transformations.
        var orderedTransformers = ImmutableArray.CreateBuilder<ISourceTransformer>(n);
        for (var i = 0; i < n; i++)
        {
            orderedTransformers.Add(transformers[sortedIndexes[i]]);
        }

        transformers = orderedTransformers;

        // Check that all the constraints are respected.
        var lastDistance = -1;

        for (var i = 0; i < n; i++)
        {
            // Check that all transformers are strongly ordered.
            var currentDistance = distances[sortedIndexes[i]];
            if (currentDistance == lastDistance)
            {
                // We discovered a group without strong ordering.

                // Transformers "{1}" and "{2}" are not strongly ordered. 
                // Their order of execution is nondeterministic.
                System.Diagnostics.Debugger.Launch();
                diagnostics.Add(new DiagnosticInfo(
                    MetalamaCompilerMessageProvider.Instance, (int)MetalamaErrorCode.WRN_TransformersNotOrdered,
                    orderedTransformers[i - 1].GetDisplayName(), orderedTransformers[i].GetDisplayName()));
            }

            lastDistance = currentDistance;
        }

        return true;
    }

    private static string GetDisplayName(this ISourceTransformer transformer)
    {
        return transformer.GetType().FullName!;
    }
}
