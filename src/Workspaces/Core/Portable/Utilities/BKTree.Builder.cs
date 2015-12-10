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
            private const int CompactEdgeAllocationSize = 4;

            private readonly char[][] _values;

            // Note: while building a BKTree we have to store children with parents, keyed by the
            // edit distance between the two.  Naive implementations might store a list or dictionary
            // of children along with each node.  However, this would be very inefficient and would
            // put an enormous amount of memory pressure on the system.
            //
            // Imperical data for a nice large assembly like mscorlib gives us the following 
            // information:
            // 
            //      Nodes length: 9662
            // 
            // If we stored a list or dictionary with each node, that would be 10s of thousands of
            // objects created that would then just have to be GCed.  That's a lot of garbage pressure
            // we'd like to avoid.
            //
            // Now if we look at all those nodes, we can see the following information about how many
            // children each has.
            //
            //      Edge counts:
            //      0   5560
            //      1   1884
            //      2   887
            //      3   527
            //      4   322
            //      5   200
            //      6   114
            //      7   69
            //      8   47
            //      9   20
            //      10  8
            //      11  10
            //      12  7
            //      13  4
            //      15  1
            //      16  1
            //      54  1
            //
            //
            // i.e. The number of nodes with edge-counts less than or equal to four is: 5560+1884+887+527+322=9180.
            // This is 95% of the total number of edges we are adding.  
            //
            // So, to optimize things, we pre-alloc a single array with space for 4 edges for each 
            // node we're going to add.  Each node then gets that much space to store edge information.
            // If it needs more than that space, then we have a fall-over dictionary that it can store 
            // information in.
            // 
            // Once building is complete, the GC only needs to deallocate this single array and the
            // spillover dictionaries.
            //
            // This approach produces 1/20th the amount of garbage while building the tree.
            //
            // Each node at index i has its edges in this array in the range [4*i, 4*i + 4);
            private readonly Edge[] _compactEdges;

            // If a node needs more than 4 children while building, then we spill over into this 
            // dictionary instead. 
            private readonly Dictionary<int, Dictionary<int, int>> _spilloverEdges;
            private readonly BuilderNode[] _builderNodes;

            public Builder(IEnumerable<string> values)
            {
                _values = values.Select(v => v.ToLower())
                                .Distinct()
                                .Select(v => v.ToCharArray())
                                .Where(a => a.Length > 0).ToArray();

                // We will have one node for each string value that we are adding.
                _builderNodes = new BuilderNode[_values.Length];
                _compactEdges = new Edge[_values.Length * CompactEdgeAllocationSize];
                _spilloverEdges = new Dictionary<int, Dictionary<int, int>>();
            }

            internal BKTree Create()
            {
                for (var i = 0; i < _values.Length; i++)
                {
                    Add(_values[i], insertionIndex: i);
                }

                var nodes = new Node[_builderNodes.Length];

                // There will be one less edge in the graph than nodes.  Each node (except for the
                // root) will have a single edge pointing to it.
                var edges = new Edge[_builderNodes.Length - 1];

                BuildArrays(nodes, edges);

                return new BKTree(nodes, edges);
            }

            private void BuildArrays(Node[] nodes, Edge[] edges)
            {
                var currentEdgeIndex = 0;
                for (var i = 0; i < _builderNodes.Length; i++)
                {
                    var builderNode = _builderNodes[i];
                    var edgeCount = builderNode.EdgeCount;

                    nodes[i] = new Node(
                        builderNode.LowerCaseCharacters, edgeCount, currentEdgeIndex);

                    if (edgeCount > 0)
                    {
                        if (edgeCount <= CompactEdgeAllocationSize)
                        {
                            // When tehre are less than 4 elements, we can just do an easy array 
                            // copy from our array into the final destination.
                            Array.Copy(_compactEdges, i * CompactEdgeAllocationSize, edges, currentEdgeIndex, edgeCount);
                            currentEdgeIndex += edgeCount;
                        }
                        else
                        {
                            // When there are more than 4 elements, then we have to manually 
                            // copy over the spilled items ourselves.
                            var spilledEdges = _spilloverEdges[i];
                            Debug.Assert(spilledEdges.Count == edgeCount);

                            foreach (var kvp in spilledEdges)
                            {
                                edges[currentEdgeIndex] = new Edge(kvp.Key, kvp.Value);
                                currentEdgeIndex++;
                            }
                        }
                    }
                }

                Debug.Assert(currentEdgeIndex == edges.Length);
            }

            private void Add(char[] lowerCaseCharacters, int insertionIndex)
            {
                if (insertionIndex == 0)
                {
                    _builderNodes[insertionIndex] = new BuilderNode(lowerCaseCharacters);
                    return;
                }

                var currentNodeIndex = 0;
                while (true)
                {
                    var currentNode = _builderNodes[currentNodeIndex];

                    var editDistance = EditDistance.GetEditDistance(currentNode.LowerCaseCharacters, lowerCaseCharacters);
                    // This shoudl never happen.  We dedupe all items before proceeding to the 'Add' step.
                    Debug.Assert(editDistance != 0);

                    int childNodeIndex;
                    if (TryGetChildIndex(currentNode, currentNodeIndex, editDistance, out childNodeIndex))
                    {
                        // Edit distances collide.  Move to this child and add this word to it.
                        currentNodeIndex = childNodeIndex;
                        continue;
                    }

                    // Node doesn't have an edge with this edit distance. Three cases to handle:
                    // 1) there are less than 4 edges.  We simply place the edge into the correct
                    //    location in compactEdges
                    // 2) there are 4 edges.  We need to copy these edges into the spillover 
                    //    dictionary and then add the new edge into that.
                    // 3) there are more than 4 edges.  Just put the new edge in the spillover 
                    //    dictionary.

                    if (currentNode.EdgeCount < CompactEdgeAllocationSize)
                    {
                        _compactEdges[currentNodeIndex * CompactEdgeAllocationSize + currentNode.EdgeCount] =
                            new Edge(editDistance, insertionIndex);
                    }
                    else
                    {
                        if (currentNode.EdgeCount == CompactEdgeAllocationSize)
                        {
                            var spillover = new Dictionary<int, int>();
                            _spilloverEdges[currentNodeIndex] = spillover;

                            var start = currentNodeIndex * CompactEdgeAllocationSize;
                            var end = start + CompactEdgeAllocationSize;
                            for (var i = start; i < end; i++)
                            {
                                var edge = _compactEdges[i];
                                spillover.Add(edge.EditDistance, edge.ChildNodeIndex);
                            }
                        }

                        _spilloverEdges[currentNodeIndex].Add(editDistance, insertionIndex);
                    }

                    _builderNodes[currentNodeIndex].EdgeCount++;
                    _builderNodes[insertionIndex] = new BuilderNode(lowerCaseCharacters);
                    return;
                }
            }

            private bool TryGetChildIndex(BuilderNode currentNode, int currentNodeIndex, int editDistance, out int childIndex)
            {
                if (currentNode.EdgeCount > CompactEdgeAllocationSize)
                {
                    // Can't use the compact array.  Have to use the spillover dictionary instead.
                    var dictionary = _spilloverEdges[currentNodeIndex];
                    Debug.Assert(dictionary.Count == currentNode.EdgeCount);
                    return dictionary.TryGetValue(editDistance, out childIndex);
                }

                // linearly scan the children we have to see if there is one with this edit distance.
                var start = currentNodeIndex * CompactEdgeAllocationSize;
                var end = start + currentNode.EdgeCount;

                for (var i = start; i < end; i++)
                {
                    if (_compactEdges[i].EditDistance == editDistance)
                    {
                        childIndex = _compactEdges[i].ChildNodeIndex;
                        return true;
                    }
                }

                childIndex = -1;
                return false;
            }

            private struct BuilderNode
            {
                public readonly char[] LowerCaseCharacters;
                public int EdgeCount;

                public BuilderNode(char[] lowerCaseCharacters) : this()
                {
                    this.LowerCaseCharacters = lowerCaseCharacters;
                }
            }
        }
    }
}