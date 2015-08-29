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

            //If you were in middle of navigating history and entered a new command at the 
            //prompt ( not the one that was returned by history navigation), then the history 
            //navigation pointer is reset. However if you submit a command that you got from history then
            //the history navigation pointer is maintained and subsequent navigation starts from
            //the same pointer location
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

        internal Entry GetNext(string pattern)
        {
            return Get(pattern, step: +1);
        }

        internal Entry GetPrevious(string pattern)
        {
            return Get(pattern, step: -1);
        }

        private Entry Move(string pattern, int step)
        {
            Debug.Assert(step == -1 || step == +1);

            int end;
            bool wasLive = _live;
            bool wasUninitialized = (_current == -1);
            bool patternEmpty = string.IsNullOrWhiteSpace(pattern);

            _live = true;
            if (Length == 0)
            {
                return null;
            }

            if (step > 0)
            {
                end = Length - 1;
                if (wasUninitialized) return null;
             
                while (true)
                {
                    if (_current == end)
                    {
                        return null;
                    }
                    else
                    {
                        _current += step;
                    }

                    Entry entry;
                    if (TryMatch(pattern, out entry)) return entry;
                }

            }
            else  //step == -1
            {
                end = 0;
                if (wasUninitialized)
                {
                    _current = Length - 1;
                    Entry entry;
                    if (TryMatch(pattern, out entry)) return entry;
                }

                while (true)
                {
                    if (_current == end)
                    {
                        return null;
                    }
                    else if (!wasLive && patternEmpty)
                    {
                        // if history pointer was uninitialized then add step else do nothing.
                        // We want to return the same history entry again in cases like up, up, up, enter, up
                        if(wasUninitialized) _current += step;
                    }
                    else
                    {
                        _current += step;
                    }

                    Entry entry;
                    if (TryMatch(pattern, out entry)) return entry;
                }
            }
        }

        private bool TryMatch(string pattern, out Entry entry)
        {
            bool patternEmpty = string.IsNullOrWhiteSpace(pattern);
            var _entry = _history[_current];
            if (patternEmpty || Matches(_entry.Text, pattern))
            {
                entry = _entry;
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
