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
            private string cachedText;

            /// <summary>
            /// The span of the original submission of this text.
            /// </summary>
            private SnapshotSpan? originalSpan;

            public bool Command { get; set; }
            public bool Failed { get; set; }

            public SnapshotSpan? OriginalSpan { get { return originalSpan; } }

            public string Text
            {
                get
                {
                    if (cachedText != null)
                    {
                        return cachedText;
                    }

                    return originalSpan.Value.GetText();
                }
            }

            internal void ForgetOriginalBuffer()
            {
                if (originalSpan.HasValue)
                {
                    cachedText = originalSpan.Value.GetText();
                    originalSpan = null;
                }
            }

            public Entry(SnapshotSpan span)
            {
                this.originalSpan = span;
            }
        }

        private readonly List<Entry> history;
        private readonly int maxLength;

        private int current;
        private bool live;

        internal string UncommittedInput { get; set; }

        internal History()
            : this(maxLength: 50)
        {
        }

        internal History(int maxLength)
        {
            this.maxLength = maxLength;
            this.current = -1;
            this.history = new List<Entry>();
        }

        internal void Clear()
        {
            current = -1;
            live = false;
            history.Clear();
        }

        internal void ForgetOriginalBuffers()
        {
            foreach (var entry in history)
            {
                entry.ForgetOriginalBuffer();
            }
        }

        internal int MaxLength
        {
            get { return maxLength; }
        }

        internal int Length
        {
            get { return history.Count; }
        }

        internal IEnumerable<Entry> Items
        {
            get { return history; }
        }

        internal Entry Last
        {
            get
            {
                if (history.Count > 0)
                {
                    return history[history.Count - 1];
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

            live = false;
            if (Length == 0 || Last.Text != text)
            {
                history.Add(entry);
            }

            if (history[(current == -1) ? Length - 1 : current].Text != text)
            {
                current = -1;
            }

            if (Length > MaxLength)
            {
                history.RemoveAt(0);
                if (current > 0)
                {
                    current--;
                }
            }
        }

        private Entry Get(string pattern, int step)
        {
            var startPos = current;
            var next = Move(pattern, step);
            if (next == null)
            {
                current = startPos;
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

            bool wasLive = live;
            live = true;

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
            if (current == -1)
            {
                current = start;
            }

            int visited = 0;
            while (true)
            {
                if (current == end)
                {
                    if (visited < Length)
                    {
                        current = start;
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
                    current += step;
                }

                var entry = history[current];
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
