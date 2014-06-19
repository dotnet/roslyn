using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    partial struct ReadOnlyArray<T>
    {
        public class Builder
        {
            [Serializable]
            [DebuggerDisplay("{Value,nq}")]
            internal struct ArrayElement
            {
                internal T Value;

                public static implicit operator T(ArrayElement element)
                {
                    return element.Value;
                }
            }

            private ArrayElement[] elements;
            private int count;

            public Builder(int size)
            {
                this.elements = new ArrayElement[size];
                this.count = 0;
            }

            public Builder() :
                this(8)
            {
            }

            public int Count
            {
                get
                {
                    return count;
                }
            }

            public void Clear()
            {
                if (count != 0)
                {
                    var e = this.elements;

                    //PERF: Array.Clear works well for big arrays, 
                    //      but may have too much overhead with small ones (which is the common case here)
                    if (count < 64)
                    {
                        for (int i = 0; i < e.Length; i++)
                        {
                            if (i >= count)
                            {
                                break;
                            }

                            e[i].Value = default(T);
                        }
                    }
                    else
                    {
                        Array.Clear(e, 0, count);
                    }

                    this.count = 0;
                }
            }

            public T this[int index]
            {
                get
                {
                    if ((uint)index < (uint)count)
                    {
                        return this.elements[index].Value;
                    }

                    throw new IndexOutOfRangeException();
                }
                set
                {
                    if ((uint)index < (uint)count)
                    {
                        this.elements[index].Value = value;
                        return;
                    }

                    throw new IndexOutOfRangeException();
                }
            }

            public ReadOnlyArray<T> ToImmutable()
            {
                if (count == 0)
                {
                    return ReadOnlyArray<T>.Empty;
                }

                var tmp = ToArray();
                return new ReadOnlyArray<T>(tmp);
            }

            public T[] ToArray()
            {
                var tmp = new T[count];
                var elements = this.elements;
                for (int i = 0; i < tmp.Length; i++)
                {
                    tmp[i] = elements[i].Value;
                }
                return tmp;
            }

            public void CopyTo(T[] array, int start)
            {
                var elements = this.elements;
                for (int i = 0, n = this.count; i < n; i++)
                {
                    array[start + i] = elements[i].Value;
                }
            }

            public void EnsureCapacity(int capacity)
            {
                if (elements.Length >= capacity)
                {
                    return;
                }

                this.DoEnsureCapacity(capacity);
            }

            private void DoEnsureCapacity(int capacity)
            {
                int newCapacity = Math.Max(elements.Length * 2, capacity);
                Array.Resize(ref elements, newCapacity);
            }

            public void Add(T item)
            {
                EnsureCapacity(count + 1);
                elements[count++].Value = item;
            }

            public void InsertAt(int index, T item)
            {
                if (index < 0 || index > count)
                {
                    throw new ArgumentOutOfRangeException();
                }

                EnsureCapacity(count + 1);

                if (index < count)
                {
                    Array.Copy(elements, index, elements, index + 1, count - index);
                }

                count++;
                elements[index].Value = item;
            }

            public void RemoveAt(int index)
            {
                if (index < 0 || index >= count)
                {
                    throw new ArgumentOutOfRangeException();
                }

                if (index < count - 1)
                {
                    Array.Copy(elements, index + 1, elements, index, count - index - 1);
                }

                RemoveLast();
            }

            public void RemoveLast()
            {
                elements[count - 1] = default(ArrayElement);
                count--;
            }

            public void AddRange(IEnumerable<T> items)
            {
                foreach (var item in items)
                {
                    Add(item);
                }
            }

            public void AddRange(params T[] items)
            {
                EnsureCapacity(count + items.Length);

                var offset = count;
                count += items.Length;

                var nodes = this.elements;
                for (int i = 0; i < items.Length; i++)
                {
                    nodes[offset + i].Value = items[i];
                }
            }

            public void AddRange<U>(U[] items) where U: T
            {
                EnsureCapacity(count + items.Length);

                var offset = count;
                count += items.Length;

                var nodes = this.elements;
                for (int i = 0; i < items.Length; i++)
                {
                    nodes[offset + i].Value = items[i];
                }
            }

            public void AddRange(T[] items, int length)
            {
                EnsureCapacity(count + length);

                var offset = count;
                count += length;

                var nodes = this.elements;
                for (int i = 0; i < length; i++)
                {
                    nodes[offset + i].Value = items[i];
                }
            }

            public void AddRange(ReadOnlyArray<T> items)
            {
                AddRange(items.elements);
            }

            public void AddRange(ReadOnlyArray<T> items, int length)
            {
                AddRange(items.elements, length);
            }

            public void AddRange<U>(ReadOnlyArray<U> items) where U: T
            {
                AddRange(items.elements);
            }

            private void AddRange(ArrayElement[] items, int length)
            {
                EnsureCapacity(count + length);

                var offset = count;
                count += length;

                var nodes = this.elements;
                for (int i = 0; i < length; i++)
                {
                    nodes[offset + i].Value = items[i].Value;
                }
            }

            public void AddRange(Builder items)
            {
                AddRange(items.elements, items.count);
            }

            private void AddRange<U>(ReadOnlyArray<U>.Builder.ArrayElement[] items, int length) where U: T
            {
                EnsureCapacity(count + length);

                var offset = count;
                count += length;

                var nodes = this.elements;
                for (int i = 0; i < length; i++)
                {
                    nodes[offset + i].Value = items[i].Value;
                }
            }

            public void AddRange<U>(ReadOnlyArray<U>.Builder items) where U : T
            {
                AddRange(items.elements, items.count);
            }

            public void UnionWith(Builder items)
            {
                for (int i = 0, n = items.Count; i < n; i++)
                {
                    var item = items.elements[i].Value;
                    if (!this.Contains(item))
                    {
                        this.Add(item);
                    }
                }
            }

            public bool Contains(T item)
            {
                var comparer = EqualityComparer<T>.Default;

                for (int i = 0, n = this.count; i < n; i++)
                {
                    if (comparer.Equals(item, this.elements[i].Value))
                    {
                        return true;
                    }
                }

                return false;
            }

            public void ReverseContents()
            {
                int end = count - 1;
                for (int i = 0, j = end; i < j; i++, j--)
                {
                    var tmp = elements[i].Value;
                    elements[i] = elements[j];
                    elements[j].Value = tmp;
                }
            }

            public T First()
            {
                return this[0];
            }

            public T FirstOrDefault()
            {
                return Any() ? this[0] : default(T);
            }

            public T Last()
            {
                return this[count - 1];
            }

            public bool Any()
            {
                return count > 0;
            }

            public void Sort(IComparer<T> comparer)
            {
                Array.Sort(this.elements, 0, count, new Comparer(comparer));
            }

            public void Sort(int startIndex, IComparer<T> comparer)
            {
                Array.Sort(this.elements, startIndex, count - startIndex, new Comparer(comparer));
            }

            public void Sort()
            {
                Array.Sort(this.elements, 0, count, new Comparer());
            }

            #region DebuggerProxy

            private sealed class DebuggerProxy
            {
                private readonly Builder builder;

                public DebuggerProxy(Builder builder)
                {
                    this.builder = builder;
                }

                [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
                public T[] A
                {
                    get
                    {
                        var result = new T[builder.count];
                        for (int i = 0; i < result.Length; i++)
                        {
                            result[i] = builder.elements[i].Value;
                        }

                        return result;
                    }
                }
            }

            #endregion

            private sealed class Comparer : IComparer<ArrayElement>
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

                public int Compare(ArrayElement x, ArrayElement y)
                {
                    return comparer.Compare(x.Value, y.Value);
                }
            }
        }
    }
}