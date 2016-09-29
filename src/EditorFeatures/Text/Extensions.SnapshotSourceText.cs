// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    public static partial class Extensions
    {
        /// <summary>
        /// ITextSnapshot implementation of SourceText
        /// </summary>
        private class SnapshotSourceText : SourceText
        {
            /// <summary>
            /// Use a separate class for closed files to simplify memory leak investigations
            /// </summary>
            internal sealed class ClosedSnapshotSourceText : SnapshotSourceText
            {
                public ClosedSnapshotSourceText(ITextSnapshot roslynSnapshot, Encoding encodingOpt)
                    : base(roslynSnapshot, encodingOpt, containerOpt: null)
                {
                }
            }

            private static readonly Func<int, int, string> s_textLog = (v1, v2) => string.Format("FullRange : from {0} to {1}", v1, v2);

            /// <summary>
            /// The ITextSnapshot backing the SourceText instance
            /// </summary>
            protected readonly ITextSnapshot RoslynSnapshot;
            private readonly Encoding _encodingOpt;
            private readonly TextBufferContainer _containerOpt;
            private readonly int _reiteratedVersion;

            private SnapshotSourceText(ITextSnapshot editorSnapshot, Encoding encodingOpt)
            {
                Contract.ThrowIfNull(editorSnapshot);

                this.RoslynSnapshot = TextBufferMapper.ToRoslyn(editorSnapshot);
                _containerOpt = TextBufferContainer.From(editorSnapshot.TextBuffer);
                _reiteratedVersion = editorSnapshot.Version.ReiteratedVersionNumber;
                _encodingOpt = encodingOpt;
            }

            public SnapshotSourceText(ITextSnapshot roslynSnapshot, Encoding encodingOpt, TextBufferContainer containerOpt)
            {
                Contract.ThrowIfNull(roslynSnapshot);

                this.RoslynSnapshot = roslynSnapshot;
                _encodingOpt = encodingOpt;
                _containerOpt = containerOpt;
            }

            /// <summary>
            /// A weak map of all Editor ITextSnapshots and their associated SourceText
            /// </summary>
            private static readonly ConditionalWeakTable<ITextSnapshot, SnapshotSourceText> s_textSnapshotMap = new ConditionalWeakTable<ITextSnapshot, SnapshotSourceText>();
            private static readonly ConditionalWeakTable<ITextSnapshot, SnapshotSourceText>.CreateValueCallback s_createTextCallback = CreateText;

            public static SourceText From(ITextSnapshot editorSnapshot)
            {
                if (editorSnapshot == null)
                {
                    throw new ArgumentNullException(nameof(editorSnapshot));
                }

                return s_textSnapshotMap.GetValue(editorSnapshot, s_createTextCallback);
            }

            // Use this as a secondary cache to catch ITextSnapshots that have the same ReiteratedVersionNumber as a previously created SnapshotSourceText
            private static readonly ConditionalWeakTable<ITextBuffer, StrongBox<SnapshotSourceText>> s_textBufferLatestSnapshotMap = new ConditionalWeakTable<ITextBuffer, StrongBox<SnapshotSourceText>>();

            private static SnapshotSourceText CreateText(ITextSnapshot editorSnapshot)
            {
                var strongBox = s_textBufferLatestSnapshotMap.GetOrCreateValue(editorSnapshot.TextBuffer);
                var text = strongBox.Value;
                if (text != null && text._reiteratedVersion == editorSnapshot.Version.ReiteratedVersionNumber)
                {
                    if (text.Length == editorSnapshot.Length)
                    {
                        return text;
                    }
                    else
                    {
                        // In editors with non-compliant Undo/Redo implementations, you can end up
                        // with two Versions with the same ReiteratedVersionNumber but with very
                        // different text. We've never provably seen this problem occur in Visual 
                        // Studio, but we have seen crashes that look like they could have been
                        // caused by incorrect results being returned from this cache. 
                        try
                        {
                            throw new InvalidOperationException(
                                $"The matching cached SnapshotSourceText with <Reiterated Version, Length> = <{text._reiteratedVersion}, {text.Length}> " +
                                $"does not match the given editorSnapshot with <{editorSnapshot.Version.ReiteratedVersionNumber}, {editorSnapshot.Length}>");
                        }
                        catch (Exception e) when (FatalError.ReportWithoutCrash(e))
                        {
                        }
                    }
                }

                text = new SnapshotSourceText(editorSnapshot, editorSnapshot.TextBuffer.GetEncodingOrUTF8());
                strongBox.Value = text;
                return text;
            }

            public override Encoding Encoding
            {
                get { return _encodingOpt; }
            }

            public ITextSnapshot EditorSnapshot
            {
                get { return TextBufferMapper.ToEditor(this.RoslynSnapshot); }
            }

            protected static ITextBufferCloneService TextBufferFactory
            {
                get
                {
                    // simplest way to get text factory
                    var ws = PrimaryWorkspace.Workspace;
                    if (ws != null)
                    {
                        return ws.Services.GetService<ITextBufferCloneService>();
                    }

                    return null;
                }
            }

            public override SourceTextContainer Container
            {
                get
                {
                    return _containerOpt ?? base.Container;
                }
            }

            public override int Length
            {
                get
                {
                    var res = this.RoslynSnapshot.Length;
                    return res;
                }
            }

            public override char this[int position]
            {
                get { return this.RoslynSnapshot[position]; }
            }

            #region Lines
            protected override TextLineCollection GetLinesCore()
            {
                return new LineInfo(this);
            }

            private class LineInfo : TextLineCollection
            {
                private readonly SnapshotSourceText _text;

                public LineInfo(SnapshotSourceText text)
                {
                    _text = text;
                }

                public override int Count
                {
                    get { return _text.RoslynSnapshot.LineCount; }
                }

                public override TextLine this[int index]
                {
                    get
                    {
                        var line = _text.RoslynSnapshot.GetLineFromLineNumber(index);
                        return TextLine.FromSpan(_text, TextSpan.FromBounds(line.Start, line.End));
                    }
                }

                public override int IndexOf(int position)
                {
                    return _text.RoslynSnapshot.GetLineNumberFromPosition(position);
                }

                public override TextLine GetLineFromPosition(int position)
                {
                    return this[this.IndexOf(position)];
                }

                public override LinePosition GetLinePosition(int position)
                {
                    ITextSnapshotLine textLine = _text.RoslynSnapshot.GetLineFromPosition(position);
                    return new LinePosition(textLine.LineNumber, position - textLine.Start);
                }
            }
            #endregion

            public override string ToString()
            {
                return this.RoslynSnapshot.GetText();
            }

            public override string ToString(TextSpan textSpan)
            {
                var editorSpan = new Span(textSpan.Start, textSpan.Length);
                var res = this.RoslynSnapshot.GetText(editorSpan);
                return res;
            }

            public override SourceText WithChanges(IEnumerable<TextChange> changes)
            {
                if (changes == null)
                {
                    throw new ArgumentNullException(nameof(changes));
                }

                if (!changes.Any())
                {
                    return this;
                }

                // check whether we can use text buffer factory
                var factory = TextBufferFactory;
                if (factory == null)
                {
                    // if we can't get the factory, use the default implementation
                    return base.WithChanges(changes);
                }

                // otherwise, create a new cloned snapshot
                var buffer = factory.Clone(RoslynSnapshot.GetFullSpan());
                var baseSnapshot = buffer.CurrentSnapshot;

                // apply the change to the buffer
                using (var edit = buffer.CreateEdit())
                {
                    foreach (var change in changes)
                    {
                        edit.Replace(change.Span.ToSpan(), change.NewText);
                    }

                    edit.Apply();
                }

                return new ChangedSourceText(this, baseSnapshot, buffer.CurrentSnapshot);
            }

            /// <summary>
            /// Perf: Optimize calls to GetChangeRanges after WithChanges by using editor snapshots
            /// </summary>
            private class ChangedSourceText : SnapshotSourceText
            {
                private readonly SnapshotSourceText _baseText;
                private readonly ITextSnapshot _baseSnapshot;

                public ChangedSourceText(SnapshotSourceText baseText, ITextSnapshot baseSnapshot, ITextSnapshot currentSnapshot)
                    : base(currentSnapshot, baseText.Encoding, containerOpt: null)
                {
                    _baseText = baseText;
                    _baseSnapshot = baseSnapshot;
                }

                public override IReadOnlyList<TextChangeRange> GetChangeRanges(SourceText oldText)
                {
                    if (oldText == null)
                    {
                        throw new ArgumentNullException(nameof(oldText));
                    }

                    // if they are the same text there is no change.
                    if (oldText == this)
                    {
                        return TextChangeRange.NoChanges;
                    }

                    if (oldText != _baseText)
                    {
                        return new[] { new TextChangeRange(new TextSpan(0, oldText.Length), this.Length) };
                    }

                    return GetChangeRanges(_baseSnapshot, _baseSnapshot.Length, this.RoslynSnapshot);
                }
            }

            public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
            {
                this.RoslynSnapshot.CopyTo(sourceIndex, destination, destinationIndex, count);
            }

            public override void Write(TextWriter textWriter, TextSpan span, CancellationToken cancellationToken)
            {
                this.RoslynSnapshot.Write(textWriter, span.ToSpan());
            }

            #region GetChangeRangesImplementation 

            public override IReadOnlyList<TextChangeRange> GetChangeRanges(SourceText oldText)
            {
                if (oldText == null)
                {
                    throw new ArgumentNullException(nameof(oldText));
                }

                // if they are the same text there is no change.
                if (oldText == this)
                {
                    return TextChangeRange.NoChanges;
                }

                // first, check whether the text buffer is still alive.
                var container = this.Container as TextBufferContainer;
                if (container != null)
                {
                    var lastEventArgs = container.LastEventArgs;
                    if (lastEventArgs != null && lastEventArgs.OldText == oldText && lastEventArgs.NewText == this)
                    {
                        return lastEventArgs.Changes;
                    }
                }

                var oldSnapshot = oldText.FindCorrespondingEditorTextSnapshot();
                var newSnapshot = this.FindCorrespondingEditorTextSnapshot();
                return GetChangeRanges(oldSnapshot, oldText.Length, newSnapshot);
            }

            private IReadOnlyList<TextChangeRange> GetChangeRanges(ITextSnapshot oldSnapshot, int oldTextLength, ITextSnapshot newSnapshot)
            {
                if (oldSnapshot == null ||
                    newSnapshot == null ||
                    oldSnapshot.TextBuffer != newSnapshot.TextBuffer)
                {
                    // Claim its all changed
                    Logger.Log(FunctionId.Workspace_SourceText_GetChangeRanges, "Invalid Snapshots");
                    return ImmutableArray.Create<TextChangeRange>(new TextChangeRange(new TextSpan(0, oldTextLength), this.Length));
                }
                else if (oldSnapshot.Version.ReiteratedVersionNumber == newSnapshot.Version.ReiteratedVersionNumber)
                {
                    // content of two snapshot must be same even if versions are different
                    return TextChangeRange.NoChanges;
                }
                else
                {
                    return GetChangeRanges(oldSnapshot, newSnapshot, forward: oldSnapshot.Version.VersionNumber <= newSnapshot.Version.VersionNumber);
                }
            }

            private static readonly Func<ITextChange, TextChangeRange> s_forwardTextChangeRange = c => CreateTextChangeRange(c, forward: true);
            private static readonly Func<ITextChange, TextChangeRange> s_backwardTextChangeRange = c => CreateTextChangeRange(c, forward: false);

            private IReadOnlyList<TextChangeRange> GetChangeRanges(ITextSnapshot snapshot1, ITextSnapshot snapshot2, bool forward)
            {
                var oldSnapshot = forward ? snapshot1 : snapshot2;
                var newSnapshot = forward ? snapshot2 : snapshot1;

                INormalizedTextChangeCollection changes = null;
                for (var oldVersion = oldSnapshot.Version;
                    oldVersion != newSnapshot.Version;
                    oldVersion = oldVersion.Next)
                {
                    if (oldVersion.Changes.Count != 0)
                    {
                        if (changes != null)
                        {
                            // Oops - more than one "textual" change between these snapshots, bail and try to find smallest changes span
                            Logger.Log(FunctionId.Workspace_SourceText_GetChangeRanges, s_textLog, snapshot1.Version.VersionNumber, snapshot2.Version.VersionNumber);

                            return new[] { GetChangeRanges(oldSnapshot.Version, newSnapshot.Version, forward) };
                        }
                        else
                        {
                            changes = oldVersion.Changes;
                        }
                    }
                }

                if (changes == null)
                {
                    return ImmutableArray.Create<TextChangeRange>();
                }
                else
                {
                    return ImmutableArray.CreateRange(changes.Select(forward ? s_forwardTextChangeRange : s_backwardTextChangeRange));
                }
            }

            private TextChangeRange GetChangeRanges(ITextVersion oldVersion, ITextVersion newVersion, bool forward)
            {
                TextChangeRange? range = null;
                var iterator = GetMultipleVersionTextChanges(oldVersion, newVersion, forward);
                foreach (var changes in forward ? iterator : iterator.Reverse())
                {
                    range = range.Accumulate(changes);
                }

                Contract.Requires(range.HasValue);
                return range.Value;
            }

            private static IEnumerable<IEnumerable<TextChangeRange>> GetMultipleVersionTextChanges(ITextVersion oldVersion, ITextVersion newVersion, bool forward)
            {
                for (var version = oldVersion; version != newVersion; version = version.Next)
                {
                    yield return version.Changes.Select(forward ? s_forwardTextChangeRange : s_backwardTextChangeRange);
                }
            }

            private static TextChangeRange CreateTextChangeRange(ITextChange change, bool forward)
            {
                if (forward)
                {
                    return new TextChangeRange(new TextSpan(change.OldSpan.Start, change.OldSpan.Length), change.NewLength);
                }
                else
                {
                    return new TextChangeRange(new TextSpan(change.NewSpan.Start, change.NewSpan.Length), change.OldLength);
                }
            }
            #endregion
        }
    }
}
