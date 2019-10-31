// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
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
            private static readonly Func<int, int, string> s_textLog = (v1, v2) => string.Format("FullRange : from {0} to {1}", v1, v2);

            /// <summary>
            /// The <see cref="ITextImage"/> backing the SourceText instance
            /// </summary>
            public readonly ITextImage TextImage;

            private readonly ITextBufferCloneService? _textBufferCloneServiceOpt;

            private readonly Encoding? _encodingOpt;
            private readonly TextBufferContainer? _containerOpt;

            private SnapshotSourceText(ITextBufferCloneService? textBufferCloneServiceOpt, ITextSnapshot editorSnapshot, TextBufferContainer container)
            {
                Contract.ThrowIfNull(editorSnapshot);

                _textBufferCloneServiceOpt = textBufferCloneServiceOpt;
                this.TextImage = RecordReverseMapAndGetImage(editorSnapshot);
                _encodingOpt = editorSnapshot.TextBuffer.GetEncodingOrUTF8();
                _containerOpt = container;
            }

            public SnapshotSourceText(ITextBufferCloneService? textBufferCloneServiceOpt, ITextImage textImage, Encoding? encodingOpt, TextBufferContainer? containerOpt)
            {
                Contract.ThrowIfNull(textImage);

                _textBufferCloneServiceOpt = textBufferCloneServiceOpt;
                this.TextImage = textImage;
                _encodingOpt = encodingOpt;
                _containerOpt = containerOpt;
            }

            /// <summary>
            /// A weak map of all Editor ITextSnapshots and their associated SourceText
            /// </summary>
            private static readonly ConditionalWeakTable<ITextSnapshot, SnapshotSourceText> s_textSnapshotMap = new ConditionalWeakTable<ITextSnapshot, SnapshotSourceText>();

            /// <summary>
            /// Reverse map of roslyn text to editor snapshot. unlike forward map, this doesn't strongly hold onto editor snapshot so that 
            /// we don't leak editor snapshot which should go away once editor is closed. roslyn source's lifetime is not usually tied to view.
            /// </summary>
            private static readonly ConditionalWeakTable<ITextImage, WeakReference<ITextSnapshot>> s_textImageToEditorSnapshotMap = new ConditionalWeakTable<ITextImage, WeakReference<ITextSnapshot>>();

            public static SourceText From(ITextBufferCloneService? textBufferCloneServiceOpt, ITextSnapshot editorSnapshot)
            {
                if (editorSnapshot == null)
                {
                    throw new ArgumentNullException(nameof(editorSnapshot));
                }

                if (!s_textSnapshotMap.TryGetValue(editorSnapshot, out var snapshot))
                {
                    // Explicitly obtain the TextBufferContainer before calling GetValue to avoid reentrancy in
                    // ConditionalWeakTable. https://github.com/dotnet/roslyn/issues/28256
                    var container = TextBufferContainer.From(editorSnapshot.TextBuffer);

                    // Avoid capturing `textBufferCloneServiceOpt` on the fast path
                    var tempTextBufferCloneServiceOpt = textBufferCloneServiceOpt;
                    snapshot = s_textSnapshotMap.GetValue(editorSnapshot, s => new SnapshotSourceText(tempTextBufferCloneServiceOpt, s, container));
                }

                return snapshot;
            }

            /// <summary>
            /// This only exist to break circular dependency on creating buffer. nobody except extension itself should use it
            /// </summary>
            internal static SourceText From(ITextBufferCloneService? textBufferCloneServiceOpt, ITextSnapshot editorSnapshot, TextBufferContainer container)
            {
                if (editorSnapshot == null)
                {
                    throw new ArgumentNullException(nameof(editorSnapshot));
                }

                Contract.ThrowIfFalse(editorSnapshot.TextBuffer == container.GetTextBuffer());
                return s_textSnapshotMap.GetValue(editorSnapshot, s => new SnapshotSourceText(textBufferCloneServiceOpt, s, container));
            }

            public override Encoding? Encoding
            {
                get { return _encodingOpt; }
            }

            public ITextSnapshot? TryFindEditorSnapshot()
                => TryFindEditorSnapshot(this.TextImage);

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
                    var res = this.TextImage.Length;
                    return res;
                }
            }

            public override char this[int position]
            {
                get { return this.TextImage[position]; }
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
                    get { return _text.TextImage.LineCount; }
                }

                public override TextLine this[int index]
                {
                    get
                    {
                        var line = _text.TextImage.GetLineFromLineNumber(index);
                        return TextLine.FromSpan(_text, TextSpan.FromBounds(line.Start, line.End));
                    }
                }

                public override int IndexOf(int position)
                {
                    return _text.TextImage.GetLineNumberFromPosition(position);
                }

                public override TextLine GetLineFromPosition(int position)
                {
                    return this[this.IndexOf(position)];
                }

                public override LinePosition GetLinePosition(int position)
                {
                    var textLine = _text.TextImage.GetLineFromPosition(position);
                    return new LinePosition(textLine.LineNumber, position - textLine.Start);
                }
            }
            #endregion

            public override string ToString()
            {
                return this.TextImage.GetText();
            }

            public override string ToString(TextSpan textSpan)
            {
                var editorSpan = new Span(textSpan.Start, textSpan.Length);
                var res = this.TextImage.GetText(editorSpan);
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
                var factory = _textBufferCloneServiceOpt;
                if (factory == null)
                {
                    // if we can't get the factory, use the default implementation
                    return base.WithChanges(changes);
                }

                // otherwise, create a new cloned snapshot
                var buffer = factory.CloneWithUnknownContentType(TextImage);
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

                return new ChangedSourceText(
                    textBufferCloneServiceOpt: _textBufferCloneServiceOpt,
                    baseText: this,
                    baseSnapshot: ((ITextSnapshot2)baseSnapshot).TextImage,
                    currentSnapshot: ((ITextSnapshot2)buffer.CurrentSnapshot).TextImage);
            }

            private static ITextImage RecordReverseMapAndGetImage(ITextSnapshot editorSnapshot)
            {
                Contract.ThrowIfNull(editorSnapshot);

                var textImage = ((ITextSnapshot2)editorSnapshot).TextImage;
                Contract.ThrowIfNull(textImage);

                // If we're already in the map, there's nothing to update.  Do a quick check
                // to avoid two allocations per call to RecordTextSnapshotAndGetImage.
                if (!s_textImageToEditorSnapshotMap.TryGetValue(textImage, out var weakReference))
                {
                    // put reverse entry that won't hold onto anything
                    weakReference = s_textImageToEditorSnapshotMap.GetValue(
                        textImage, _ => new WeakReference<ITextSnapshot>(editorSnapshot));
                }

#if DEBUG
                // forward and reversed map is 1:1 map. snapshot can't be different
                var snapshot = weakReference.GetTarget();
                Contract.ThrowIfFalse(snapshot == editorSnapshot);
#endif
                return textImage;
            }

            private static ITextSnapshot? TryFindEditorSnapshot(ITextImage textImage)
            {
                if (!s_textImageToEditorSnapshotMap.TryGetValue(textImage, out var weakReference) ||
                    !weakReference.TryGetTarget(out var editorSnapshot))
                {
                    return null;
                }

                return editorSnapshot;
            }

            /// <summary>
            /// Use a separate class for closed files to simplify memory leak investigations
            /// </summary>
            internal sealed class ClosedSnapshotSourceText : SnapshotSourceText
            {
                public ClosedSnapshotSourceText(ITextBufferCloneService? textBufferCloneServiceOpt, ITextImage textImage, Encoding? encodingOpt)
                    : base(textBufferCloneServiceOpt, textImage, encodingOpt, containerOpt: null)
                {
                }
            }

            /// <summary>
            /// Perf: Optimize calls to GetChangeRanges after WithChanges by using editor snapshots
            /// </summary>
            private class ChangedSourceText : SnapshotSourceText
            {
                private readonly SnapshotSourceText _baseText;
                private readonly ITextImage _baseSnapshot;

                public ChangedSourceText(ITextBufferCloneService? textBufferCloneServiceOpt, SnapshotSourceText baseText, ITextImage baseSnapshot, ITextImage currentSnapshot)
                    : base(textBufferCloneServiceOpt, currentSnapshot, baseText.Encoding, containerOpt: null)
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

                    return GetChangeRanges(_baseSnapshot, _baseSnapshot.Length, this.TextImage);
                }
            }

            public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
            {
                this.TextImage.CopyTo(sourceIndex, destination, destinationIndex, count);
            }

            public override void Write(TextWriter textWriter, TextSpan span, CancellationToken cancellationToken)
            {
                this.TextImage.Write(textWriter, span.ToSpan());
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
                if (this.Container is TextBufferContainer container)
                {
                    var lastEventArgs = container.LastEventArgs;
                    if (lastEventArgs != null && lastEventArgs.OldText == oldText && lastEventArgs.NewText == this)
                    {
                        return lastEventArgs.Changes;
                    }
                }

                var oldSnapshot = oldText.TryFindCorrespondingEditorTextImage();
                var newSnapshot = this.TryFindCorrespondingEditorTextImage();
                return GetChangeRanges(oldSnapshot, oldText.Length, newSnapshot);
            }

            private IReadOnlyList<TextChangeRange> GetChangeRanges(ITextImage? oldImage, int oldTextLength, ITextImage? newImage)
            {
                if (oldImage == null ||
                    newImage == null ||
                    oldImage.Version.Identifier != newImage.Version.Identifier)
                {
                    // Claim its all changed
                    Logger.Log(FunctionId.Workspace_SourceText_GetChangeRanges, "Invalid Snapshots");
                    return ImmutableArray.Create(new TextChangeRange(new TextSpan(0, oldTextLength), this.Length));
                }
                else if (AreSameReiteratedVersion(oldImage, newImage))
                {
                    // content of two snapshot must be same even if versions are different
                    return TextChangeRange.NoChanges;
                }
                else
                {
                    return GetChangeRanges(oldImage, newImage, forward: oldImage.Version.VersionNumber <= newImage.Version.VersionNumber);
                }
            }

            private static bool AreSameReiteratedVersion(ITextImage oldImage, ITextImage newImage)
            {
                var oldSnapshot = TryFindEditorSnapshot(oldImage);
                var newSnapshot = TryFindEditorSnapshot(newImage);

                return oldSnapshot != null && newSnapshot != null && oldSnapshot.Version.ReiteratedVersionNumber == newSnapshot.Version.ReiteratedVersionNumber;
            }

            private static readonly Func<ITextChange, TextChangeRange> s_forwardTextChangeRange = c => CreateTextChangeRange(c, forward: true);
            private static readonly Func<ITextChange, TextChangeRange> s_backwardTextChangeRange = c => CreateTextChangeRange(c, forward: false);

            private IReadOnlyList<TextChangeRange> GetChangeRanges(ITextImage snapshot1, ITextImage snapshot2, bool forward)
            {
                var oldSnapshot = forward ? snapshot1 : snapshot2;
                var newSnapshot = forward ? snapshot2 : snapshot1;

                INormalizedTextChangeCollection? changes = null;
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

            private TextChangeRange GetChangeRanges(ITextImageVersion oldVersion, ITextImageVersion newVersion, bool forward)
            {
                TextChangeRange? range = null;
                var iterator = GetMultipleVersionTextChanges(oldVersion, newVersion, forward);
                foreach (var changes in forward ? iterator : iterator.Reverse())
                {
                    range = range.Accumulate(changes);
                }

                RoslynDebug.Assert(range.HasValue);
                return range.Value;
            }

            private static IEnumerable<IEnumerable<TextChangeRange>> GetMultipleVersionTextChanges(
                ITextImageVersion oldVersion, ITextImageVersion newVersion, bool forward)
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
