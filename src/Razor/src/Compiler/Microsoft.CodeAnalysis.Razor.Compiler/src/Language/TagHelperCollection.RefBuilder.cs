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
    public ref partial struct RefBuilder
    {
#pragma warning disable CA2213 // Disposable fields should be disposed
        private MemoryBuilder<TagHelperDescriptor> _builder;
#pragma warning restore CA2213

        private HashSet<Checksum> _set;

        public RefBuilder()
            : this(initialCapacity: 8)
        {
        }

        public RefBuilder(int initialCapacity)
        {
            if (initialCapacity < 8)
            {
                initialCapacity = 8;
            }

            _builder = new(initialCapacity, clearArray: true);
            _set = ChecksumSetPool.Default.Get();

#if NET
            _set.EnsureCapacity(initialCapacity);
#endif
        }

        public void Dispose()
        {
            _builder.Dispose();
            
            var set = Interlocked.Exchange(ref _set, null!);
            if (set is not null)
            {
                ChecksumSetPool.Default.Return(set);
            }
        }

        public readonly bool IsEmpty => Count == 0;

        public readonly int Count => _builder.Length;

        public readonly TagHelperDescriptor this[int index]
        {
            get
            {
                ArgHelper.ThrowIfNegative(index);
                ArgHelper.ThrowIfGreaterThanOrEqual(index, Count);

                return _builder[index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Add(TagHelperDescriptor item)
        {
            if (!_set.Add(item.Checksum))
            {
                return false;
            }

            _builder.Append(item);
            return true;
        }

        public void AddRange(TagHelperCollection collection)
        {
            foreach (var item in collection)
            {
                if (_set.Add(item.Checksum))
                {
                    _builder.Append(item);
                }
            }
        }

        public void AddRange(ReadOnlySpan<TagHelperDescriptor> span)
        {
            foreach (var item in span)
            {
                if (_set.Add(item.Checksum))
                {
                    _builder.Append(item);
                }
            }
        }

        public void AddRange(IEnumerable<TagHelperDescriptor> source)
        {
            foreach (var item in source)
            {
                if (_set.Add(item.Checksum))
                {
                    _builder.Append(item);
                }
            }
        }

        public readonly Enumerator GetEnumerator()
            => new(this);

        public readonly TagHelperCollection ToCollection()
        {
            switch (_builder.Length)
            {
                case 0:
                    return Empty;
            }

            // We need to copy the final array out since MemoryBuilder<T>
            // uses ArrayPool<T> internally.
            var array = _builder.AsMemory().ToArray();

            return new SingleSegmentCollection(array);
        }
    }
}
