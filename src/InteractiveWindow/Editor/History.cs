// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    internal class History
    {
        internal sealed class Entry
        {
            /// <summary>
            /// The cached text of this entry, which may exist if we've detached from the span.
            /// </summary>
            private string _cachedText;

            /// <summary>
            /// The span of the original submission of this text.
            /// </summary>
            private SnapshotSpan? _originalSpan;

            public bool Command { get; set; }
            public bool Failed { get; set; }

            public SnapshotSpan? OriginalSpan { get { return _originalSpan; } }

            public string Text
            {
                get
                {
                    if (_cachedText != null)
                    {
                        return _cachedText;
                    }

                    return _originalSpan.Value.GetText();
                }
            }

            internal void ForgetOriginalBuffer()
            {
                if (_originalSpan.HasValue)
                {
                    _cachedText = _originalSpan.Value.GetText();
                    _originalSpan = null;
                }
            }

            public Entry(SnapshotSpan span)
            {
                _originalSpan = span;
            }
        }

        private readonly List<Entry> _history;
        private readonly int _maxLength;

        private int _current;
        private bool _live;

        internal string UncommittedInput { get; set; }

        internal History()
            : this(maxLength: 50)
        {
        }

        internal History(int maxLength)
        {
            _maxLength = maxLength;
            _current = -1;
            _history = new List<Entry>();
        }

        internal void Clear()
        {
            _current = -1;
            _live = false;
            _history.Clear();
        }

        internal void ForgetOriginalBuffers()
        {
            foreach (var entry in _history)
            {
                entry.ForgetOriginalBuffer();
            }
        }

        internal int MaxLength
        {
            get { return _maxLength; }
        }

        internal int Length
        {
            get { return _history.Count; }
        }

        internal IEnumerable<Entry> Items
        {
            get { return _history; }
        }

        internal Entry Last
        {
            get
            {
                if (_history.Count > 0)
                {
                    return _history[_history.Count - 1];
                }
                else
                {
                    return null;
                }
            }
        }

        internal void Add(SnapshotSpan span)
        {
            var entry = new Entry(span);
            var text = span.GetText();

            _live = false;
            if (Length == 0 || Last.Text != text)
            {
                _history.Add(entry);
            }

            if (_history[(_current == -1) ? Length - 1 : _current].Text != text)
            {
                _current = -1;
            }

            if (Length > MaxLength)
            {
                _history.RemoveAt(0);
                if (_current > 0)
                {
                    _current--;
                }
            }
        }

        private Entry Get(string pattern, int step)
        {
            var startPos = _current;
            var next = Move(pattern, step);
            if (next == null)
            {
                _current = startPos;
                return null;
            }

            return next;
        }

        internal Entry GetNext(string pattern = null)
        {
            return Get(pattern, step: +1);
        }

        internal Entry GetPrevious(string pattern = null)
        {
            return Get(pattern, step: -1);
        }

        private Entry Move(string pattern, int step)
        {
            Debug.Assert(step == -1 || step == +1);

            bool wasLive = _live;
            _live = true;

            if (Length == 0)
            {
                return null;
            }

            bool patternEmpty = string.IsNullOrWhiteSpace(pattern);

            int start, end;
            if (step > 0)
            {
                start = 0;
                end = Length - 1;
            }
            else
            {
                start = Length - 1;
                end = 0;
            }

            // no search in progress:
            if (_current == -1)
            {
                _current = start;
            }

            int visited = 0;
            while (true)
            {
                if (_current == end)
                {
                    if (visited < Length)
                    {
                        _current = start;
                    }
                    else
                    {
                        // we cycled thru the entire history:
                        return null;
                    }
                }
                else if (!wasLive && patternEmpty)
                {
                    // Handles up up up enter up
                    // Do nothing
                }
                else
                {
                    _current += step;
                }

                var entry = _history[_current];
                if (patternEmpty || Matches(entry.Text, pattern))
                {
                    return entry;
                }

                visited++;
            }
        }

        private static bool Matches(string entry, string pattern)
        {
            return entry.Contains(pattern);
        }
    }
}
