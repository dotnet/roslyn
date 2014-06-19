using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    partial struct ReadOnlyArray<T>
    {
        /// <summary>
        /// IList Wrapper on top of ReadOnlyArray
        /// </summary>
        private class ReadOnlyList : IList<T>
        {
            public readonly ReadOnlyArray<T> array;

            public ReadOnlyList(ReadOnlyArray<T> array)
            {
                this.array = array;
            }

            [Obsolete("This method is currently unused and excluded from code coverage. Should you decide to use it, please add a test.", true)]
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]  //used to hide from code coverage tools.
            public int IndexOf(T item)
            {
                return this.array.IndexOf(item);
            }

            [Obsolete("This method is currently unused and excluded from code coverage. Should you decide to use it, please add a test.", true)]
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]  //used to hide from code coverage tools.
            public void Insert(int index, T item)
            {
                throw new InvalidOperationException("array is readonly");
            }

            [Obsolete("This method is currently unused and excluded from code coverage. Should you decide to use it, please add a test.", true)]
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]  //used to hide from code coverage tools.
            public void RemoveAt(int index)
            {
                throw new InvalidOperationException("array is readonly");
            }

            public T this[int index]
            {
                get
                {
                    return array[index];
                }
                set
                {
                    throw new InvalidOperationException("array is readonly");
                }
            }

            [Obsolete("This method is currently unused and excluded from code coverage. Should you decide to use it, please add a test.", true)]
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]  //used to hide from code coverage tools.
            public void Add(T item)
            {
                throw new InvalidOperationException("array is readonly");
            }

            [Obsolete("This method is currently unused and excluded from code coverage. Should you decide to use it, please add a test.", true)]
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]  //used to hide from code coverage tools.
            public void Clear()
            {
                throw new InvalidOperationException("array is readonly");
            }

            [Obsolete("This method is currently unused and excluded from code coverage. Should you decide to use it, please add a test.", true)]
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]  //used to hide from code coverage tools.
            public bool Contains(T item)
            {
                return this.array.Contains(item);
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                this.array.CopyTo(0, array, arrayIndex, this.Count);
            }

            public int Count
            {
                get { return this.array.Count; }
            }

            [Obsolete("This method is currently unused and excluded from code coverage. Should you decide to use it, please add a test.", true)]
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]  //used to hide from code coverage tools.
            public bool IsReadOnly
            {
                get { return true; }
            }

            [Obsolete("This method is currently unused and excluded from code coverage. Should you decide to use it, please add a test.", true)]
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]  //used to hide from code coverage tools.
            public bool Remove(T item)
            {
                throw new InvalidOperationException("array is readonly");
            }

            public IEnumerator<T> GetEnumerator()
            {
                for (int i = 0; i < this.array.Count; i++)
                {
                    yield return this.array[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }
    }
}