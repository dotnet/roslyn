
namespace Microsoft.CodeAnalysis
{
    partial struct ReadOnlyArray<T>
    {
        // foreach Enumerator
        public struct Enumerator
        {
            private int i;
            private readonly T[] array;

            internal Enumerator(T[] array)
            {
                this.i = -1;
                this.array = array;
            }

            public bool MoveNext()
            {
                i++;
                return i < array.Length;
            }

            public T Current
            {
                get
                {
                    return array[i];
                }
            }
        }
    }
}