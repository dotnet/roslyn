using System;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;

namespace System.Collections.Immutable
{
#if !COMPILERCORE
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    internal abstract class ImmutableList<T> : IList<T>, IReadOnlyList<T>
    {
        protected ImmutableList()
        {
        }

        public bool Contains(T item)
        {
            return this.IndexOf(item) >= 0;
        }

        public ImmutableList<T> Add(T item)
        {
            return this.InsertAt(this.Count, item);
        }

        public ImmutableList<T> AddRange(T[] items)
        {
            return this.InsertAt(this.Count, items);
        }

        public ImmutableList<T> AddRange(IEnumerable<T> items)
        {
            return this.InsertAt(this.Count, items);
        }

        public ImmutableList<T> Prepend(T item)
        {
            return this.InsertAt(0, item);
        }

        public ImmutableList<T> Prepend(params T[] items)
        {
            return this.InsertAt(0, items);
        }

        public ImmutableList<T> Prepend(IEnumerable<T> items)
        {
            return this.InsertAt(0, items);
        }

        public ImmutableList<T> Remove(T item)
        {
            int index = this.IndexOf(item);

            if (index >= 0)
            {
                return this.RemoveAt(index, 1);
            }

            return this;
        }

        public ImmutableList<T> Replace(T oldItem, T newItem)
        {
            int index = this.IndexOf(oldItem);
            if (index < 0)
            {
                throw new ArgumentException("oldItem");
            }

            return this.ReplaceAt(index, 1, newItem);
        }

        public ImmutableList<T> Replace(T oldItem, IEnumerable<T> newItems)
        {
            int index = this.IndexOf(oldItem);
            if (index < 0)
            {
                throw new ArgumentException("oldItem");
            }

            return this.ReplaceAt(index, 1, newItems);
        }

        public ImmutableList<T> RemoveAt(int index)
        {
            return RemoveAt(index, 1);
        }

        public abstract int Count { get; }
        public abstract T this[int index] { get; }
        public abstract ImmutableList<T> InsertAt(int index, T item);
        public abstract ImmutableList<T> InsertAt(int index, params T[] items);
        public abstract ImmutableList<T> InsertAt(int index, IEnumerable<T> items);
        public abstract ImmutableList<T> InsertAt(int index, ImmutableList<T> items);
        public abstract ImmutableList<T> RemoveAt(int index, int length);
        public abstract ImmutableList<T> ReplaceAt(int index, int length, T item);
        public abstract ImmutableList<T> ReplaceAt(int index, int length, params T[] items);
        public abstract ImmutableList<T> ReplaceAt(int index, int length, IEnumerable<T> items);
        public abstract int IndexOf(T item);
        public abstract void CopyTo(int index, T[] array, int arrayOffset, int length);
        public abstract IEnumerator<T> GetEnumerator();

        private static readonly ImmutableList<T> Empty = AbstractList.Empty;

        // Temporary hack to allow access to the creation methods until we can use the BCL version
        internal static ImmutableList<T> PrivateEmpty_DONOTUSE()
        {
            return Empty;
        }

        private abstract class AbstractList : ImmutableList<T>
        {
            public override ImmutableList<T> InsertAt(int index, params T[] items)
            {
                return this.InsertAtInternal(index, items.Copy(0, items.Length));
            }

            public override ImmutableList<T> InsertAt(int index, IEnumerable<T> items)
            {
                return this.InsertAtInternal(index, items.ToArray());
            }

            public override ImmutableList<T> InsertAt(int index, T item)
            {
                return this.InsertAtInternal(index, new T[] { item });
            }

            public override ImmutableList<T> InsertAt(int index, ImmutableList<T> items)
            {
                return this.InsertAtInternal(index, items.ToArray());
            }

            internal AbstractList InsertAtInternal(int index, params T[] items)
            {
                if (index < 0 || index > this.Count)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                if (items.Length == 0)
                {
                    return this;
                }

                if (this.Count == 0)
                {
                    return new ElementList(items);
                }

                AbstractList one;
                AbstractList[] many;
                this.InsertAtInternal(index, items, out one, out many);

                if (one != null)
                {
                    return one;
                }
                else
                {
                    return new CompoundList(many);
                }
            }

            public override ImmutableList<T> RemoveAt(int index, int length)
            {
                return this.RemoveAtInternal(index, length);
            }

            public override ImmutableList<T> ReplaceAt(int index, int length, T item)
            {
                return this.ReplaceAtInternal(index, length, new T[] { item });
            }

            public override ImmutableList<T> ReplaceAt(int index, int length, params T[] items)
            {
                return this.ReplaceAtInternal(index, length, items.Copy(0, items.Length));
            }

            public override ImmutableList<T> ReplaceAt(int index, int length, IEnumerable<T> items)
            {
                return this.ReplaceAtInternal(index, length, items.ToArray());
            }

            private AbstractList ReplaceAtInternal(int index, int length, T[] items)
            {
                return this.RemoveAtInternal(index, length).InsertAtInternal(index, items);
            }

            public override IEnumerator<T> GetEnumerator()
            {
                for (int i = 0; i < this.Count; i++)
                {
                    yield return this[i];
                }
            }

            public abstract void InsertAtInternal(int index, T[] items, out AbstractList one, out AbstractList[] many);
            public abstract AbstractList RemoveAtInternal(int index, int length);

            protected const int IdealElementCount = 32;
            protected const int IdealSlotCount = 32;

            internal static new readonly AbstractList Empty = new ElementList(new T[] { });
        }

        /// <summary>
        /// This is a simple list of elements
        /// </summary>
        private class ElementList : AbstractList
        {
            private readonly T[] items;

            internal ElementList(params T[] items)
            {
                this.items = items;
            }

            public override int Count
            {
                get { return this.items.Length; }
            }

            public override T this[int index]
            {
                get { return this.items[index]; }
            }

            public override int IndexOf(T item)
            {
                var comparer = EqualityComparer<T>.Default;

                for (int i = 0; i < this.items.Length; i++)
                {
                    if (comparer.Equals(item, this.items[i]))
                    {
                        return i;
                    }
                }

                return -1;
            }

            public override void InsertAtInternal(int index, T[] items, out AbstractList one, out AbstractList[] many)
            {
                if (this.items.Length + items.Length <= IdealElementCount)
                {
                    one = new ElementList(this.items.InsertAt(index, items));
                    many = null;
                }
                else if (index == 0)
                {
                    one = null;
                    many = new AbstractList[] { new ElementList(items), this };
                }
                else if (index == this.items.Length)
                {
                    one = null;
                    many = new AbstractList[] { this, new ElementList(items) };
                }
                else
                {
                    var before = this.items.RemoveAt(index, this.items.Length - index);
                    var after = this.items.RemoveAt(0, index);

                    one = null;
                    if (before.Length + items.Length <= IdealElementCount)
                    {
                        many = new AbstractList[] { new ElementList(before.Append(items)), new ElementList(after) };
                    }
                    else if (after.Length + items.Length <= IdealElementCount)
                    {
                        many = new AbstractList[] { new ElementList(before), new ElementList(items.Append(after)) };
                    }
                    else
                    {
                        many = new AbstractList[] { new ElementList(before), new ElementList(items), new ElementList(after) };
                    }
                }
            }

            public override AbstractList RemoveAtInternal(int index, int length)
            {
                if (length == 0)
                {
                    return this;
                }

                if (index < 0 || index >= this.items.Length)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                if (index + length > this.items.Length)
                {
                    throw new ArgumentOutOfRangeException("length");
                }

                if (index == 0 && length == this.items.Length)
                {
                    return Empty;
                }
                else
                {
                    return new ElementList(this.items.RemoveAt(index, length));
                }
            }

            public override void CopyTo(int index, T[] array, int arrayOffset, int length)
            {
                Array.Copy(this.items, index, array, arrayOffset, length);
            }
        }

        /// <summary>
        /// This is a compound list of lists
        /// </summary>
        private class CompoundList : AbstractList
        {
            private readonly AbstractList[] arrays;
            private readonly int count;

            internal CompoundList(int count, params AbstractList[] arrays)
            {
                this.count = count;
                this.arrays = arrays;

                System.Diagnostics.Debug.Assert(arrays.Length > 0);
            }

            internal CompoundList(params AbstractList[] arrays)
                : this(SumCounts(arrays), arrays)
            {
            }

            private static int SumCounts(AbstractList[] arrays)
            {
                int c = 0;
                for (int i = 0; i < arrays.Length; i++)
                {
                    c += arrays[i].Count;
                }

                return c;
            }

            public override int Count
            {
                get { return this.count; }
            }

            public override IEnumerator<T> GetEnumerator()
            {
                // Our indexer is O(n), so override GetEnumerator to efficiently walk nested
                // CompoundLists.
                // To avoid allocating nested enumerators we manually manage a stack of the
                // "CompoundList" elements we come across as we enumerate.  When we find one, we push
                // the list we're currently working on onto a stack, together with the index to come
                // back to.  Then we push the the nested CompoundList (with a start at index of 0),
                // and exit the for loop.  If it's something other than a compound list, we just rely
                // on the indexer being O(1).
                var compound = this;
                var startIndex = 0;
                var stack = new Stack<ValueTuple<CompoundList, int>>();
                do
                {
                    if (stack.Count != 0)
                    {
                        var item = stack.Pop();
                        compound = item.Item1;
                        startIndex = item.Item2;
                    }

                StartAgain:
                    var compoundCount = compound.arrays.Length;
                    for (int i = startIndex; i < compoundCount; i++)
                    {
                        var element = compound.arrays[i];
                        var compoundElement = element as CompoundList;
                        if (compoundElement != null)
                        {
                            stack.Push(ValueTuple.Create(compound, i + 1));
                            compound = compoundElement;
                            startIndex = 0;
                            goto StartAgain;
                        }
                        else
                        {
                            var elementCount = element.Count;
                            for (int j = 0; j < elementCount; j++)
                            {
                                yield return element[j];
                            }
                        }
                    }
                }
                while (!stack.IsEmpty());
            }

            public override T this[int index]
            {
                get
                {
                    for (int i = 0; i < this.arrays.Length; i++)
                    {
                        var array = this.arrays[i];
                        if (index < array.Count)
                        {
                            return array[index];
                        }
                        else
                        {
                            index -= array.Count;
                        }
                    }

                    throw new IndexOutOfRangeException();
                }
            }

            public override int IndexOf(T item)
            {
                int offset = 0;

                for (int slot = 0; slot < this.arrays.Length; slot++)
                {
                    var subArray = this.arrays[slot];
                    var subIndex = subArray.IndexOf(item);
                    if (subIndex >= 0)
                    {
                        return offset + subIndex;
                    }

                    offset += subArray.Count;
                }

                return -1;
            }

            private int GetSlot(int index, out int slotIndex)
            {
                if (index >= this.count)
                {
                    slotIndex = this.arrays[this.arrays.Length - 1].Count;
                    return this.arrays.Length - 1;
                }
                else if (index < 0)
                {
                    slotIndex = 0;
                    return 0;
                }

                for (int i = 0; i < this.arrays.Length; i++)
                {
                    var array = this.arrays[i];
                    if (index < array.Count)
                    {
                        slotIndex = index;
                        return i;
                    }
                    else
                    {
                        index -= array.Count;
                    }
                }

                throw new InvalidOperationException();
            }

            public override void InsertAtInternal(int index, T[] items, out AbstractList one, out AbstractList[] many)
            {
                int subIndex;
                int slot = this.GetSlot(index, out subIndex);
                var subArray = this.arrays[slot];

                AbstractList subOne;
                AbstractList[] subMany;
                subArray.InsertAtInternal(subIndex, items, out subOne, out subMany);

                this.ReplaceSubArray(slot, subOne, subMany, out one, out many);
            }

            private void ReplaceSubArray(int slot, AbstractList subOne, AbstractList[] subMany, out AbstractList one, out AbstractList[] many)
            {
                if (subOne != null)
                {
                    one = new CompoundList(this.arrays.ReplaceAt(slot, subOne));
                    many = null;
                }
                else if (subMany == null || subMany.Length == 0)
                {
                    // no subOne or subMany? Item in slot is gone
                    if (this.arrays.Length == 1)
                    {
                        // nothing left
                        one = Empty;
                    }
                    else
                    {
                        one = new CompoundList(this.arrays.RemoveAt(slot));
                    }

                    many = null;
                }
                else if (this.arrays.Length - 1 + subMany.Length <= IdealSlotCount)
                {
                    one = new CompoundList(this.arrays.ReplaceAt(slot, 1, subMany));
                    many = null;
                }
                else if (slot == 0)
                {
                    one = null;
                    many = new AbstractList[] 
                    { 
                        new CompoundList(subMany),
                        new CompoundList(this.arrays.RemoveAt(0, 1))
                    };
                }
                else if (slot == this.arrays.Length - 1)
                {
                    one = null;
                    many = new AbstractList[] 
                    { 
                        new CompoundList(this.arrays.RemoveAt(slot, this.arrays.Length - slot)),
                        new CompoundList(subMany)
                    };
                }
                else
                {
                    var before = this.arrays.RemoveAt(slot, this.arrays.Length - slot);
                    var after = this.arrays.RemoveAt(0, slot + 1);

                    one = null;

                    if (before.Length + subMany.Length <= IdealElementCount)
                    {
                        many = new AbstractList[] { new CompoundList(before.Append(subMany)), new CompoundList(after) };
                    }
                    else if (after.Length + subMany.Length <= IdealElementCount)
                    {
                        many = new AbstractList[] { new CompoundList(before), new CompoundList(subMany.Append(after)) };
                    }
                    else
                    {
                        many = new AbstractList[] { new CompoundList(before), new CompoundList(subMany), new CompoundList(after) };
                    }
                }
            }

            public override AbstractList RemoveAtInternal(int index, int length)
            {
                if (length == 0)
                {
                    return this;
                }

                if (index < 0 || index >= this.Count)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                if (index + length > this.Count)
                {
                    throw new ArgumentOutOfRangeException("length");
                }

                // if all removes are local then do it the easy way
                int subIndex;
                var slot = this.GetSlot(index, out subIndex);
                var subArray = this.arrays[slot];

                // all removes are local to one element?
                if (subIndex + length < subArray.Count)
                {
                    var newSubArray = subArray.RemoveAtInternal(subIndex, length);
                    return new CompoundList(this.arrays.ReplaceAt(slot, newSubArray));
                }

                // otherwise do it the long way
                var newList = new List<AbstractList>(this.Count);
                for (slot = 0; slot < this.arrays.Length; slot++)
                {
                    subArray = this.arrays[slot];

                    if (length > 0)
                    {
                        if (index < subArray.Count)
                        {
                            var lengthToRemove = Math.Min(length, subArray.Count - index);
                            var newSubArray = subArray.RemoveAtInternal(index, lengthToRemove);

                            length -= lengthToRemove;
                            index = 0;

                            if (newSubArray.Count > 0)
                            {
                                newList.Add(newSubArray);
                            }
                        }
                        else
                        {
                            newList.Add(subArray);
                            index -= subArray.Count;
                        }
                    }
                    else
                    {
                        newList.Add(subArray);
                    }
                }

                if (newList.Count == 0)
                {
                    return Empty;
                }
                else
                {
                    return new CompoundList(newList.ToArray());
                }
            }

            public override void CopyTo(int index, T[] array, int arrayOffset, int length)
            {
                for (int slot = 0; slot < this.arrays.Length && length > 0; slot++)
                {
                    var subArray = this.arrays[slot];
                    if (index < subArray.Count)
                    {
                        int lengthToCopy = Math.Min(length, subArray.Count - index);
                        subArray.CopyTo(index, array, arrayOffset, lengthToCopy);
                        index = 0;
                        arrayOffset += lengthToCopy;
                        length -= lengthToCopy;
                    }
                    else
                    {
                        index += subArray.Count;
                    }
                }
            }
        }

        #region IList<T> Members

        int IList<T>.IndexOf(T item)
        {
            return this.IndexOf(item);
        }

        void IList<T>.Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        void IList<T>.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        T IList<T>.this[int index]
        {
            get
            {
                return this[index];
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        #endregion

        #region ICollection<T> Members

        void ICollection<T>.Add(T item)
        {
            throw new NotSupportedException();
        }

        void ICollection<T>.Clear()
        {
            throw new NotSupportedException();
        }

        bool ICollection<T>.Contains(T item)
        {
            return this.Contains(item);
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            this.CopyTo(0, array, arrayIndex, this.Count);
        }

        int ICollection<T>.Count
        {
            get { return this.Count; }
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return true; }
        }

        bool ICollection<T>.Remove(T item)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region IEnumerable<T> Members

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion
    }
}