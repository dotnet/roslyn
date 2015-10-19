// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        private static readonly Stopwatch s_stopwatch;

        private readonly InteractiveWindow _window;
        private readonly DispatcherTimer _timer;
        private readonly object _mutex;

        private Entry _firstEntry;
        private Entry _lastEntry;

        private long _lastFlushTimeMilliseconds;

        // the number of characters written to the buffer that trigger an auto-flush
        private int _flushThreshold;

        // the number of characters written to the buffer that haven't been flushed yet
        private int _unflushedLength;

        // the number of characters written to the output (doesn't reset on flush)
        private int _totalLength;

        private const int InitialFlushThreshold = 1024;
        private const int AutoFlushMilliseconds = 100;

        static OutputBuffer()
        {
            s_stopwatch = new Stopwatch();
            s_stopwatch.Start();
        }

        public OutputBuffer(InteractiveWindow window)
        {
            Reset();

            _mutex = new object();
            _window = window;

            _timer = new DispatcherTimer();
            _timer.Tick += (sender, args) => Flush();
            _timer.Interval = TimeSpan.FromMilliseconds(AutoFlushMilliseconds);
        }

        internal void Reset()
        {
            _firstEntry = _lastEntry = null;
            _totalLength = _unflushedLength = 0;
            _flushThreshold = InitialFlushThreshold;
        }

        /// <summary>
        /// Appends text to the end of the buffer. 
        /// </summary>
        /// <param name="text">Text to append.</param>
        /// <returns>Returns the position where this text is inserted relative to the buffer start.</returns>
        public int Write(string text)
        {
            int result = _totalLength;

            if (string.IsNullOrEmpty(text))
            {
                return result;
            }

            bool needsFlush = false;
            lock (_mutex)
            {
                AddEntry(text);

                needsFlush = _unflushedLength > _flushThreshold;
                if (!needsFlush && !_timer.IsEnabled)
                {
                    _timer.IsEnabled = true;
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

            lock (_mutex)
            {
                // if we're rapidly outputting grow the threshold
                long curTime = s_stopwatch.ElapsedMilliseconds;
                if (curTime - _lastFlushTimeMilliseconds < 1000)
                {
                    if (_flushThreshold < 1024 * 1024)
                    {
                        _flushThreshold *= 2;
                    }
                }

                _lastFlushTimeMilliseconds = s_stopwatch.ElapsedMilliseconds;

                if (_unflushedLength > 0)
                {
                    // normalize line breaks - the editor isn't happy about projections that cut "\r\n" line break in half:
                    if (_lastEntry.Text[_lastEntry.Text.Length - 1] == '\r')
                    {
                        AddEntry("\n");
                    }

                    firstEntryToFlush = _firstEntry;

                    _firstEntry = _lastEntry = null;
                    _unflushedLength = 0;
                }

                _timer.IsEnabled = false;
            }

            if (firstEntryToFlush != null)
            {
                _window.AppendOutput(GetEntries(firstEntryToFlush));
            }
        }

        private void AddEntry(string text)
        {
            var entry = new Entry(text);

            if (_firstEntry == null)
            {
                _firstEntry = _lastEntry = entry;
            }
            else
            {
                _lastEntry.Next = entry;
                _lastEntry = entry;
            }

            _totalLength += text.Length;
            _unflushedLength += text.Length;
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
            _timer.IsEnabled = false;
        }
    }
}
