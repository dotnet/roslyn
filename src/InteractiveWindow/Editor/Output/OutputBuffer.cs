using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Threading;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    /// <summary>
    /// Serializes and buffers output so that we avoid frequent switching to UI thread to write to the editor buffer.
    /// </summary>
    internal sealed class OutputBuffer : IDisposable
    {
        private sealed class Entry
        {
            public readonly string Text;
            public Entry Next;

            public Entry(string text)
            {
                Debug.Assert(!string.IsNullOrEmpty(text));
                Text = text;
            }
        }

        private static readonly Stopwatch stopwatch;

        private readonly InteractiveWindow window;
        private readonly DispatcherTimer timer;
        private readonly object mutex;

        private Entry firstEntry;
        private Entry lastEntry;

        private long lastFlushTimeMilliseconds;

        // the number of characters written to the buffer that trigger an auto-flush
        private int flushThreshold;

        // the number of characters written to the buffer that haven't been flushed yet
        private int unflushedLength;

        // the number of characters written to the output (doesn't reset on flush)
        private int totalLength;

        private const int InitialFlushThreshold = 1024;
        private const int AutoFlushMilliseconds = 100;

        static OutputBuffer()
        {
            stopwatch = new Stopwatch();
            stopwatch.Start();
        }

        public OutputBuffer(InteractiveWindow window)
        {
            Reset();

            this.mutex = new object();
            this.window = window;

            this.timer = new DispatcherTimer();
            this.timer.Tick += (sender, args) => Flush();
            this.timer.Interval = TimeSpan.FromMilliseconds(AutoFlushMilliseconds);
        }

        internal void Reset()
        {
            firstEntry = lastEntry = null;
            totalLength = unflushedLength = 0;
            flushThreshold = InitialFlushThreshold;
        }

        /// <summary>
        /// Appends text to the end of the buffer. 
        /// </summary>
        /// <param name="text">Text to append.</param>
        /// <returns>Returns the position where this text is inserted relative to the buffer start.</returns>
        public int Write(string text)
        {
            int result = totalLength;

            if (string.IsNullOrEmpty(text))
            {
                return result;
            }

            bool needsFlush = false;
            lock (mutex)
            {
                AddEntry(text);

                needsFlush = unflushedLength > flushThreshold;
                if (!needsFlush && !timer.IsEnabled)
                {
                    timer.IsEnabled = true;
                }
            }

            if (needsFlush)
            {
                Flush();
            }

            return result;
        }

        /// <summary>
        /// Flushes the buffer, should always be called from the UI thread.
        /// </summary>
        public void Flush()
        {
            Entry firstEntryToFlush = null;
            int flushLength = 0;

            lock (mutex)
            {
                // if we're rapidly outputting grow the threshold
                long curTime = stopwatch.ElapsedMilliseconds;
                if (curTime - lastFlushTimeMilliseconds < 1000)
                {
                    if (flushThreshold < 1024 * 1024)
                    {
                        flushThreshold *= 2;
                    }
                }

                lastFlushTimeMilliseconds = stopwatch.ElapsedMilliseconds;

                if (unflushedLength > 0)
                {
                    // normalize line breaks - the editor isn't happy about projections that cut "\r\n" line break in half:
                    if (lastEntry.Text[lastEntry.Text.Length - 1] == '\r')
                    {
                        AddEntry("\n");
                    }

                    firstEntryToFlush = firstEntry;
                    flushLength = unflushedLength;

                    firstEntry = lastEntry = null;
                    unflushedLength = 0;
                }

                timer.IsEnabled = false;
            }

            if (firstEntryToFlush != null)
            {
                window.UIThread(() => window.AppendOutput(GetEntries(firstEntryToFlush), flushLength));
            }
        }

        private void AddEntry(string text)
        {
            var entry = new Entry(text);

            if (firstEntry == null)
            {
                firstEntry = lastEntry = entry;
            }
            else
            {
                lastEntry.Next = entry;
                lastEntry = entry;
            }

            totalLength += text.Length;
            unflushedLength += text.Length;
        }

        private IEnumerable<string> GetEntries(Entry entry)
        {
            while (entry != null)
            {
                yield return entry.Text;
                entry = entry.Next;
            }
        }

        public void Dispose()
        {
            timer.IsEnabled = false;
        }
    }
}
