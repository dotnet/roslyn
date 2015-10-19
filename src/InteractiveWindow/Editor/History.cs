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

            //If text at current location in history is not the same as the text you are adding then
            //new command was typed and submitted while navigating history. In this case the _current
            //gets reset.
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

        internal Entry GetNext(string pattern)
        {
            var next = MoveNext(pattern);
            if (next == null)
            {
                // if we hit the end of history list, reset _current to stop navigating history.
                _current = -1;
            }
            return next;
        }

        internal Entry GetPrevious(string pattern)
        {
            var startPos = _current;
            Entry next;
            next = MovePrevious(pattern);
            if (next == null)
            {
                _current = startPos;
                return null;
            }

            return next;
        }

        private Entry MoveNext(string pattern)
        {
            if (Length == 0) return null;

            bool wasCurrentUninitialized = (_current == -1);

            // if current in un-initialized then we are not navigating history yet so
            // there is no next entry.
            if (wasCurrentUninitialized) return null;

            //indicates that history search/navigation is in progress
            _live = true;

            _current++;

            for (; _current < Length; _current++)
            {
                Entry entry;
                if (TryMatch(pattern, out entry)) return entry;
            }

            return null;
        }

        private Entry MovePrevious(string pattern)
        {
            if (Length == 0) return null;
            bool wasLive = _live;

            //indicates that history search/navigation is in progress
            _live = true;

            bool wasCurrentUninitialized = (_current == -1);

            // if current in un-initialized then we are not navigating history yet so
            // current needs to be set to last entry before navigating previous.
            if (wasCurrentUninitialized)
            {
                _current = Length - 1;
                Entry entry;
                if (TryMatch(pattern, out entry)) return entry;
            }

            bool patternEmpty = string.IsNullOrWhiteSpace(pattern);
            if (!wasLive && patternEmpty)
            {
                //return the current entry again ( handles case up, up, enter, up)
                Entry entry;
                if (TryMatch(pattern, out entry)) return entry;
            }

            for (_current--; _current >= 0; _current--)
            {
                Entry entry;
                if (TryMatch(pattern, out entry)) return entry;
            }

            return null;
        }


        private bool TryMatch(string pattern, out Entry entry)
        {
            bool patternEmpty = string.IsNullOrWhiteSpace(pattern);
            var tmpEntry = _history[_current];
            if (patternEmpty || Matches(tmpEntry.Text, pattern))
            {
                entry = tmpEntry;
                return true;
            }
            else
            {
                entry = null;
                return false;
            }
        }

        private static bool Matches(string entry, string pattern)
        {
            return entry.Contains(pattern);
        }
    }
}
