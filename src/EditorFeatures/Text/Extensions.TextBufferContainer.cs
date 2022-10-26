// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    public static partial class Extensions
    {
        /// <summary>
        /// ITextBuffer implementation of SourceTextContainer
        /// </summary>
        internal class TextBufferContainer : SourceTextContainer
        {
            private readonly WeakReference<ITextBuffer> _weakEditorBuffer;
            private readonly object _gate = new object();
            private readonly ITextBufferCloneService? _textBufferCloneService;

            private event EventHandler<TextChangeEventArgs>? EtextChanged;
            private SourceText _currentText;

            private TextBufferContainer(ITextBuffer editorBuffer)
            {
                Contract.ThrowIfNull(editorBuffer);

                _weakEditorBuffer = new WeakReference<ITextBuffer>(editorBuffer);
                editorBuffer.Properties.TryGetProperty(typeof(ITextBufferCloneService), out _textBufferCloneService);
                _currentText = SnapshotSourceText.From(_textBufferCloneService, editorBuffer.CurrentSnapshot, this);
            }

            /// <summary>
            /// A weak map of all Editor ITextBuffers and their associated SourceTextContainer
            /// </summary>
            private static readonly ConditionalWeakTable<ITextBuffer, TextBufferContainer> s_textContainerMap = new();

            public static TextBufferContainer From(ITextBuffer buffer)
            {
                if (buffer == null)
                {
                    throw new ArgumentNullException(nameof(buffer));
                }

                return s_textContainerMap.GetValue(buffer, static buffer => new TextBufferContainer(buffer));
            }

            public ITextBuffer? TryFindEditorTextBuffer()
                => _weakEditorBuffer.GetTarget();

            public override SourceText CurrentText
            {
                get
                {
                    var editorBuffer = this.TryFindEditorTextBuffer();
                    return editorBuffer != null
                        ? editorBuffer.CurrentSnapshot.AsText()
                        : _currentText;
                }
            }

            public override event EventHandler<TextChangeEventArgs> TextChanged
            {
                add
                {
                    lock (_gate)
                    {
                        var textBuffer = this.TryFindEditorTextBuffer();
                        if (this.EtextChanged == null && textBuffer != null)
                        {
                            textBuffer.ChangedHighPriority += this.OnTextContentChanged;
                        }

                        this.EtextChanged += value;
                    }
                }

                remove
                {
                    lock (_gate)
                    {
                        this.EtextChanged -= value;

                        var textBuffer = this.TryFindEditorTextBuffer();
                        if (this.EtextChanged == null && textBuffer != null)
                        {
                            textBuffer.ChangedHighPriority -= this.OnTextContentChanged;
                        }
                    }
                }
            }

            private void OnTextContentChanged(object? sender, TextContentChangedEventArgs args)
            {
                var changed = this.EtextChanged;
                if (changed == null)
                {
                    return;
                }

                // we should process all changes even though there is no text changes
                // otherwise, Workspace.CurrentSolution won't move forward to latest ITextSnapshot

                // this should convert given editor snapshots to roslyn forked snapshots
                var oldText = (SnapshotSourceText)args.Before.AsText();
                var newText = SnapshotSourceText.From(_textBufferCloneService, args.After);
                _currentText = newText;

                var changes = ImmutableArray.CreateRange(args.Changes.Select(c => new TextChangeRange(new TextSpan(c.OldSpan.Start, c.OldSpan.Length), c.NewLength)));
                var eventArgs = new TextChangeEventArgs(oldText, newText, changes);

                this.LastEventArgs = eventArgs;
                changed(sender, eventArgs);
            }

            // These are the event args that were last sent from this text container when the text
            // content may have changed.
            public TextChangeEventArgs? LastEventArgs { get; private set; }
        }
    }
}
