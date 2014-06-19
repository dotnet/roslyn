using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Roslyn.Compilers
{
    // The idea of this class is that we have a set that keeps track of two 
    // things:
    // 1) What the most recently added member is
    // 2) What the previously added members are
    //
    // This set is useful for situations where the common operation from the 
    // client is to repeatedly add a new item, then remove it, then add a 
    // different new item, then remove it, and so on.  The client wants to 
    // be able to re-use cached information about the old, infrequently changing
    // items.  By using this class it can determine what was added most recently
    // so that it can decide whether or not to keep its cache around.
    //
    // In particular, consider a set of source file syntax trees. The likely 
    // operation is that the most recently edited source file is going to be 
    // removed from the set, and then a new one representing the latest edits 
    // will be added. The client can cache and re-use information derived from 
    // the persistent, infrequently changing syntax trees.

    internal sealed class ImmutableBipartiteSet<TItem> : IEnumerable<TItem>
        where TItem : class
    {
        private readonly ImmutableSet<TItem> oldItems;
        private readonly TItem newItem;

        public static readonly ImmutableBipartiteSet<TItem> Empty  = new ImmutableBipartiteSet<TItem>(null, null);

        private ImmutableBipartiteSet(
            TItem newItem,
            ImmutableSet<TItem> oldItems)
        {
            this.newItem = newItem;
            this.oldItems = oldItems ?? ImmutableSet<TItem>.Empty;
        }

        public IEnumerator<TItem> GetEnumerator()
        {
            if (this.NewestItem != null)
            {
                yield return this.NewestItem;
            }

            foreach (var item in this.OldItems)
            {
                yield return item;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public ImmutableSet<TItem> OldItems
        {
            get
            {
                return this.oldItems;
            }
        }

        public TItem NewestItem
        {
            get
            {
                return this.newItem;
            }
        }

        public ImmutableBipartiteSet<TItem> Add(TItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (this.Contains(item))
            {
                return this;
            }

            var set = this.oldItems;
            if (!object.Equals(this.newItem, null))
            {
                // Two new items added in a row. Make the previous new item
                // part of the old items.
                set = set.Add(this.newItem);
            }

            return new ImmutableBipartiteSet<TItem>(item, set);
        }

        public ImmutableBipartiteSet<TItem> Remove(TItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (!this.Contains(item))
            {
                return this;
            }

            var set = this.oldItems;
            if (!object.Equals(item, this.newItem))
            {
                set = set.Remove(item);
                if (this.newItem != null)
                {
                    set = set.Add(this.newItem);
                }
            }

            return new ImmutableBipartiteSet<TItem>(newItem: null, oldItems: set);
        }

        public bool Contains(TItem item)
        {
            return object.Equals(this.newItem, item) || this.oldItems.Contains(item);
        }
    }
}