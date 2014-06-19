using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    partial class ArrayBuilder<T>
    {
        private sealed class Comparer : IComparer<ArrayElement<T>>
        {
            private readonly IComparer<T> comparer;

            public Comparer()
            {
                this.comparer = Comparer<T>.Default;
            }

            public Comparer(IComparer<T> comparer)
            {
                this.comparer = comparer;
            }

            public int Compare(ArrayElement<T> x, ArrayElement<T> y)
            {
                return comparer.Compare(x.Value, y.Value);
            }
        }
    }
}