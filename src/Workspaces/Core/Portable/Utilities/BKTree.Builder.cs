// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Utilities
{
    internal partial class BKTree
    {
        private class Builder
        {
            // The number of edges we pre-allocate space for for each node in _compactEdges.
            //
            // To make the comments simpler below, i'll use '4' as a synonym for CompactEdgeAllocationSize.
            // '4' simply reads better and makes it clearer what's going on.
            private const int CompactEdgeAllocationSize = 4;

            // Instead of producing a char[] for each string we're building a node for, we instead 
            // have one long char[] with all the chracters of each string concatenated.  i.e.
            // "foo" "bar" and "baz" becomes { f, o, o, b, a, r, b, a, z }.  Then in _wordSpans
            // we have the text spans for each of those words in this array.  This gives us only
            // two allocations instead of as many allocations as the number of strings we have.
            //
            // Once we are done building, we pass this to the BKTree and its nodes also state the
            // span of this array that corresponds to the word they were created for.  This works
            // well as other dependent facilities (like EditDistance) can work on sub-arrays without
            // any problems.
            private readonly char[] _concatenatedLowerCaseWords;
            private readonly TextSpan[] _wordSpans;

            // Note: while building a BKTree we have to store children with parents, keyed by the
            // edit distance between the two.  Naive implementations might store a list or dictionary
            // of children along with each node.  However, this would be very inefficient and would
            // put an enormous amount of memory pressure on the system.
            //
            // Emperical data for a nice large assembly like mscorlib gives us the following 
            // information:
            // 
            //      Unique-Words (ignoring case): 9662
            // 
            // For each unique word we need a node in the BKTree. If we stored a list or dictionary 
            // with each node, that would be 10s of thousands of objects created that would then 
            // just have to be GCed.  That's a lot of garbage pressure we'd like to avoid.
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
            // This is 95% of the total number of edges we are adding.  Looking at many other dlls
            // we found that this ratio stays true across the board.  i.e. with all dlls, 95% of nodes
            // have 4 or less edges.
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
            private readonly BuilderNode[] _builderNodes;

            public Builder(IEnumerable<string> values)
            {
                // TODO(cyrusn): Properly handle unicode normalization here.
                var distinctValues = values.Where(v => v.Length > 0).Distinct(CaseInsensitiveComparison.Comparer).ToArray();
                var charCount = values.Sum(v => v.Length);

                _concatenatedLowerCaseWords = new char[charCount];
                _wordSpans = new TextSpan[distinctValues.Length];

                var characterIndex = 0;
                for (int i = 0; i < distinctValues.Length; i++)
                {
                    var value = distinctValues[i];
                    _wordSpans[i] = new TextSpan(characterIndex, value.Length);

                    foreach (var ch in value)
                    {
                        _concatenatedLowerCaseWords[characterIndex] = CaseInsensitiveComparison.ToLower(ch);
                        characterIndex++;
                    }
                }

                // We will have one node for each string value that we are adding.
                _builderNodes = new BuilderNode[distinctValues.Length];
                _compactEdges = new Edge[distinctValues.Length * CompactEdgeAllocationSize];
            }

            internal BKTree Create()
            {
                for (var i = 0; i < _wordSpans.Length; i++)
                {
                    Add(_wordSpans[i], insertionIndex: i);
                }

                var nodes = ImmutableArray.CreateBuilder<Node>(_builderNodes.Length);

                // There will be one less edge in the graph than nodes.  Each node (except for the
                // root) will have a single edge pointing to it.
                var edges = ImmutableArray.CreateBuilder<Edge>(Math.Max(0, _builderNodes.Length - 1));

                BuildArrays(nodes, edges);

                return new BKTree(_concatenatedLowerCaseWords, nodes.MoveToImmutable(), edges.MoveToImmutable());
            }

            private void BuildArrays(ImmutableArray<Node>.Builder nodes, ImmutableArray<Edge>.Builder edges)
            {
                var currentEdgeIndex = 0;
                for (var i = 0; i < _builderNodes.Length; i++)
                {
                    var builderNode = _builderNodes[i];
                    var edgeCount = builderNode.EdgeCount;

                    nodes.Add(new Node(builderNode.CharacterSpan, edgeCount, currentEdgeIndex));

                    if (edgeCount > 0)
                    {
                        // First, copy any edges that are in the compact array.
                        var start = i * CompactEdgeAllocationSize;
                        var end = start + Math.Min(edgeCount, CompactEdgeAllocationSize);
                        for (var j = start; j < end; j++)
                        {
                            edges.Add(_compactEdges[j]);
                        }

                        // Then, if we've spilled over any edges, copy them as well.
                        var spilledEdges = builderNode.SpilloverEdges;
                        if (spilledEdges != null)
                        {
                            Debug.Assert(spilledEdges.Count == (edgeCount - CompactEdgeAllocationSize));

                            foreach (var kvp in spilledEdges)
                            {
                                edges.Add(new Edge(kvp.Key, kvp.Value));
                            }
                        }
                    }

                    currentEdgeIndex += edgeCount;
                }

                Debug.Assert(currentEdgeIndex == edges.Capacity);
                Debug.Assert(currentEdgeIndex == edges.Count);
            }

            private void Add(TextSpan characterSpan, int insertionIndex)
            {
                if (insertionIndex == 0)
                {
                    _builderNodes[insertionIndex] = new BuilderNode(characterSpan);
                    return;
                }

                var currentNodeIndex = 0;
                while (true)
                {
                    var currentNode = _builderNodes[currentNodeIndex];

                    // Determine the edit distance between these two words.  Note: we do not use
                    // a threshold here as we need the actual edit distance so we can actually
                    // determine what edge to make or walk.
                    var editDistance = EditDistance.GetEditDistance(
                        new ArraySlice<char>(_concatenatedLowerCaseWords, currentNode.CharacterSpan),
                        new ArraySlice<char>(_concatenatedLowerCaseWords, characterSpan));

                    if (editDistance == 0)
                    {
                        // This should never happen.  We dedupe all items before proceeding to the 'Add' step.
                        // So the edit distance should always be non-zero.
                        throw new InvalidOperationException();
                    }

                    int childNodeIndex;
                    if (TryGetChildIndex(currentNode, currentNodeIndex, editDistance, out childNodeIndex))
                    {
                        // Edit distances collide.  Move to this child and add this word to it.
                        currentNodeIndex = childNodeIndex;
                        continue;
                    }

                    // found the node we want to add the child node to.
                    AddChildNode(characterSpan, insertionIndex, currentNode.EdgeCount, currentNodeIndex, editDistance);
                    return;
                }
            }

            private void AddChildNode(
                TextSpan characterSpan, int insertionIndex, int currentNodeEdgeCount, int currentNodeIndex, int editDistance)
            {
                // The node as 'currentNodeIndex' doesn't have an edge with this edit distance. 
                // Three cases to handle:
                // 1) there are less than 4 edges.  We simply place the edge into the correct
                //    location in compactEdges
                // 2) there are 4 edges.  We need to make the spillover dictionary and then add 
                //    the new edge into that.
                // 3) there are more than 4 edges.  Just put the new edge in the spillover 
                //    dictionary.

                if (currentNodeEdgeCount < CompactEdgeAllocationSize)
                {
                    _compactEdges[currentNodeIndex * CompactEdgeAllocationSize + currentNodeEdgeCount] =
                        new Edge(editDistance, insertionIndex);
                }
                else
                {
                    // When we hit 4 elements, we need to allocate the spillover dictionary to 
                    // place the extra edges.
                    if (currentNodeEdgeCount == CompactEdgeAllocationSize)
                    {
                        Debug.Assert(_builderNodes[currentNodeIndex].SpilloverEdges == null);
                        var spilloverEdges = new Dictionary<int, int>();
                        _builderNodes[currentNodeIndex].SpilloverEdges = spilloverEdges;
                    }

                    _builderNodes[currentNodeIndex].SpilloverEdges.Add(editDistance, insertionIndex);
                }

                _builderNodes[currentNodeIndex].EdgeCount++;
                _builderNodes[insertionIndex] = new BuilderNode(characterSpan);
                return;
            }

            private bool TryGetChildIndex(BuilderNode currentNode, int currentNodeIndex, int editDistance, out int childIndex)
            {
                // linearly scan the children we have to see if there is one with this edit distance.
                var start = currentNodeIndex * CompactEdgeAllocationSize;
                var end = start + Math.Min(currentNode.EdgeCount, CompactEdgeAllocationSize);

                for (var i = start; i < end; i++)
                {
                    if (_compactEdges[i].EditDistance == editDistance)
                    {
                        childIndex = _compactEdges[i].ChildNodeIndex;
                        return true;
                    }
                }

                // If we've spilled over any edges, check there as well
                if (currentNode.SpilloverEdges != null)
                {
                    // Can't use the compact array.  Have to use the spillover dictionary instead.
                    Debug.Assert(currentNode.SpilloverEdges.Count == (currentNode.EdgeCount - CompactEdgeAllocationSize));
                    return currentNode.SpilloverEdges.TryGetValue(editDistance, out childIndex);
                }

                childIndex = -1;
                return false;
            }

            private struct BuilderNode
            {
                public readonly TextSpan CharacterSpan;
                public int EdgeCount;
                public Dictionary<int, int> SpilloverEdges;

                public BuilderNode(TextSpan characterSpan) : this()
                {
                    this.CharacterSpan = characterSpan;
                }
            }
        }
    }
}