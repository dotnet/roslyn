using System.Collections.Generic;

namespace RoslynEx.Graphs
{
    internal sealed class GraphInt64 : AbstractGraph
    {
        private static readonly long[] bits;
        private readonly long[] successorMatrix;

#pragma warning disable CA1810 // Initialize reference type static fields inline
        static GraphInt64()
#pragma warning restore CA1810 // Initialize reference type static fields inline
        {
            bits = new long[64];
            long bit = 1;
            for ( int i = 0; i < 64; i++ )
            {
                bits[i] = bit;
                bit = bit << 1;
            }
        }

        public GraphInt64( int size ) : base( size )
        {
            this.successorMatrix = new long[size];
        }

        public override void AddEdge( int predecessor, int successor )
        {
            this.successorMatrix[predecessor] = this.successorMatrix[predecessor] | bits[successor];
        }

        public override void RemoveEdge( int predecessor, int successor )
        {
            this.successorMatrix[predecessor] = this.successorMatrix[predecessor] & ~bits[successor];
        }

        public override bool HasEdge(int predecessor, int successor)
        {
            return (this.successorMatrix[predecessor] & bits[successor]) != 0;
        }

        public override int DoBreadthFirstSearch( int initialNode, int[] distances, int[] directPredecessors )
        {
            long[] m = this.successorMatrix;
            int n = m.Length;

            long[] allPredecessors = new long[n];

            distances[initialNode] = 0;

            Queue<int> queue = new Queue<int>( n );
            queue.Enqueue( initialNode );

            while ( queue.Count > 0 )
            {
                int current = queue.Dequeue();
                long currentSuccessors = m[current];

                if ( currentSuccessors == 0 ) continue;

                int currentDistance = distances[current];
                long currentPredecessors = allPredecessors[current];

                for ( int successor = 0; successor < n; successor++ )
                {
                    if ( (currentSuccessors & 1) != 0 )
                    {
                        int successorDistance = distances[successor];
                        int newSucccessorDistance = currentDistance + 1;

                        if ( (currentPredecessors & 1) != 0 )
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
                            allPredecessors[successor] |= bits[current] | allPredecessors[current];
                            queue.Enqueue( successor );
                        }
                        else if ( successorDistance == Cycle )
                        {
                            // We have already discovered that the successor is part of a cycle.
                        }
                    }

                    currentPredecessors = currentPredecessors >> 1;
                    currentSuccessors = currentSuccessors >> 1;

                    if ( currentSuccessors == 0 )
                        break;
                }
            }

            return -1;
        }
    }
}
