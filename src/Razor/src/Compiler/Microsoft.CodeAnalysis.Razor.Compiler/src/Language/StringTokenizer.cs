// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

internal readonly ref struct StringTokenizer
{
    private readonly ReadOnlySpan<char> _value;
    private readonly ReadOnlySpan<char> _separators;
    private readonly bool _hasValue;

    private StringTokenizer(ReadOnlySpan<char> value, ReadOnlySpan<char> separators, bool hasValue)
    {
        _value = value;
        _separators = separators;
        _hasValue = hasValue;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="StringTokenizer"/>.
    /// </summary>
    /// <param name="value">The <see cref="ReadOnlySpan{T}"/> to tokenize.</param>
    /// <param name="separators">The characters to tokenize by.</param>
    public StringTokenizer(ReadOnlySpan<char> value, ReadOnlySpan<char> separators)
        : this(value, separators, hasValue: true)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="StringTokenizer"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to tokenize.</param>
    /// <param name="separators">The characters to tokenize by.</param>
    public StringTokenizer(string? value, ReadOnlySpan<char> separators)
        : this(value.AsSpanOrDefault(), separators, hasValue: value is not null)
    {
    }

    public Enumerator GetEnumerator() => new(_value, _separators, done: !_hasValue);

    public ref struct Enumerator
    {
        private ReadOnlySpan<char> _span;
        private readonly ReadOnlySpan<char> _separators;
        private bool _done;

        internal Enumerator(ReadOnlySpan<char> span, ReadOnlySpan<char> separators, bool done)
        {
            _span = span;
            _separators = separators;
            Current = default;
            _done = done;
        }

        public ReadOnlySpan<char> Current { get; private set; }

        public bool MoveNext()
        {
            if (_span.Length == 0)
            {
                Current = default;

                if (!_done)
                {
                    // The _done flag is used to ensure that we return an empty ReadOnlySpan<char>
                    // at least once in the case that the StringTokenizer is initialized with
                    // an empty string or a string that ends in a separator.
                    _done = true;
                    return true;
                }

                return false;
            }

            var separatorIndex = _span.IndexOfAny(_separators);
            if (separatorIndex < 0)
            {
                Current = _span;
                _span = default;
                _done = true;
                return true;
            }

            Current = _span[..separatorIndex];

            var nextIndex = separatorIndex + 1;

            _span = nextIndex < _span.Length
                ? _span[nextIndex..]
                : default;

            return true;
        }
    }
}
