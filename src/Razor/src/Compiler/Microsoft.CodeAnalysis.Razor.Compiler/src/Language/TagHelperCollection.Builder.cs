// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract partial class TagHelperCollection
{
    public sealed partial class Builder : ICollection<TagHelperDescriptor>, IReadOnlyList<TagHelperDescriptor>, IDisposable
    {
        private List<TagHelperDescriptor> _items;
        private HashSet<Checksum> _set;

        public Builder()
        {
            _items = TagHelperDescriptorListPool.Default.Get();
            _set = ChecksumSetPool.Default.Get();
        }

        public void Dispose()
        {
            var items = Interlocked.Exchange(ref _items, null!);
            if (items is not null)
            {
                TagHelperDescriptorListPool.Default.Return(items);
            }

            var set = Interlocked.Exchange(ref _set, null!);
            if (set is not null)
            {
                ChecksumSetPool.Default.Return(set);
            }
        }

        public bool IsEmpty => Count == 0;

        public int Count => _items.Count;

        public bool IsReadOnly => false;

        public TagHelperDescriptor this[int index]
        {
            get
            {
                ArgHelper.ThrowIfNegative(index);
                ArgHelper.ThrowIfGreaterThanOrEqual(index, Count);

                return _items[index];
            }
        }

        public bool Add(TagHelperDescriptor item)
        {
            if (!_set.Add(item.Checksum))
            {
                return false;
            }

            _items.Add(item);
            return true;
        }

        void ICollection<TagHelperDescriptor>.Add(TagHelperDescriptor item)
            => Add(item);

        public void AddRange(TagHelperCollection items)
        {
            foreach (var item in items)
            {
                if (_set.Add(item.Checksum))
                {
                    _items.Add(item);
                }
            }
        }

        public void AddRange(ReadOnlySpan<TagHelperDescriptor> span)
        {
            foreach (var item in span)
            {
                if (_set.Add(item.Checksum))
                {
                    _items.Add(item);
                }
            }
        }

        public void AddRange(IEnumerable<TagHelperDescriptor> source)
        {
            foreach (var item in source)
            {
                if (_set.Add(item.Checksum))
                {
                    _items.Add(item);
                }
            }
        }

        public void Clear()
        {
            _items.Clear();
            _set.Clear();
        }

        public bool Contains(TagHelperDescriptor item)
            => _set.Contains(item.Checksum);

        public void CopyTo(TagHelperDescriptor[] array, int arrayIndex)
            => _items.CopyTo(array, arrayIndex);

        public bool Remove(TagHelperDescriptor item)
            => _set.Remove(item.Checksum) && _items.Remove(item);

        public TagHelperCollection ToCollection()
        {
            if (_items.Count == 0)
            {
                return Empty;
            }

            return new SingleSegmentCollection(_items.ToArray().AsMemory());
        }

        public Enumerator GetEnumerator()
            => new(this);

        IEnumerator<TagHelperDescriptor> IEnumerable<TagHelperDescriptor>.GetEnumerator()
            => new EnumeratorImpl(this);

        IEnumerator IEnumerable.GetEnumerator()
            => new EnumeratorImpl(this);
    }
}
