using System;
using System.Globalization;
using System.Text;

namespace RoslynEx.Graphs
{
    /// <summary>
    /// Implements efficient algorithms for graphs of maximum 32 nodes.
    /// </summary>
    internal abstract class AbstractGraph
    {
        public const int NotDiscovered = int.MaxValue;
        public const int Cycle = int.MinValue;

        private readonly int size;


        public static AbstractGraph CreateGraph(int size)
        {
            if (size <= 32)
            {
                return new GraphInt32(size);
            }
            else if (size <= 64)
            {
                return new GraphInt64(size);
            }
            else
            {
                return new Graph(size);
            }
        }

        protected AbstractGraph(int size)
        {
            this.size = size;
        }

        public int[] GetInitialVector()
        {
            int n = this.size;
            int[] vector = new int[n];
            for (int i = 0; i < n; i++)
            {
                vector[i] = NotDiscovered;
            }

            return vector;
        }


        public abstract void AddEdge(int predecessor, int successor);

        public abstract void RemoveEdge(int predecessor, int successor);

        public abstract bool HasEdge(int predecessor, int successor);

        public abstract int DoBreadthFirstSearch(int initialNode, int[] distances, int[] directPredecessors);

        public string Serialize()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0};", this.size);

            for (int i = 0; i < this.size; i++)
            {
                for (int j = 0; j < this.size; j++)
                {
                    if (i == j)
                        continue;

                    if (this.HasEdge(i, j))
                        stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0},{1};", i, j);
                }
            }

            return stringBuilder.ToString();
        }

        public static AbstractGraph Deserialize(string data)
        {
            string[] elements = data.Split(';');
            int size = int.Parse(elements[0], CultureInfo.InvariantCulture);

            AbstractGraph graph = CreateGraph(size);

            for (int i = 1; i < elements.Length; i++)
            {
                if (string.IsNullOrEmpty(elements[i]))
                    continue;

                string[] edge = elements[i].Split(',');
                int predecessor = int.Parse(edge[0], CultureInfo.InvariantCulture);
                int sucessor = int.Parse(edge[1], CultureInfo.InvariantCulture);

                graph.AddEdge(predecessor, sucessor);
            }

            return graph;
        }
    }
}
