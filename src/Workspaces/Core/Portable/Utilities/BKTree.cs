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
        // We have two completely flat arrays of structs (except for the char[] values the nodes
        // point to).  These arrays fully represent the BK tree.  The structure is as follows:
        //
        // The root node is in nodeArray[0].
        //
        // It lists the count of children it has.  These children are in editDistanceArray 
        // in the range [0*, childCount).  Each item in editDistance array is information about
        // a child of the root.  Specifically the edit distance between the root and it, and
        // what location in nodeArray the child is at.
        //
        // * of course '0' is only for the root case.  All nodes state where in editDistanceArray
        // their child range starts.  So the children for any node are in editDistanceArray from
        // [node.FirstChildIndex, node.FirstChildIndex + node.ChildCount)

        private readonly Node[] nodeArray;
        private readonly EditDistanceAndChildIndex[] editDistanceArray;

        private BKTree(Node[] nodeArray, EditDistanceAndChildIndex[] editDistanceArray)
        {
            this.nodeArray = nodeArray;
            this.editDistanceArray = editDistanceArray;
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
            if (nodeArray.Length == 0)
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
                Lookup(nodeArray[0], lowerCaseCharacters, value.Length, threshold.Value, result);
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

            var min = editDistance - threshold;
            var max = editDistance + threshold;

            var firstChild = currentNode.FirstChildIndexInEditDistanceArray;
            var lastChild = firstChild + currentNode.ChildCount;
            for (var i = firstChild; i < lastChild; i++)
            {
                var childEditDistance = editDistanceArray[i].EditDistance;
                if (min <= childEditDistance && childEditDistance <= max)
                {
                    Lookup(this.nodeArray[editDistanceArray[i].ChildNodeIndexInNodeArray],
                        queryCharacters, queryLength, threshold, result);
                }
            }
        }

        internal void DumpStats()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Nodes length: " + nodeArray.Length);
            var childCountHistogram = new Dictionary<int, int>();

            foreach (var node in nodeArray)
            {
                var childCount = node.ChildCount;
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

            foreach (var node in nodeArray)
            {
                if (node.ChildCount == 0)
                {
                    empyCount++;
                    continue;
                }

                var maxEditDistance = -1;
                var firstChild = node.FirstChildIndexInEditDistanceArray;
                var lastChild = firstChild + node.ChildCount;
                for (var i = firstChild; i < lastChild; i++)
                {
                    maxEditDistance = Max(maxEditDistance, editDistanceArray[i].EditDistance);
                }

                var editDistanceCount = node.ChildCount;

                var bucket = 10 * editDistanceCount / maxEditDistance;
                densities[bucket]++;
            }

            var nonEmptyCount = nodeArray.Length - empyCount;
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