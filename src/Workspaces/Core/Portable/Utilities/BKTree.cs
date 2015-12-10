using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Utilities;
using static System.Math;
using static Roslyn.Utilities.PortableShim;

namespace Roslyn.Utilities
{
    internal partial class BKTree
    {
        public static readonly BKTree Empty = new BKTree(
            SpecializedCollections.EmptyArray<Node>(),
            SpecializedCollections.EmptyArray<Edge>());

        // We have two completely flat arrays of structs (except for the char[] values the nodes
        // point to).  These arrays fully represent the BK tree.  The structure is as follows:
        //
        // The root node is in _nodes[0].
        //
        // It lists the count of edges it has.  These edges are in _edges in the range 
        // [0*, childCount).  Each edge has the index of the child node it points to, and the
        // edit distance between the parent and the child.
        //
        // * of course '0' is only for the root case.  All nodes state where in _edges
        // their child edgs range starts.  So the children for any node are in _edges from
        // [node.FirstEdgeIndex, node.FirstEdgeIndex + node.EdgeCount)

        private readonly Node[] _nodes;
        private readonly Edge[] _edges;

        private BKTree(Node[] nodes, Edge[] edges)
        {
            _nodes = nodes;
            _edges = edges;
        }

        public static BKTree Create(params string[] values)
        {
            return Create((IEnumerable<string>)values);
        }

        public static BKTree Create(IEnumerable<string> values)
        {
            return new Builder(values).Create();
        }

        public IList<string> Find(string value, int? threshold = null)
        {
            if (_nodes.Length == 0)
            {
                return SpecializedCollections.EmptyList<string>();
            }

            var lowerCaseCharacters = ArrayPool<char>.GetArray(value.Length);
            try
            {
                for (var i = 0; i < value.Length; i++)
                {
                    lowerCaseCharacters[i] = char.ToLower(value[i]);
                }

                threshold = threshold ?? EditDistance.GetThreshold(value);
                var result = new List<string>();
                Lookup(_nodes[0], lowerCaseCharacters, value.Length, threshold.Value, result);
                return result;
            }
            finally
            {
                ArrayPool<char>.ReleaseArray(lowerCaseCharacters);
            }
        }

        private void Lookup(Node currentNode, char[] queryCharacters, int queryLength, int threshold, List<string> result)
        {
            // We always want to compute the real edit distance (ignoring any thresholds).  This is
            // because we need that edit distance to appropriately determine which edges to walk 
            // in the tree.
            var editDistance = EditDistance.GetEditDistance(
                currentNode.LowerCaseCharacters, queryCharacters,
                currentNode.LowerCaseCharacters.Length, queryLength,
                useThreshold: false);

            if (editDistance <= threshold)
            {
                // Found a match.
                result.Add(new string(currentNode.LowerCaseCharacters));
            }

            var min = editDistance - threshold;
            var max = editDistance + threshold;

            var firstEdgeIndex = currentNode.FirstEdgeIndex;
            var lastEdgeIndex = firstEdgeIndex + currentNode.EdgeCount;
            for (var i = firstEdgeIndex; i < lastEdgeIndex; i++)
            {
                var childEditDistance = _edges[i].EditDistance;
                if (min <= childEditDistance && childEditDistance <= max)
                {
                    Lookup(this._nodes[_edges[i].ChildNodeIndex],
                        queryCharacters, queryLength, threshold, result);
                }
            }
        }

        internal void DumpStats()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Nodes length: " + _nodes.Length);
            var childCountHistogram = new Dictionary<int, int>();

            foreach (var node in _nodes)
            {
                var childCount = node.EdgeCount;
                int existing;
                childCountHistogram.TryGetValue(childCount, out existing);

                childCountHistogram[childCount] = existing + 1;
            }

            sb.AppendLine();
            sb.AppendLine("Child counts:");
            foreach (var kvp in childCountHistogram.OrderBy(kvp => kvp.Key))
            {
                sb.AppendLine(kvp.Key + "\t" + kvp.Value);
            }

            // An item is dense if, starting from 1, at least 80% of it's array would be full.
            var densities = new int[11];
            var empyCount = 0;

            foreach (var node in _nodes)
            {
                if (node.EdgeCount == 0)
                {
                    empyCount++;
                    continue;
                }

                var maxEditDistance = -1;
                var firstChild = node.FirstEdgeIndex;
                var lastChild = firstChild + node.EdgeCount;
                for (var i = firstChild; i < lastChild; i++)
                {
                    maxEditDistance = Max(maxEditDistance, _edges[i].EditDistance);
                }

                var editDistanceCount = node.EdgeCount;

                var bucket = 10 * editDistanceCount / maxEditDistance;
                densities[bucket]++;
            }

            var nonEmptyCount = _nodes.Length - empyCount;
            sb.AppendLine();
            sb.AppendLine("NoChildren: " + empyCount);
            sb.AppendLine("AnyChildren: " + nonEmptyCount);
            sb.AppendLine("Densities:");
            for (var i = 0; i < densities.Length; i++)
            {
                sb.AppendLine("<=" + i + "0% = " + densities[i] + ", " + ((float)densities[i] / nonEmptyCount));
            }

            var result = sb.ToString();
        }
    }
}