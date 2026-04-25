// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed partial class TagHelperBinder
{
    /// <summary>
    ///  Similar to <see cref="ImmutableArray{T}"/>, but optimized to store either a single value or an array of values.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    private readonly struct TagHelperSet
    {
        public static readonly TagHelperSet Empty = default!;

        private readonly object _valueOrArray;

        public TagHelperSet(TagHelperDescriptor value)
        {
            _valueOrArray = value;
        }

        public TagHelperSet(TagHelperDescriptor[] array)
        {
            _valueOrArray = array;
        }

        public TagHelperDescriptor this[int index]
        {
            get
            {
                return _valueOrArray switch
                {
                    TagHelperDescriptor[] array => array[index],
                    not null when index == 0 => (TagHelperDescriptor)_valueOrArray,
                    _ => throw new IndexOutOfRangeException(),
                };
            }
        }

        public int Count
            => _valueOrArray switch
            {
                TagHelperDescriptor[] array => array.Length,
                null => 0,

                // _valueOrArray can be an array, a single value, or null.
                // So, we can avoid a type check for the single value case.
                _ => 1
            };

        public Enumerator GetEnumerator()
            => new(this);

        public struct Enumerator
        {
            private readonly TagHelperSet _tagHelperSet;
            private int _index;

            internal Enumerator(TagHelperSet tagHelperSet)
            {
                _tagHelperSet = tagHelperSet;
                _index = -1;
            }

            public bool MoveNext()
            {
                _index++;
                return _index < _tagHelperSet.Count;
            }

            public readonly TagHelperDescriptor Current
                => _tagHelperSet[_index];
        }

        private sealed class DebuggerProxy(TagHelperSet instance)
        {
            private readonly TagHelperSet _instance = instance;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public TagHelperDescriptor[] Items
                => _instance._valueOrArray switch
                {
                    TagHelperDescriptor[] array => array,
                    TagHelperDescriptor value => [value],
                    _ => []
                };
        }

        private string GetDebuggerDisplay()
            => "Count " + Count;

        /// <summary>
        ///  This is a mutable builder for <see cref="TagHelperSet"/>. However, it works differently from
        ///  a typical builder. First, you must call <see cref="IncreaseSize"/> to set the number of items.
        ///  Once you've done that for each item to be added, you can call <see cref="Add(TagHelperDescriptor)"/>
        ///  exactly that many times. This ensures that space allocated is exactly what's needed to
        ///  produce the resulting <see cref="TagHelperSet"/>.
        /// </summary>
        public struct Builder
        {
            private object? _valueOrArray;
            private int _index;
            private int _size;

            public void IncreaseSize()
            {
                Debug.Assert(_valueOrArray is null, "Cannot increase size once items have been added.");
                _size++;
            }

            public void Add(TagHelperDescriptor item)
            {
                Debug.Assert(_index < _size, "Cannot add more items.");

                if (_size == 1)
                {
                    // We only need to store a single value.
                    _valueOrArray = item;
                    _index = 1;
                    return;
                }

                Debug.Assert(_valueOrArray is null or TagHelperDescriptor[]);

                if (_valueOrArray is not TagHelperDescriptor[] array)
                {
                    array = new TagHelperDescriptor[_size];
                    _valueOrArray = array;
                }

                array[_index++] = item;
            }

            public readonly TagHelperSet ToSet()
            {
                Debug.Assert(_index == _size, "Must have added all items.");

                return _size switch
                {
                    0 => Empty,
                    1 => new((TagHelperDescriptor)_valueOrArray!),
                    _ => new((TagHelperDescriptor[])_valueOrArray!)
                };
            }
        }
    }
}
