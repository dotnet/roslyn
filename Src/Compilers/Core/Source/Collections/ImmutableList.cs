using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Roslyn.Compilers
{
    public class ImmutableList<T> : IList<T>
    {
        private readonly Segment root;

        private ImmutableList(Segment root)
        {
            this.root = root;
        }

        public ImmutableList(IEnumerable<T> items)
        {
            this.root = Empty.InsertAt(0, items).root;
        }

        public int Count
        {
            get { return this.root != null ? this.root.Count : 0; }
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index > this.Count)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                return this.root.Get(index);
            }
        }

#if DEBUG
        private T[] Items
        {
            get { return this.ToArray(); }
        }
#endif

        public ImmutableList<T> InsertAt(int index, T item)
        {
            if (index < 0 || index > this.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            return InsertImpl(index, new T[] { item });
        }

        public ImmutableList<T> InsertAt(int index, IEnumerable<T> items)
        {
            if (index < 0 || index > this.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            if (!IsImmutable(items))
            {
                items = items.ToArray();
            }

            return this.InsertImpl(index, items);
        }

        public ImmutableList<T> Append(T item)
        {
            return InsertAt(this.Count, item);
        }

        public ImmutableList<T> Append(IEnumerable<T> items)
        {
            return InsertAt(this.Count, items);
        }

        public ImmutableList<T> Prepend(T item)
        {
            return InsertAt(0, item);
        }

        public ImmutableList<T> Prepend(IEnumerable<T> items)
        {
            return InsertAt(0, items);
        }

        private static bool IsImmutable(IEnumerable<T> items)
        {
            if (items is ImmutableList<T> || items is ReadOnlyCollection<T> || items is string)
            {
                return true;
            }

            return false;
        }

        private ImmutableList<T> InsertImpl(int index, IEnumerable<T> items)
        {
            if (this.root == null)
            {
                return new ImmutableList<T>(LeafSegment.MakeLeaf(items));
            }
            else
            {
                var segs = this.root.Insert(index, items);
                if (segs.Length == 1)
                {
                    return new ImmutableList<T>(segs[0]);
                }
                else
                {
                    return new ImmutableList<T>(new TreeSegment(segs));
                }
            }
        }

        public ImmutableList<T> RemoveAt(int index)
        {
            return RemoveAt(index, 1);
        }

        public ImmutableList<T> RemoveAt(int index, int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length");
            }

            if (index >= 0 && index < this.Count)
            {
                if (index + length > this.Count)
                {
                    length = this.Count - index;
                }

                var segs = this.root.Remove(index, length);
                if (segs.Length == 0)
                {
                    return Empty;
                }
                else if (segs.Length == 1)
                {
                    return new ImmutableList<T>(segs[0]);
                }
                else
                {
                    return new ImmutableList<T>(new TreeSegment(segs));
                }
            }

            return this;
        }

        public ImmutableList<T> ReplaceAt(int index, T item)
        {
            return this.RemoveAt(index).InsertAt(index, item);
        }

        public ImmutableList<T> ReplaceAt(int index, int length, IEnumerable<T> items)
        {
            return this.RemoveAt(index, length).InsertAt(index, items);
        }

        public ImmutableList<T> Range(int index, int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length");
            }

            if (index < 0 || index >= this.Count)
            {
                return Empty;
            }

            if (index + length > this.Count)
            {
                length = this.Count - index;
            }

            if (index == 0)
            {
                return this.RemoveAt(length, this.Count - length);
            }
            else if (index + length == this.Count)
            {
                return this.RemoveAt(0, index);
            }
            else
            {
                return this.RemoveAt(index + length, this.Count - (index + length)).RemoveAt(0, index);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var leaf in this.GetLeafSegments())
            {
                foreach (var item in leaf.GetItems())
                {
                    yield return item;
                }
            }
        }

        private IEnumerable<LeafSegment> GetLeafSegments()
        {
            if (this.root != null)
            {
                LeafSegment vs = this.root as LeafSegment;
                if (vs != null)
                {
                    yield return vs;
                }
                else
                {
                    var stack = new Stack<IEnumerator<Segment>>();
                    stack.Push(((IEnumerable<Segment>)((TreeSegment)this.root).Segments).GetEnumerator());
                    while (stack.Count > 0)
                    {
                        var en = stack.Peek();
                        if (en.MoveNext())
                        {
                            vs = en.Current as LeafSegment;
                            if (vs != null)
                            {
                                yield return vs;
                            }
                            else
                            {
                                stack.Push(((IEnumerable<Segment>)((TreeSegment)en.Current).Segments).GetEnumerator());
                            }
                        }
                        else
                        {
                            stack.Pop();
                        }
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public static readonly ImmutableList<T> Empty = new ImmutableList<T>((Segment)null);

        #region IList<T> Members

        int IList<T>.IndexOf(T item)
        {
            var comparer = EqualityComparer<T>.Default;
            for (int i = 0, n = this.Count; i < n; i++)
            {
                if (comparer.Equals(this[i], item))
                {
                    return i;
                }
            }

            return -1;
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
            var comparer = EqualityComparer<T>.Default;
            return this.Any(t => comparer.Equals(t, item));
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            if (this.root != null)
            {
                this.root.CopyTo(0, this.Count, array, arrayIndex);
            }
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

        private abstract class Segment
        {
            internal abstract int Count { get; }

            internal abstract T Get(int index);
            internal abstract void CopyTo(int index, int length, T[] array, int arrayOffset);
            internal abstract Segment[] Insert(int index, IEnumerable<T> items);
            internal abstract Segment[] Remove(int index, int length);
            internal abstract Segment[] BalanceWith(Segment segment);

            protected const int MaxSize = 32;
            protected const int MinSize = 8;
            internal static readonly Segment[] NoSegments = new Segment[] { };

            internal static int GetCount(IEnumerable<T> items)
            {
                var array = items as T[];
                if (array != null)
                {
                    return array.Length;
                }

                var list = items as IList<T>;
                if (list != null)
                {
                    return list.Count;
                }

                var str = ((object)items) as string;
                if (str != null)
                {
                    return str.Length;
                }

                return items.Count();
            }

            internal static Segment[] Balance(IList<Segment> segments)
            {
                var list = segments as List<Segment>;
                for (int i = 0; i < segments.Count - 1;)
                {
                    var s1 = segments[i];
                    var s2 = segments[i + 1];
                    var segs = s1.BalanceWith(s2);
                    if (segs.Length != 2 || segs[0] != s1 || segs[1] != s2)
                    {
                        if (list == null)
                        {
                            segments = list = segments.ToList();
                        }

                        list.RemoveRange(i, 2);
                        list.InsertRange(i, segs);
                    }

                    i++;
                }

                var array = segments as Segment[];
                if (array == null)
                {
                    array = segments.ToArray();
                }

                return array;
            }
        }

        private abstract class LeafSegment : Segment
        {
            internal abstract T[] ToArray();
            internal abstract IEnumerable<T> GetItems();
            internal abstract LeafSegment GetRange(int offset, int length);

            internal override Segment[] Insert(int index, IEnumerable<T> items)
            {
                Segment[] segs;
                if (index == 0)
                {
                    segs = new Segment[] { MakeLeaf(items), this };
                }
                else if (index == this.Count)
                {
                    segs = new Segment[] { this, MakeLeaf(items) };
                }
                else
                {
                    segs = new Segment[] { this.GetRange(0, index), MakeLeaf(items), this.GetRange(index, this.Count - index) };
                }

                return Balance(segs);
            }

            internal override Segment[] Remove(int index, int length)
            {
                if (index == 0)
                {
                    return new Segment[] { this.GetRange(length, this.Count - length) };
                }
                else if (index + length == this.Count)
                {
                    return new Segment[] { this.GetRange(0, index) };
                }
                else
                {
                    return Balance(new Segment[] { this.GetRange(0, index), this.GetRange(index + length, this.Count - (index + length)) });
                }
            }

            internal override Segment[] BalanceWith(Segment segment)
            {
                LeafSegment s1 = this;
                LeafSegment s2 = (LeafSegment)segment;
                if (s1.Count <= MinSize)
                {
                    if (s2.Count <= MaxSize)
                    {
                        return MakeLeaves(s1.ToArray().Append(s2.ToArray()));
                    }
                    else
                    {
                        // borrow MinSize from s2
                        return new Segment[]
                        {
                            MakeLeaf(s1.ToArray().Append(s2.GetRange(0, MinSize).ToArray())),
                            s2.GetRange(MinSize, s2.Count - MinSize)
                        };
                    }
                }
                else if (s2.Count <= MinSize)
                {
                    if (s1.Count <= MaxSize)
                    {
                        return MakeLeaves(s1.ToArray().Append(s2.ToArray()));
                    }
                    else
                    {
                        // borrow MinSize from s1
                        return new Segment[]
                        {
                            s1.GetRange(0, s1.Count - MinSize),
                            MakeLeaf(s1.GetRange(s1.Count - MinSize, MinSize).ToArray().Append(s2.ToArray()))
                        };
                    }
                }

                return new Segment[] { s1, s2 };
            }

            internal static LeafSegment[] MakeLeaves(IList<T> items)
            {
                if (items.Count <= MaxSize)
                {
                    return new LeafSegment[] { MakeLeaf(items, 0, items.Count) };
                }
                else
                {
                    int size = items.Count / 2;
                    return new LeafSegment[] { MakeLeaf(items, 0, size), MakeLeaf(items, size, items.Count - size) };
                }
            }

            internal static LeafSegment MakeLeaf(IEnumerable<T> items)
            {
                return MakeLeaf(items, 0, GetCount(items));
            }

            private static bool IsSignificantlySmaller(int subLength, int wholeLength)
            {
                return subLength <= (wholeLength >> 3);
            }

            internal static LeafSegment MakeLeaf(IEnumerable<T> items, int offset, int length)
            {
                var array = items as T[];
                if (array != null)
                {
                    if (length <= MaxSize || IsSignificantlySmaller(length, array.Length))
                    {
                        array = array.Copy(offset, length);
                        offset = 0;
                        length = array.Length;
                    }

                    return new ArraySegment(array, offset, length);
                }

                var str = ((object)items) as string;
                if (str != null)
                {
                    if (length <= MaxSize || IsSignificantlySmaller(length, str.Length))
                    {
                        return (LeafSegment)(object)new StringSegment(str.Substring(offset, length), 0, length);
                    }
                    else
                    {
                        return (LeafSegment)(object)new StringSegment(str, offset, length);
                    }
                }

                var list = (IList<T>)items;
                return new ListSegment(list, offset, length);
            }
        }

        private class ArraySegment : LeafSegment
        {
            private readonly T[] items;
            private readonly int offset;
            private readonly int length;

            internal ArraySegment(T[] items, int offset, int length)
            {
                this.items = items;
                this.offset = offset;
                this.length = length;
            }

            internal override int Count
            {
                get { return this.length; }
            }

            internal override T Get(int index)
            {
                return this.items[this.offset + index];
            }

            internal override T[] ToArray()
            {
                if (this.offset == 0 && this.length == this.items.Length)
                {
                    return this.items;
                }
                else
                {
                    return this.items.Copy(this.offset, this.length);
                }
            }

            internal override IEnumerable<T> GetItems()
            {
                if (this.offset == 0 && this.length == this.items.Length)
                {
                    return this.items;
                }
                else
                {
                    return this.EnumerateItems();
                }
            }

            private IEnumerable<T> EnumerateItems()
            {
                for (int i = 0; i < this.length; i++)
                {
                    yield return this.Get(i);
                }
            }

            internal override void CopyTo(int index, int length, T[] array, int arrayOffset)
            {
                Array.Copy(this.items, this.offset + index, array, arrayOffset, length);
            }

            internal override LeafSegment GetRange(int subOffset, int subLength)
            {
                return MakeLeaf(this.items, this.offset + subOffset, subLength);
            }
        }

        private class ListSegment : LeafSegment
        {
            private readonly IList<T> items;
            private readonly int offset;
            private readonly int length;

            internal ListSegment(IList<T> items, int offset, int length)
            {
                System.Diagnostics.Debug.Assert(length > MaxSize);
                this.items = items;
                this.offset = offset;
                this.length = length;
            }

            internal override int Count
            {
                get { return this.length; }
            }

            internal override T Get(int index)
            {
                return this.items[this.offset + index];
            }

            internal override IEnumerable<T> GetItems()
            {
                for (int i = 0; i < this.length; i++)
                {
                    yield return this.Get(i);
                }
            }

            internal override T[] ToArray()
            {
                if (this.offset == 0 && this.length == this.items.Count)
                {
                    return this.items.ToArray();
                }
                else
                {
                    var array = new T[this.length];
                    this.CopyTo(0, this.length, array, 0);
                    return array;
                }
            }

            internal override void CopyTo(int index, int length, T[] array, int arrayOffset)
            {
                for (int i = 0; i < this.length; i++)
                {
                    array[arrayOffset + i] = Get(i);
                }
            }

            internal override LeafSegment GetRange(int subOffset, int subLength)
            {
                return MakeLeaf(this.items, this.offset + subOffset, subLength);
            }
        }

        private class StringSegment : ImmutableList<char>.LeafSegment
        {
            private readonly string items;
            private readonly int offset;
            private readonly int length;

            internal StringSegment(string items, int offset, int length)
            {
                System.Diagnostics.Debug.Assert(length > MaxSize);
                this.items = items;
                this.offset = offset;
                this.length = length;
            }

            internal override int Count
            {
                get { return this.length; }
            }

            internal override char Get(int index)
            {
                return this.items[this.offset + index];
            }

            internal override IEnumerable<char> GetItems()
            {
                for (int i = 0; i < this.length; i++)
                {
                    yield return this.Get(i);
                }
            }

            internal override char[] ToArray()
            {
                var array = new char[this.length];
                this.items.CopyTo(this.offset, array, 0, this.length);
                return array;
            }

            internal override void CopyTo(int index, int length, char[] array, int arrayOffset)
            {
                this.items.CopyTo(this.offset + index, array, arrayOffset, length);
            }

            internal override ImmutableList<char>.LeafSegment GetRange(int subOffset, int subLength)
            {
                return MakeLeaf(this.items, this.offset + subOffset, subLength);
            }
        }

        private class TreeSegment : Segment
        {
            internal readonly Segment[] Segments;
            private readonly int count;

            internal TreeSegment(Segment[] segments)
            {
                this.Segments = segments;
                this.count = segments.Sum(s => s.Count);
            }

            internal override int Count
            {
                get { return this.count; }
            }

            internal override T Get(int index)
            {
                int slot = this.GetSlot(ref index);
                return this.Segments[slot].Get(index);
            }

            private int GetSlot(ref int index)
            {
                // determine segment & slot
                int slot = 0;
                do
                {
                    var seg = this.Segments[slot];
                    if (index <= seg.Count)
                    {
                        break;
                    }

                    index -= seg.Count;
                    slot++;
                }
                while (slot < this.Segments.Length);
                return slot;
            }

            internal override void CopyTo(int index, int length, T[] array, int arrayOffset)
            {
                int slot = this.GetSlot(ref index);
                while (length > 0 && slot < this.Segments.Length)
                {
                    var seg = this.Segments[slot];
                    var len = Math.Min(seg.Count, length);
                    seg.CopyTo(index, len, array, arrayOffset);
                    length -= len;
                    arrayOffset += len;
                    slot++;
                    index = 0;
                }
            }

            internal override Segment[] Insert(int index, IEnumerable<T> items)
            {
                int slot = this.GetSlot(ref index);
                var seg = this.Segments[slot];

                // recurse and replace results
                var newSegs = this.Segments.ReplaceAt(slot, 1, seg.Insert(index, items));

                // return or break down results if too big
                if (newSegs.Length < MaxSize)
                {
                    return new Segment[] { new TreeSegment(newSegs) };
                }
                else
                {
                    int size = newSegs.Length / 2;
                    return new Segment[] { new TreeSegment(newSegs.Copy(0, size)), new TreeSegment(newSegs.Copy(size, newSegs.Length - size)) };
                }
            }

            internal override Segment[] Remove(int index, int length)
            {
                int slot = this.GetSlot(ref index);
                var list = this.Segments.ToList();
                while (length > 0 && slot < list.Count)
                {
                    var seg = list[slot];
                    var len = Math.Min(length, seg.Count - index);
                    var resultSegs = seg.Remove(index, len);
                    list.RemoveAt(slot);
                    list.InsertRange(slot, resultSegs);
                    length -= len;
                    index = 0;
                    slot++;
                }

                var array = Balance(list);
                if (array.Length == 0)
                {
                    return NoSegments;
                }
                else
                {
                    return new Segment[] { new TreeSegment(array) };
                }
            }

            internal override Segment[] BalanceWith(Segment segment)
            {
                TreeSegment s1 = this;
                TreeSegment s2 = (TreeSegment)segment;

                if (s1.Segments.Length <= MinSize)
                {
                    if (s2.Segments.Length <= MaxSize)
                    {
                        return MakeTrees(s1.Segments.Append(s2.Segments));
                    }
                    else
                    {
                        // borrow MinSize from s2
                        return new Segment[]
                        {
                            new TreeSegment(s1.Segments.Append(s2.Segments.Copy(0, MinSize))),
                            new TreeSegment(s2.Segments.Copy(MinSize, s2.Segments.Length - MinSize))
                        };
                    }
                }
                else if (s2.Segments.Length <= MinSize)
                {
                    if (s1.Segments.Length <= MaxSize)
                    {
                        return MakeTrees(s1.Segments.Append(s2.Segments));
                    }
                    else
                    {
                        // borrow MinSize from s1
                        return new Segment[]
                        {
                            new TreeSegment(s1.Segments.Copy(0, s1.Segments.Length - MinSize)),
                            new TreeSegment(s1.Segments.Copy(s1.Segments.Length - MinSize, MinSize))
                        };
                    }
                }

                return new Segment[] { s1, s2 };
            }

            internal static TreeSegment[] MakeTrees(Segment[] segments)
            {
                // return or break down results if too big
                if (segments.Length < MaxSize)
                {
                    return new TreeSegment[] { new TreeSegment(segments) };
                }
                else
                {
                    int size = segments.Length / 2;
                    return new TreeSegment[] { new TreeSegment(segments.Copy(0, size)), new TreeSegment(segments.Copy(size, segments.Length - size)) };
                }
            }
        }
    }
}