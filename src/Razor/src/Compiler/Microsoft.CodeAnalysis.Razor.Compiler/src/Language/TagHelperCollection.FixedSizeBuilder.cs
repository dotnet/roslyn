// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract partial class TagHelperCollection
{
    /// <summary>
    ///  Provides a builder for efficiently constructing a collection of
    ///  <see cref="TagHelperDescriptor"/> objects with a single segment
    ///  and a fixed maximum size.
    /// </summary>
    private ref struct FixedSizeBuilder
    {
        private readonly TagHelperDescriptor[] _items;
        private HashSet<Checksum> _set;
        private int _length;

        public FixedSizeBuilder(int size)
        {
            _items = new TagHelperDescriptor[size];
            _set = ChecksumSetPool.Default.Get();
            _length = 0;

#if NET
            _set.EnsureCapacity(size);
#endif
        }

        public void Dispose()
        {
            var set = Interlocked.Exchange(ref _set, null!);
            if (set is not null)
            {
                ChecksumSetPool.Default.Return(set);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Add(TagHelperDescriptor item)
        {
            if (!_set.Add(item.Checksum))
            {
                return false;
            }

            AppendItem(item);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(TagHelperCollection collection)
        {
            foreach (var item in collection)
            {
                if (_set.Add(item.Checksum))
                {
                    AppendItem(item);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(ReadOnlySpan<TagHelperDescriptor> span)
        {
            foreach (var item in span)
            {
                if (_set.Add(item.Checksum))
                {
                    AppendItem(item);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(IEnumerable<TagHelperDescriptor> source)
        {
            foreach (var item in source)
            {
                if (_set.Add(item.Checksum))
                {
                    AppendItem(item);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AppendItem(TagHelperDescriptor item)
        {
            _items[_length++] = item;
        }

        public readonly TagHelperCollection ToCollection()
            => _length switch
            {
                0 => Empty,
                var length => new SingleSegmentCollection(_items.AsMemory(0, length))
            };
    }
}
