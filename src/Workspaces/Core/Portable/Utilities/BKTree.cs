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
    internal class BKTree
    {
        // Root node is at index 0.
        private readonly Node[] nodes;

        private BKTree(Node[] nodes)
        {
            this.nodes = nodes;
        }

        private struct Node
        {
            public readonly char[] LowerCaseCharacters;

            // The edit distance and node index of our child if we only have one. Both values will 
            // be -1 if we have no children or if we have multiple children.
            //public EditDistanceAndChildIndex SingleChild;

            // Maps from our edit distance to our single child with that edit distance (when we have 
            // multiple children).  Null if we have zero or one child.
            public Dictionary<int, int> AllChildren;

            public bool HasChildren => /*!this.SingleChild.IsNull ||*/ this.AllChildren != null;

            public Node(char[] lowerCaseCharacters) : this()
            {
                this.LowerCaseCharacters = lowerCaseCharacters;
                // SingleChild = EditDistanceAndChildIndex.Null;
            }
        }

        public static BKTree Create(params string[] values)
        {
            return Create((IEnumerable<string>)values);
        }

        public static BKTree Create(IEnumerable<string> values)
        {
            return Create(values.Select(v => v.ToLower()).Distinct().Select(v => v.ToCharArray()).ToArray());
        }

        private static BKTree Create(char[][] values)
        {
            var nodes = new List<Node>();
            foreach (var value in values)
            {
                if (value.Length > 0)
                {
                    Add(nodes, value);
                }
            }

            return new BKTree(nodes.ToArray());
        }

        private static void Add(List<Node> nodes, char[] lowerCaseCharacters)
        {
            if (nodes.Count == 0)
            {
                nodes.Add(new Node(lowerCaseCharacters));
                return;
            }

            var currentNodeIndex = 0;
            while (true)
            {
                var currentNode = nodes[currentNodeIndex];

                var editDistance = EditDistance.GetEditDistance(currentNode.LowerCaseCharacters, lowerCaseCharacters);
                if (editDistance == 0)
                {
                    // Already in the graph.  Can happen because we added something that is the same
                    // as an existing item, but only differs in case.
                    return;
                }

                if (currentNode.AllChildren == null)
                {
                    currentNode.AllChildren = new Dictionary<int, int>();
                    nodes[currentNodeIndex] = currentNode;
                    // Fall through. to actually add the child to this node.
                }
                else
                {
                    int childNodeIndex;
                    if (currentNode.AllChildren.TryGetValue(editDistance, out childNodeIndex))
                    {
                        // Edit distances collide.  Move to this child and add this word to it.
                        currentNodeIndex = childNodeIndex;
                        continue;
                    }

                    // Fall through. to actually add the child to this node.
                }

                currentNode.AllChildren.Add(editDistance, nodes.Count);
                nodes.Add(new Node(lowerCaseCharacters));
                return;
            }
        }

        public IList<string> Find(string value, int? threshold = null)
        {
            if (nodes.Length == 0)
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
                Lookup(nodes[0], lowerCaseCharacters, value.Length, threshold.Value, result);
                return result;
            }
            finally
            {
                ArrayPool<char>.ReleaseArray(lowerCaseCharacters);
            }
        }

        private void Lookup(Node currentNode, char[] queryCharacters, int queryLength, int threshold, List<string> result)
        {
            var editDistance = EditDistance.GetEditDistance(
                currentNode.LowerCaseCharacters, queryCharacters,
                currentNode.LowerCaseCharacters.Length, queryLength);

            if (editDistance <= threshold)
            {
                // Found a match.
                result.Add(new string(currentNode.LowerCaseCharacters));
            }

            if (!currentNode.HasChildren)
            {
                return;
            }

            var min = editDistance - threshold;
            var max = editDistance + threshold;

            for (var i = min; i <= max; i++)
            {
                int childIndex;
                if (currentNode.AllChildren.TryGetValue(i, out childIndex))
                {
                    Lookup(this.nodes[childIndex], queryCharacters, queryLength, threshold, result);
                }
            }
        }

        internal void DumpStats()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Nodes length: " + nodes.Length);
            var childCountHistogram = new Dictionary<int, int>();

            foreach (var node in nodes)
            {
                var childCount = node.AllChildren == null ? 0 : node.AllChildren.Count;
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

            foreach (var node in nodes)
            {
                if (node.AllChildren == null)
                {
                    empyCount++;
                    continue;
                }

                var maxEditDistance = node.AllChildren.Max(kvp => kvp.Key);
                var editDistanceCount = node.AllChildren.Count;

                var bucket = 10 * editDistanceCount / maxEditDistance;
                densities[bucket]++;
            }

            var nonEmptyCount = nodes.Length - empyCount;
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