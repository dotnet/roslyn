using System.Globalization;
using System.Text;

namespace RoslynEx.Graphs
{
    /// <summary>
    /// Implements algorithms for graphs.
    /// </summary>
    internal abstract class AbstractGraph
    {
        public const int NotDiscovered = int.MaxValue;
        public const int Cycle = int.MinValue;

        private readonly int size;

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
    }
}
