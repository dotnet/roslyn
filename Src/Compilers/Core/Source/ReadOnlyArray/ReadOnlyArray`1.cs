using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A wrapper that prevents writing to elements of an underlying array. It is a struct
    /// to avoid extra allocation and indirections. Equality is determined by reference equality
    /// of the underlying array. Implements a struct enumerator to minimize allocations. For similar
    /// reasons, it does not implement IEnumerable, but instead implements a subset of the Linq extensions.
    /// 
    /// This type is expected to be replaced by a BCL type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public partial struct ReadOnlyArray<T> : ISerializable,
        IEquatable<ReadOnlyArray<T>>     // because it is a struct. We only do reference equality.
    {
        // not readonly to implement Interlocked.
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        private T[] elements;


        /// <summary>
        /// A singleton representing an empty array.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly static ReadOnlyArray<T> Empty = new ReadOnlyArray<T>(new T[] { });

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly static ReadOnlyArray<T> Null = default(ReadOnlyArray<T>);
        
        private ReadOnlyArray(T[] items)
        {
            this.elements = items;
        }

        private ReadOnlyArray(SerializationInfo info, StreamingContext context)
        {
            this.elements = (T[])info.GetValue("elements", typeof(T[]));
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("elements", elements, typeof(T[]));
        }

        public static ReadOnlyArray<T> CreateFrom(T item)
        {
            return new ReadOnlyArray<T>(new[] { item });
        }

        public static ReadOnlyArray<T> CreateFrom(T item1, T item2)
        {
            return new ReadOnlyArray<T>(new[] { item1, item2 });
        }

        public static ReadOnlyArray<T> CreateFrom(T item1, T item2, T item3)
        {
            return new ReadOnlyArray<T>(new[] { item1, item2, item3 });
        }

        public static ReadOnlyArray<T> CreateFrom(T item1, T item2, T item3, T item4)
        {
            return new ReadOnlyArray<T>(new[] { item1, item2, item3, item4 });
        }

        public static ReadOnlyArray<T> CreateFrom(params T[] items)
        {
            if (items == null)
            {
                return Null;
            }

            if (items.Length == 0)
            {
                return Empty;
            }

            var tmp = new T[items.Length];
            Array.Copy(items, tmp, items.Length);

            return new ReadOnlyArray<T>(tmp);
        }

        public static ReadOnlyArray<T> CreateFrom<U>(IList<U> items) where U : T
        {
            if (items == null)
            {
                return Null;
            }

            if (items.Count == 0)
            {
                return Empty;
            }

            var tmp = new T[items.Count];
            for (int i = 0; i < tmp.Length; i++)
            {
                tmp[i] = items[i];
            }

            return new ReadOnlyArray<T>(tmp);
        }

        public static ReadOnlyArray<T> CreateFrom<U>(IEnumerable<U> items) where U : T
        {
            if (items == null)
            {
                return Null;
            }

            var builder = new Builder();
            foreach (T t in items)
            {
                builder.Add(t);
            }

            return builder.ToImmutable();
        }

        // operators cannot be generic...
        public static ReadOnlyArray<T> CreateFrom<U>(ReadOnlyArray<U> other) where U : class, T
        {
            return new ReadOnlyArray<T>(other.elements);
        }

        /// <summary>
        /// Downcast the array to ReadOnlyArray of derived type without doing additional allocation.
        /// </summary>
        internal ReadOnlyArray<U> DownCast<U>() where U : class, T
        {
            return new ReadOnlyArray<U>((U[])this.elements);
        }

        private object GetDebuggerDisplay()
        {
            return (elements != null) ? "Count = " + Count : null;
        }

        public T this[int i]
        {
            get
            {
                return elements[i];
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int Count
        {
            get
            {
                return elements.Length;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsNull
        {
            get
            {
                return elements == null;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsNotNull
        {
            get
            {
                return elements != null;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsNullOrEmpty
        {
            get
            {
                var e = elements;
                return e == null || e.Length == 0;
            }
        }

        public static bool operator ==(ReadOnlyArray<T> left, ReadOnlyArray<T> right)
        {
            return left.elements == right.elements;
        }

        public static bool operator !=(ReadOnlyArray<T> left, ReadOnlyArray<T> right)
        {
            return left.elements != right.elements;
        }

        public static bool operator ==(ReadOnlyArray<T>? left, ReadOnlyArray<T>? right)
        {
            return left.GetValueOrDefault().elements == right.GetValueOrDefault().elements;
        }

        public static bool operator !=(ReadOnlyArray<T>? left, ReadOnlyArray<T>? right)
        {
            return left.GetValueOrDefault().elements != right.GetValueOrDefault().elements;
        }

        public override int GetHashCode()
        {
            return elements == null ? 0 : elements.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyArray<T>)
            {
                return this.Equals((ReadOnlyArray<T>)obj);
            }

            return false;
        }

        public bool Equals(ReadOnlyArray<T> other)
        {
            return this == other;
        }

        internal bool Equals<S>(ReadOnlyArray<S> other)
        {
            return (object)this.elements == (object)other.elements;
        }

        public int IndexOf(T item)
        {
            return Array.IndexOf(elements, item);
        }

        public int IndexOf(T item, IEqualityComparer<T> comparer)
        {
            var array = this.elements;
            for (var i = 0; i < array.Length; i++)
            {
                if (comparer.Equals(array[i], item))
                {
                    return i;
                }
            }

            return -1;
        }

        public void CopyTo(T[] destination)
        {
            Array.Copy(elements, 0, destination, 0, Count);
        }

        public void CopyTo(T[] destination, int index)
        {
            Array.Copy(elements, 0, destination, index, Count);
        }

        public void CopyTo(int sourceIndex, T[] destination, int destinationIndex, int length)
        {
            Array.Copy(elements, sourceIndex, destination, destinationIndex, length);
        }

        public ReadOnlyArray<T> RemoveFirst()
        {
            if (Count == 0)
            {
                throw new InvalidOperationException();
            }

            if (Count == 1)
            {
                return ReadOnlyArray<T>.Empty;
            }

            T[] result = new T[Count - 1];
            CopyTo(1, result, 0, result.Length);
            return new ReadOnlyArray<T>(result);
        }

        public ReadOnlyArray<T> Append(T item)
        {
            if (Count == 0)
            {
                return CreateFrom(item);
            }

            int oldCount = Count;
            var tmp = new T[oldCount + 1];
            Array.Copy(this.elements, tmp, oldCount);
            tmp[oldCount] = item;

            return new ReadOnlyArray<T>(tmp);
        }

        public ReadOnlyArray<T> Concat(ReadOnlyArray<T> another)
        {
            if (Count == 0)
            {
                return another;
            }

            if (another.Count == 0)
            {
                return this;
            }

            var tmp = new T[Count + another.Count];
            Array.Copy(this.elements, tmp, Count);

            var anotherArr = another.elements;
            Array.Copy(anotherArr, 0, tmp, Count, anotherArr.Length);

            return new ReadOnlyArray<T>(tmp);
        }

        public bool Contains(T item)
        {
            var array = this.elements;
            if (item == null)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    if (array[j] == null)
                    {
                        return true;
                    }
                }
                return false;
            }
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < array.Length; i++)
            {
                if (comparer.Equals(array[i], item))
                {
                    return true;
                }
            }
            return false;
        }

        public bool Contains(T item, IEqualityComparer<T> comparer)
        {
            return this.IndexOf(item, comparer) >= 0;
        }

        public IEnumerable<T> AsEnumerable()
        {
            return this.AsList();
        }

        public IList<T> AsList()
        {
            if (this.Count == 0)
            {
                return Empty.elements;
            }

            return new ReadOnlyList(this);
        }

        public IEnumerable<T> AsReverseEnumerable()
        {
            int n = this.elements.Length;
            for (int i = n - 1; i >= 0; i--)
                yield return elements[i];
        }

        public bool Any()
        {
            return this.Count > 0;
        }

        public bool Any(Func<T, bool> predicate)
        {
            foreach (var v in this.elements)
            {
                if (predicate(v))
                {
                    return true;
                }
            }

            return false;
        }

        public bool All(Func<T, bool> predicate)
        {
            foreach (var v in this.elements)
            {
                if (!predicate(v))
                {
                    return false;
                }
            }

            return true;
        }

        public bool SequenceEqual<U>(ReadOnlyArray<U> items, IEqualityComparer<T> comparer = null) where U : T
        {
            if (this.Count != items.Count)
            {
                return false;
            }

            if (comparer == null)
            {
                comparer = EqualityComparer<T>.Default;
            }

            for (int i = 0; i < elements.Length; i++)
            {
                if (!comparer.Equals(elements[i], items.elements[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public bool SequenceEqual<U>(IEnumerable<U> items, IEqualityComparer<T> comparer = null) where U : T
        {
            if (comparer == null)
            {
                comparer = EqualityComparer<T>.Default;
            }

            int i = 0;
            int n = this.Count;
            foreach (var item in items) 
            {
                if (i == n)
                {
                    return false;
                }

                if (!comparer.Equals(this[i], item))
                {
                    return false;
                }

                i++;
            }

            return i == n;
        }

        public bool SequenceEqual<U>(ReadOnlyArray<U> items, Func<T, T, bool> predicate) where U : T
        {
            if (this.Count != items.Count)
            {
                return false;
            }

            for (int i = 0, n = this.Count; i < n; i++)
            {
                if (!predicate(this[i], items[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public T Aggregate(Func<T, T, T> func)
        {
            if (this.Count == 0)
            {
                return default(T);
            }

            var result = this[0];
            for (int i = 1, n = this.Count; i < n; i++)
            {
                result = func(result, this[i]);
            }

            return result;
        }

        public TAccumulate Aggregate<TAccumulate>(TAccumulate seed, Func<TAccumulate, T, TAccumulate> func)
        {
            var result = seed;
            foreach (var v in this.elements)
            {
                result = func(result, v);
            }

            return result;
        }

        public TResult Aggregate<TAccumulate, TResult>(TAccumulate seed, Func<TAccumulate, T, TAccumulate> func, Func<TAccumulate, TResult> resultSelector)
        {
            return resultSelector(this.Aggregate(seed, func));
        }

        public T ElementAt(int index)
        {
            return this[index];
        }

        public T ElementAtOrDefault(int index)
        {
            if (index < 0 || index >= this.Count)
            {
                return default(T);
            }

            return this[index];
        }

        public T First(Func<T, bool> predicate)
        {
            foreach (var v in this.elements)
            {
                if (predicate(v))
                {
                    return v;
                }
            }

            throw new InvalidOperationException();
        }

        public T First()
        {
            return elements[0];
        }

        public T FirstOrDefault()
        {
            if (this.Count == 0)
            {
                return default(T);
            }

            return this.First();
        }

        public T FirstOrDefault(Func<T, bool> predicate)
        {
            foreach (var v in this.elements)
            {
                if (predicate(v))
                {
                    return v;
                }
            }

            return default(T);
        }

        public T Last()
        {
            if (this.Count > 0)
            {
                return elements[this.Count - 1];
            }

            throw new InvalidOperationException();
        }

        public T Last(Func<T, bool> predicate)
        {
            for (int i = this.Count - 1; i >= 0; i--)
            {
                if (predicate(this[i]))
                {
                    return this[i];
                }
            }

            throw new InvalidOperationException();
        }

        public T LastOrDefault()
        {
            if (this.Count == 0)
            {
                return default(T);
            }

            return this.Last();
        }

        public T LastOrDefault(Func<T, bool> predicate)
        {
            for (int i = this.Count - 1; i >= 0; i--)
            {
                if (predicate(this[i]))
                {
                    return this[i];
                }
            }

            return default(T);
        }

        public T Single()
        {
            switch (this.Count)
            {
                case 0:
                    throw new InvalidOperationException("No elements");

                case 1:
                    return this[0];

                default:
                    throw new InvalidOperationException("More than one element");
            }
        }

        public T Single(Func<T, bool> predicate)
        {
            var first = true;
            var result = default(T);
            foreach (var v in this.elements)
            {
                if (predicate(v))
                {
                    if (!first)
                    {
                        throw new InvalidOperationException();
                    }

                    first = false;
                    result = v;
                }
            }

            if (first)
            {
                throw new InvalidOperationException();
            }

            return result;
        }

        public T SingleOrDefault()
        {
            switch (this.Count)
            {
                case 0:
                    return default(T);

                case 1:
                    return this[0];

                default:
                    throw new InvalidOperationException("More than one element");
            }
        }

        public T SingleOrDefault(Func<T, bool> predicate)
        {
            var first = true;
            var result = default(T);
            foreach (var v in this.elements)
            {
                if (predicate(v))
                {
                    if (!first)
                    {
                        throw new InvalidOperationException();
                    }

                    first = false;
                    result = v;
                }
            }

            return result;
        }

        public bool HasDuplicates(IEqualityComparer<T> comparer)
        {
            switch (elements.Length)
            {
                case 0:
                case 1:
                    return false;
                case 2:
                    return comparer.Equals(elements[0], elements[1]);
                default:
                    var set = new HashSet<T>(comparer);
                    foreach (var i in elements)
                    {
                        if (!set.Add(i)) return true;
                    }

                    return false;
            }
        }

        public Dictionary<TKey, T> ToDictionary<TKey>(Func<T, TKey> keySelector)
        {
            return ToDictionary(keySelector, EqualityComparer<TKey>.Default);
        }

        public Dictionary<TKey, TElement> ToDictionary<TKey, TElement>(Func<T, TKey> keySelector, Func<T, TElement> elementSelector)
        {
            return ToDictionary(keySelector, elementSelector, EqualityComparer<TKey>.Default);
        }

        public Dictionary<TKey, T> ToDictionary<TKey>(Func<T, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            var result = new Dictionary<TKey, T>(comparer);
            foreach (var v in this)
            {
                result.Add(keySelector(v), v);
            }

            return result;
        }

        public Dictionary<TKey, TElement> ToDictionary<TKey, TElement>(Func<T, TKey> keySelector, Func<T, TElement> elementSelector, IEqualityComparer<TKey> comparer)
        {
            var result = new Dictionary<TKey, TElement>(comparer);
            foreach (var v in this.elements)
            {
                result.Add(keySelector(v), elementSelector(v));
            }

            return result;
        }

        public List<T> ToList()
        {
            var result = new List<T>(this.Count);
            for (int i = 0; i < this.Count; i++)
            {
                result.Add(this[i]);
            }
            return result;
        }

        public T[] ToArray()
        {
            return (T[])elements.Clone();
        }

        public override string ToString()
        {
            if (elements == null)
            {
                return null;
            }

            var b = new StringBuilder();

            b.Append("{");
            if (Count > 0)
            {
                b.Append(this[0]);
                for (int i = 1; i < Count; i++)
                {
                    b.Append(", ");
                    b.Append(this[i]);
                }
            }
            b.Append("}");

            return b.ToString();
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this.elements);
        }

        // TODO: Is this too inviting to Linq? if we keep this, perhaps make a custom iterator.
        public IEnumerable<TOther> OfType<TOther>()
            where TOther : class
        {
            for(int i = 0; i < this.elements.Length; i++)
            {
                var t = this.elements[i] as TOther;
                if (t != null)
                {
                    yield return t;
                }
            }
        }

        internal static ReadOnlyArray<T> InterlockedExchange(ref ReadOnlyArray<T> location, ReadOnlyArray<T> value)
        {
            return new ReadOnlyArray<T>(Interlocked.Exchange(ref location.elements, value.elements));
        }

        internal static ReadOnlyArray<T> InterlockedCompareExchange(ref ReadOnlyArray<T> location, ReadOnlyArray<T> value, ReadOnlyArray<T> comparand)
        {
            return new ReadOnlyArray<T>(Interlocked.CompareExchange(ref location.elements, value.elements, comparand.elements));
        }

        /// <summary>
        /// Replace value at location with given value if the current value is
        /// ReadOnlyArray&lt;T&gt;.Null. Return true if value was replaced.
        /// </summary>
        internal static bool InterlockedCompareExchangeIfNull(ref ReadOnlyArray<T> location, ReadOnlyArray<T> value)
        {
            var result = Interlocked.CompareExchange(ref location.elements, value.elements, null);
            return (result == null);
        }

        internal static byte[] GetSha1Hash(ReadOnlyArray<byte> x)
        {
            return new SHA1Managed().ComputeHash(x.elements);
        }

        /// <summary>
        /// Pins the underlying array and returns a <see cref="GCHandle"/> of the pinned memory.
        /// </summary>
        [Obsolete("Only to support CCI and PEVerify which we trust to not modify the underlying memory, do not use elsewhere.", error: false)]
        internal GCHandle GetPinnedHandle()
        {
            return GCHandle.Alloc(elements, GCHandleType.Pinned);
        }

        internal static void WriteToFileInternal(ReadOnlyArray<byte> array, string path)
        {
            File.WriteAllBytes(path, array.elements);
        }

        internal static System.Reflection.Assembly LoadAsAssemblyInternal(ReadOnlyArray<byte> rawAssembly, bool reflectionOnly)
        {
            if (reflectionOnly)
            {
                return System.Reflection.Assembly.ReflectionOnlyLoad(rawAssembly.elements);
            }
            else
            {
                return System.Reflection.Assembly.Load(rawAssembly.elements);
            }
        }

        internal static System.Reflection.Module LoadModuleInternal(System.Reflection.Assembly assembly, string moduleName, ReadOnlyArray<byte> rawModule, ReadOnlyArray<byte> rawSymbolStore)
        {
            return assembly.LoadModule(moduleName, rawModule.elements, rawSymbolStore.IsNull ? null : rawSymbolStore.elements);
        }
    }
}
