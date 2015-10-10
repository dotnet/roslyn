// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.VisualStudio.InteractiveWindow.Commands;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.InteractiveWindow.UnitTests
{
    public class HistoryTests : IDisposable
    {
        private readonly InteractiveWindowTestHost _testHost;
        private readonly ITextBuffer _buffer;
        private readonly History _history;

        public HistoryTests()
        {
            _testHost = new InteractiveWindowTestHost();
            _buffer = _testHost.ExportProvider.GetExport<ITextBufferFactoryService>().Value.CreateTextBuffer();
            _history = new History();
        }

        void IDisposable.Dispose()
        {
            _testHost.Dispose();
        }

        [WpfFact]
        public void TestClear()
        {
            AddEntries("1", "2", "3");
            Assert.Equal(3, _history.Length);
            Assert.Equal(3, HistoryEntries.Count());

            _history.Clear();
            Assert.Equal(0, _history.Length);
            Assert.Empty(HistoryEntries);

            AddEntries("1", "2");
            Assert.Equal(2, _history.Length);
            Assert.Equal(2, HistoryEntries.Count());
        }

        [WpfFact]
        public void TestMaxLength()
        {
            var buffer = _testHost.ExportProvider.GetExport<ITextBufferFactoryService>().Value.CreateTextBuffer();
            buffer.Insert(0, "0123456789");
            var snapshot = buffer.CurrentSnapshot;

            var history = new History(maxLength: 0);

            history.Add(new SnapshotSpan(snapshot, new Span(0, 1)));
            Assert.Empty(GetHistoryEntries(history));

            history = new History(maxLength: 1);
            history.Add(new SnapshotSpan(snapshot, new Span(0, 1)));
            Assert.Equal(new[] { "0" }, GetHistoryEntries(history));
            history.Add(new SnapshotSpan(snapshot, new Span(1, 1)));
            Assert.Equal(new[] { "1" }, GetHistoryEntries(history)); // Oldest entry is dropped.

            history = new History(maxLength: 2);
            history.Add(new SnapshotSpan(snapshot, new Span(0, 1)));
            Assert.Equal(new[] { "0" }, GetHistoryEntries(history));
            history.Add(new SnapshotSpan(snapshot, new Span(1, 1)));
            Assert.Equal(new[] { "0", "1" }, GetHistoryEntries(history));
            history.Add(new SnapshotSpan(snapshot, new Span(2, 1)));
            Assert.Equal(new[] { "1", "2" }, GetHistoryEntries(history)); // Oldest entry is dropped.
        }

        [WpfFact]
        public void TestLast()
        {
            for (int i = 0; i < 3; i++)
            {
                var text = i.ToString();
                AddEntries(text);
                Assert.Equal(text, _history.Last.Text);
            }
        }

        [WpfFact]
        public void TestForgetOriginalBuffers()
        {
            var entries = new[] { "1", "2", "3" };
            AddEntries(entries);

            Assert.Equal(entries, HistoryEntries);
            AssertEx.All(_history.Items, e => e.OriginalSpan.HasValue);

            _history.ForgetOriginalBuffers();

            Assert.Equal(entries, HistoryEntries);
            AssertEx.None(_history.Items, e => e.OriginalSpan.HasValue);
        }

        [WpfFact]
        public void TestDuplicateEntries()
        {
            AddEntries("1", "1", "2", "2", "1", "2", "2", "1");
            Assert.Equal(new[] { "1", "2", "1", "2", "1" }, HistoryEntries);
        }

        [WpfFact]
        public void TestPrevious()
        {
            AddEntries("1", "2", "3");

            CheckHistoryText(_history.GetPrevious(null), "3");
            CheckHistoryText(_history.GetPrevious(null), "2");
            CheckHistoryText(_history.GetPrevious(null), "1");
            CheckHistoryText(_history.GetPrevious(null), null);
            CheckHistoryText(_history.GetPrevious(null), null);
        }

        [WpfFact]
        public void TestNext()
        {
            AddEntries("1", "2", "3");

            CheckHistoryText(_history.GetNext(null), null);
            CheckHistoryText(_history.GetNext(null), null);
            CheckHistoryText(_history.GetPrevious(null), "3");
            CheckHistoryText(_history.GetPrevious(null), "2");
            CheckHistoryText(_history.GetPrevious(null), "1");
            CheckHistoryText(_history.GetPrevious(null), null);
            CheckHistoryText(_history.GetNext(null), "2");
            CheckHistoryText(_history.GetNext(null), "3");
            CheckHistoryText(_history.GetNext(null), null);
            CheckHistoryText(_history.GetNext(null), null);
        }

        [WpfFact]
        public void TestPreviousWithPattern_NoMatch()
        {
            AddEntries("123", "12", "1");

            CheckHistoryText(_history.GetPrevious("4"), null);
            CheckHistoryText(_history.GetPrevious("4"), null);
        }

        [WpfFact]
        public void TestPreviousWithPattern_PatternMaintained()
        {
            AddEntries("123", "12", "1");

            CheckHistoryText(_history.GetPrevious("12"), "12"); // Skip over non-matching entry.
            CheckHistoryText(_history.GetPrevious("12"), "123");
            CheckHistoryText(_history.GetPrevious("12"), null);
        }

        [WpfFact]
        public void TestPreviousWithPattern_PatternDropped()
        {
            AddEntries("1", "2", "3");

            CheckHistoryText(_history.GetPrevious("2"), "2"); // Skip over non-matching entry.
            CheckHistoryText(_history.GetPrevious(null), "1"); // Pattern isn't passed, so return to normal iteration.
            CheckHistoryText(_history.GetPrevious(null), null);
        }

        [WpfFact]
        public void TestPreviousWithPattern_PatternChanged()
        {
            AddEntries("1a", "2a", "1b", "2b");

            CheckHistoryText(_history.GetPrevious("1"), "1b"); // Skip over non-matching entry.
            CheckHistoryText(_history.GetPrevious("2"), "2a"); // Skip over non-matching entry.
            CheckHistoryText(_history.GetPrevious("2"), null);
        }

        [WpfFact]
        public void TestNextWithPattern_NoMatch()
        {
            AddEntries("start", "1", "12", "123");

            CheckHistoryText(_history.GetPrevious(null), "123");
            CheckHistoryText(_history.GetPrevious(null), "12");
            CheckHistoryText(_history.GetPrevious(null), "1");
            CheckHistoryText(_history.GetPrevious(null), "start");

            CheckHistoryText(_history.GetNext("4"), null);
            CheckHistoryText(_history.GetNext("4"), null);
        }

        [WpfFact]
        public void TestNextWithPattern_PatternMaintained()
        {
            AddEntries("start", "1", "12", "123");

            CheckHistoryText(_history.GetPrevious(null), "123");
            CheckHistoryText(_history.GetPrevious(null), "12");
            CheckHistoryText(_history.GetPrevious(null), "1");
            CheckHistoryText(_history.GetPrevious(null), "start");

            CheckHistoryText(_history.GetNext("12"), "12"); // Skip over non-matching entry.
            CheckHistoryText(_history.GetNext("12"), "123");
            CheckHistoryText(_history.GetNext("12"), null);
        }

        [WpfFact]
        public void TestNextWithPattern_PatternDropped()
        {
            AddEntries("start", "3", "2", "1");

            CheckHistoryText(_history.GetPrevious(null), "1");
            CheckHistoryText(_history.GetPrevious(null), "2");
            CheckHistoryText(_history.GetPrevious(null), "3");
            CheckHistoryText(_history.GetPrevious(null), "start");

            CheckHistoryText(_history.GetNext("2"), "2"); // Skip over non-matching entry.
            CheckHistoryText(_history.GetNext(null), "1"); // Pattern isn't passed, so return to normal iteration.
            CheckHistoryText(_history.GetNext(null), null);
        }

        [WpfFact]
        public void TestNextWithPattern_PatternChanged()
        {
            AddEntries("start", "2b", "1b", "2a", "1a");

            CheckHistoryText(_history.GetPrevious(null), "1a");
            CheckHistoryText(_history.GetPrevious(null), "2a");
            CheckHistoryText(_history.GetPrevious(null), "1b");
            CheckHistoryText(_history.GetPrevious(null), "2b");
            CheckHistoryText(_history.GetPrevious(null), "start");

            CheckHistoryText(_history.GetNext("1"), "1b"); // Skip over non-matching entry.
            CheckHistoryText(_history.GetNext("2"), "2a"); // Skip over non-matching entry.
            CheckHistoryText(_history.GetNext("2"), null);
        }

        private void AddEntries(params string[] entries)
        {
            var oldLength = BufferLength;

            foreach (var entry in entries)
            {
                AddEntry(entry);
            }

            Assert.Equal(string.Join(Environment.NewLine, entries) + Environment.NewLine, _buffer.CurrentSnapshot.GetText().Substring(oldLength));
        }

        private void AddEntry(string entry)
        {
            var oldLength = BufferLength;
            var snapshot = _buffer.Insert(oldLength, entry);
            var snapshotSpan = new SnapshotSpan(snapshot, new Span(oldLength, entry.Length));
            _history.Add(snapshotSpan);
            _buffer.Insert(snapshot.Length, Environment.NewLine);
        }

        private int BufferLength => _buffer.CurrentSnapshot.Length;

        private IEnumerable<string> HistoryEntries => GetHistoryEntries(_history);

        private static IEnumerable<string> GetHistoryEntries(History history)
        {
            return history.Items.Select(e => e.Text);
        }

        private void CheckHistoryText(History.Entry actualEntry, string expectedText)
        {
            Assert.Equal(expectedText, actualEntry?.Text);
        }
    }
}