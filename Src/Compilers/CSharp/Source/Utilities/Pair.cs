using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis.CSharp
{
    // Here we have a straightforward immutable pair struct type; we want to avoid the collection pressure
    // caused by allocating a tuple.

    internal static class Pair
    {
        public static Pair<T1, T2> Make<T1, T2>(T1 t1, T2 t2)
        {
            return new Pair<T1, T2>(t1, t2);
        }
    }

    internal struct Pair<T1, T2>
    {
        public T1 Item1 { get; private set; }
        public T2 Item2 { get; private set; }
        public Pair(T1 item1, T2 item2)
            : this()
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }
    }
}
