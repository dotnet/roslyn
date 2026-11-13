// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed partial class TagHelperBinder
{
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    private readonly struct TagHelperCandidateSet
    {
        public static readonly TagHelperCandidateSet Empty = default!;

        // Alias sets are only created when aliases exist, so a single candidate is stored inline.
        private readonly TagHelperCandidate _value;
        private readonly TagHelperCandidate[]? _array;

        public TagHelperCandidateSet(TagHelperCandidate value)
        {
            _value = value;
            _array = null;
        }

        public TagHelperCandidateSet(TagHelperCandidate[] array)
        {
            _value = default;
            _array = array;
        }

        public TagHelperCandidate this[int index]
        {
            get
            {
                if (_array is { } array)
                {
                    return array[index];
                }

                return index == 0 && _value.Descriptor is not null
                    ? _value
                    : throw new IndexOutOfRangeException();
            }
        }

        public int Count
            => _array?.Length ?? (_value.Descriptor is not null ? 1 : 0);

        public Enumerator GetEnumerator()
            => new(this);

        public struct Enumerator
        {
            private readonly TagHelperCandidateSet _set;
            private int _index;

            internal Enumerator(TagHelperCandidateSet set)
            {
                _set = set;
                _index = -1;
            }

            public bool MoveNext()
            {
                _index++;
                return _index < _set.Count;
            }

            public readonly TagHelperCandidate Current
                => _set[_index];
        }

        private sealed class DebuggerProxy(TagHelperCandidateSet instance)
        {
            private readonly TagHelperCandidateSet _instance = instance;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public TagHelperCandidate[] Items
                => _instance.Count switch
                {
                    0 => [],
                    1 => [_instance[0]],
                    _ => _instance._array!
                };
        }

        private string GetDebuggerDisplay()
            => "Count " + Count;

        public struct Builder
        {
            private TagHelperCandidate _value;
            private TagHelperCandidate[]? _array;
            private int _index;
            private int _size;

            public void IncreaseSize()
            {
                Debug.Assert(_index == 0, "Cannot increase size once items have been added.");
                _size++;
            }

            public void Add(TagHelperCandidate item)
            {
                Debug.Assert(_index < _size, "Cannot add more items.");

                if (_size == 1)
                {
                    _value = item;
                    _index = 1;
                    return;
                }

                var array = _array ??= new TagHelperCandidate[_size];
                array[_index++] = item;
            }

            public readonly TagHelperCandidateSet ToSet()
            {
                Debug.Assert(_index == _size, "Must have added all items.");
                return _size switch
                {
                    0 => Empty,
                    1 => new(_value),
                    _ => new(_array!)
                };
            }
        }
    }
}
