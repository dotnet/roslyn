// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract partial class TagHelperCollection
{
    public struct Enumerator(TagHelperCollection collection) : IDisposable
    {
        private SegmentEnumerator _segmentEnumerator = new(collection);
        private ReadOnlyMemory<TagHelperDescriptor> _segment;
        private int _index = -1;
        private TagHelperDescriptor? _current;

        public readonly TagHelperDescriptor Current
            => _index >= 0
                ? _current!
                : ThrowHelper.ThrowInvalidOperationException<TagHelperDescriptor>("Enumeration has not started. Call MoveNext.");

        public bool MoveNext()
        {
            // Try to move to next item in current segment
            var nextIndex = _index + 1;
            if (_index >= 0 && nextIndex < _segment.Length)
            {
                _index = nextIndex;
                _current = _segment.Span[_index];
                return true;
            }

            // Move to next segment
            while (_segmentEnumerator.MoveNext())
            {
                _segment = _segmentEnumerator.Current;
                _index = 0;

                if (_segment.Length > 0)
                {
                    _current = _segment.Span[0];
                    return true;
                }

                // Empty segment, continue to next one
            }

            return false;
        }

        public void Reset()
        {
            _segmentEnumerator.Reset();
            _segment = default;
            _index = -1;
            _current = null;
        }

        public void Dispose()
        {
            Reset();
        }
    }

    private sealed class EnumeratorImpl(TagHelperCollection collection) : IEnumerator<TagHelperDescriptor>
    {
        private Enumerator _enumerator = new(collection);

        public TagHelperDescriptor Current => _enumerator.Current;

        object IEnumerator.Current => Current;

        public bool MoveNext()
            => _enumerator.MoveNext();

        public void Reset()
            => _enumerator.Reset();

        public void Dispose()
            => _enumerator.Dispose();
    }
}
