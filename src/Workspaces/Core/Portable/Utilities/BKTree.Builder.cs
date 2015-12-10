using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    internal partial class BKTree
    {
        private class Builder
        {
            private readonly char[][] values;

            public Builder(IEnumerable<string> values)
            {
                this.values = values.Select(v => v.ToLower()).Distinct().Select(v => v.ToCharArray()).ToArray();
            }

            internal BKTree Create()
            {
                var builderNodes = new List<BuilderNode>(values.Length);
                foreach (var value in values)
                {
                    if (value.Length > 0)
                    {
                        Add(builderNodes, value);
                    }
                }

                var nodeArray = new Node[builderNodes.Count];
                var editDistanceList = new List<EditDistanceAndChildIndex>(builderNodes.Count);

                BuildArrays(builderNodes, nodeArray, editDistanceList);

                return new BKTree(nodeArray, editDistanceList.ToArray());
            }

            private void BuildArrays(List<BuilderNode> builderNodes, Node[] nodeArray, List<EditDistanceAndChildIndex> editDistanceList)
            {
                var currentIndexInEditDistanceList = 0;
                for (var i =0; i < builderNodes.Count; i++)
                {
                    var builderNode = builderNodes[i];
                    var childCount = builderNode.AllChildren == null ? 0 : builderNode.AllChildren.Count;

                    nodeArray[i] = new Node(
                        builderNode.LowerCaseCharacters, childCount, currentIndexInEditDistanceList);

                    currentIndexInEditDistanceList += childCount;

                    if (builderNode.AllChildren != null)
                    {
                        foreach (var kvp in builderNode.AllChildren)
                        {
                            editDistanceList.Add(new EditDistanceAndChildIndex(kvp.Key, kvp.Value));
                        }
                    }
                }
            }

            private static void Add(List<BuilderNode> nodes, char[] lowerCaseCharacters)
            {
                if (nodes.Count == 0)
                {
                    nodes.Add(new BuilderNode(lowerCaseCharacters));
                    return;
                }

                var currentNodeIndex = 0;
                while (true)
                {
                    var currentNode = nodes[currentNodeIndex];

                    var editDistance = EditDistance.GetEditDistance(currentNode.LowerCaseCharacters, lowerCaseCharacters);
                    // This shoudl never happen.  We dedupe all items before proceeding to the 'Add' step.
                    Debug.Assert(editDistance != 0);

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
                    nodes.Add(new BuilderNode(lowerCaseCharacters));
                    return;
                }
            }

            private struct BuilderNode
            {
                public readonly char[] LowerCaseCharacters;

                // The edit distance and node index of our child if we only have one. Both values will 
                // be -1 if we have no children or if we have multiple children.
                //public EditDistanceAndChildIndex SingleChild;

                // Maps from our edit distance to our single child with that edit distance (when we have 
                // multiple children).  Null if we have zero or one child.
                public Dictionary<int, int> AllChildren;

                public bool HasChildren => /*!this.SingleChild.IsNull ||*/ this.AllChildren != null;

                public BuilderNode(char[] lowerCaseCharacters) : this()
                {
                    this.LowerCaseCharacters = lowerCaseCharacters;
                    // SingleChild = EditDistanceAndChildIndex.Null;
                }
            }
        }
    }
}
