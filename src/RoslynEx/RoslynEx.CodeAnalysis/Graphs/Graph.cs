using System.Collections.Generic;

namespace RoslynEx.Graphs
{
    internal sealed class Graph : AbstractGraph
    {
        private readonly SimpleLinkedListNode<int>[] successors;
        private readonly SimpleLinkedListNode<int>[] predecessors;

        public Graph( int size ) : base( size )
        {
            this.successors = new SimpleLinkedListNode<int>[size];
            this.predecessors = new SimpleLinkedListNode<int>[size];
        }

        public override void AddEdge( int predecessor, int successor )
        {
            SimpleLinkedListNode<int>.Insert( ref this.successors[predecessor], successor );
            SimpleLinkedListNode<int>.Insert( ref this.predecessors[successor], predecessor );
        }

        public override void RemoveEdge( int predecessor, int successor )
        {
            SimpleLinkedListNode<int>.Remove( ref this.successors[predecessor], successor );
            SimpleLinkedListNode<int>.Remove( ref this.predecessors[successor], predecessor );
        }

        public override bool HasEdge(int predecessor, int successor)
        {
            SimpleLinkedListNode<int> current = this.successors[predecessor];

            while ( current != null )
            {
                if ( current.Value == successor )
                    return true;

                current = current.Next;
            }

            return false;
        }

        public override int DoBreadthFirstSearch( int initialNode, int[] distances, int[] directPredecessors )
        {
            int n = distances.Length;

            distances[initialNode] = 0;

            Queue<NodeInfo> queue = new Queue<NodeInfo>( n );
            queue.Enqueue( new NodeInfo
                               {Node = initialNode, NodesInPath = new SimpleLinkedListNode<int>( initialNode, null )} );


            while ( queue.Count > 0 )
            {
                NodeInfo nodeInfo = queue.Dequeue();
                int current = nodeInfo.Node;
                SimpleLinkedListNode<int> successorNode = this.successors[current];
                int currentDistance = distances[current];

                while ( successorNode != null )
                {
                    int successor = successorNode.Value;
                    int successorDistance = distances[successor];
                    int newSucccessorDistance = currentDistance + 1;

                    // Check that the new node is not already in the path.
                    bool hasCycle = false;
                    SimpleLinkedListNode<int> nodeInPathCursor = nodeInfo.NodesInPath;
                    while ( nodeInPathCursor != null )
                    {
                        if ( nodeInPathCursor.Value == successor )
                        {
                            hasCycle = true;
                            break;
                        }

                        nodeInPathCursor = nodeInPathCursor.Next;
                    }

                    if ( hasCycle )
                    {
                        // We just discovered that the successor is part of a cycle.
                        distances[successor] = Cycle;
                        directPredecessors[successor] = current;
                        return successor;
                    }
                    else if ( successorDistance == NotDiscovered || successorDistance < newSucccessorDistance )
                    {
                        distances[successor] = newSucccessorDistance;
                        directPredecessors[successor] = current;

                        queue.Enqueue( new NodeInfo
                                           {
                                               Node = successor,
                                               NodesInPath =
                                                   new SimpleLinkedListNode<int>( successor, nodeInfo.NodesInPath )
                                           } );
                    }
                    else if ( successorDistance == Cycle )
                    {
                        // We have already discovered that the successor is part of a cycle.
                    }

                    successorNode = successorNode.Next;
                }
            }

            return -1;
        }

        private struct NodeInfo
        {
            public int Node;
            public SimpleLinkedListNode<int> NodesInPath;
        }
    }
}
