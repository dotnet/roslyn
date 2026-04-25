// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.AspNetCore.Razor.Test.Common.Editor;

public class TestTextBuffer : ITextBuffer
{
    private readonly List<EventHandler<TextContentChangedEventArgs>> _attachedChangedEvents;

    public TestTextBuffer(ITextSnapshot initialSnapshot, IContentType? contentType = null)
    {
        ChangeContentType(contentType ?? TestInertContentType.Instance, editTag: null);

        CurrentSnapshot = initialSnapshot;
        if (CurrentSnapshot is StringTextSnapshot testSnapshot)
        {
            testSnapshot.TextBuffer = this;
        }

        _attachedChangedEvents = [];

        ReadOnlyRegionsChanged += (sender, args) => { };
        ChangedLowPriority += (sender, args) => { };
        ChangedHighPriority += (sender, args) => { };
        Changing += (sender, args) => { };
        PostChanged += (sender, args) => { };
        ContentTypeChanged += (sender, args) => { };
        Properties = new PropertyCollection();
    }

    public void ApplyEdit(TestEdit edit)
    {
        ApplyEdits(edit);
    }

    public void ApplyEdits(params TestEdit[] edits)
    {
        if (edits.Length == 0)
        {
            return;
        }

        var args = new TextContentChangedEventArgs(edits[0].OldSnapshot, edits[^1].NewSnapshot, new EditOptions(), null);
        foreach (var edit in edits)
        {
            args.Changes.Add(new TestTextChange(edit.Change));
        }

        CurrentSnapshot = edits[^1].NewSnapshot;
        if (CurrentSnapshot is StringTextSnapshot testSnapshot)
        {
            testSnapshot.TextBuffer = this;
        }

        foreach (var changedEvent in AttachedChangedEvents)
        {
            changedEvent.Invoke(this, args);
        }

        PostChanged?.Invoke(this, null!);

        ReadOnlyRegionsChanged?.Invoke(this, null!);
        ChangedLowPriority?.Invoke(this, null!);
        ChangedHighPriority?.Invoke(this, null!);
        Changing?.Invoke(this, null!);
    }

    public IReadOnlyList<EventHandler<TextContentChangedEventArgs>> AttachedChangedEvents => _attachedChangedEvents;

    public ITextSnapshot CurrentSnapshot { get; private set; }

    public PropertyCollection Properties { get; }

    public event EventHandler<SnapshotSpanEventArgs> ReadOnlyRegionsChanged;

    public event EventHandler<TextContentChangedEventArgs> Changed
    {
        add
        {
            _attachedChangedEvents.Add(value);
        }
        remove
        {
            _attachedChangedEvents.Remove(value);
        }
    }

    public event EventHandler<TextContentChangedEventArgs> ChangedLowPriority;
    public event EventHandler<TextContentChangedEventArgs> ChangedHighPriority;
    public event EventHandler<TextContentChangingEventArgs> Changing;
    public event EventHandler PostChanged;
    public event EventHandler<ContentTypeChangedEventArgs> ContentTypeChanged;

    public bool EditInProgress => throw new NotImplementedException();

    public IContentType? ContentType { get; private set; }

    public ITextEdit CreateEdit() => new BufferEdit(this);

    public void ChangeContentType(IContentType newContentType, object? editTag)
    {
        ContentType = newContentType;

        if (CurrentSnapshot is StringTextSnapshot oldStringTextSnapshot)
        {
            var newStringTextSnapshot = new StringTextSnapshot(oldStringTextSnapshot.Content, oldStringTextSnapshot.Version.VersionNumber + 1)
            {
                TextBuffer = this
            };
            CurrentSnapshot = newStringTextSnapshot;
        }

        ContentTypeChanged?.Invoke(this, null!);
    }

    public bool CheckEditAccess() => throw new NotImplementedException();

    public ITextEdit CreateEdit(EditOptions options, int? reiteratedVersionNumber, object editTag) => new BufferEdit(this);

    public IReadOnlyRegionEdit CreateReadOnlyRegionEdit() => throw new NotImplementedException();

    public ITextSnapshot Delete(Span deleteSpan) => throw new NotImplementedException();

    public NormalizedSpanCollection GetReadOnlyExtents(Span span) => throw new NotImplementedException();

    public ITextSnapshot Insert(int position, string text) => throw new NotImplementedException();

    public bool IsReadOnly(int position) => throw new NotImplementedException();

    public bool IsReadOnly(int position, bool isEdit) => throw new NotImplementedException();

    public bool IsReadOnly(Span span) => throw new NotImplementedException();

    public bool IsReadOnly(Span span, bool isEdit) => throw new NotImplementedException();

    public ITextSnapshot Replace(Span replaceSpan, string replaceWith) => throw new NotImplementedException();

    public void TakeThreadOwnership() => throw new NotImplementedException();

    private class BufferEdit : ITextEdit
    {
        private readonly TestTextBuffer _textBuffer;
        private readonly List<TestEdit> _edits;
        private StringTextSnapshot _editSnapshot;

        public BufferEdit(TestTextBuffer textBuffer)
        {
            _textBuffer = textBuffer;
            _editSnapshot = new StringTextSnapshot(_textBuffer.CurrentSnapshot.GetText(), _textBuffer.CurrentSnapshot.Version.VersionNumber);
            _edits = new List<TestEdit>();
        }

        public bool HasEffectiveChanges => throw new NotImplementedException();

        public bool HasFailedChanges => throw new NotImplementedException();

        public ITextSnapshot Snapshot => throw new NotImplementedException();

        public bool Canceled => throw new NotImplementedException();

        public ITextSnapshot Apply()
        {
            _textBuffer.ApplyEdits(_edits.ToArray());
            _edits.Clear();

            return _textBuffer.CurrentSnapshot;
        }

        public bool Insert(int position, string text)
        {
            var initialSnapshot = _editSnapshot;
            var newText = initialSnapshot.Content.Insert(position, text);
            var initialVersionNumber = initialSnapshot.Version.VersionNumber;
            var changedSnapshot = new StringTextSnapshot(newText, initialVersionNumber + 1);
            var edit = new TestEdit(position, 0, initialSnapshot, changedSnapshot, text);
            _edits.Add(edit);

            _editSnapshot = changedSnapshot;

            return true;
        }

        public void Cancel() => throw new NotImplementedException();

        public bool Delete(Span deleteSpan) => Delete(deleteSpan.Start, deleteSpan.Length);

        public bool Delete(int startPosition, int charsToDelete) => Replace(startPosition, charsToDelete, replaceWith: string.Empty);

        public void Dispose()
        {
        }

        public bool Insert(int position, char[] characterBuffer, int startIndex, int length) => throw new NotImplementedException();

        public bool Replace(Span replaceSpan, string replaceWith) => Replace(replaceSpan.Start, replaceSpan.Length, replaceWith);

        public bool Replace(int startPosition, int charsToReplace, string replaceWith)
        {
            var initialSnapshot = _editSnapshot;
            var preText = initialSnapshot.Content[..startPosition];
            var postText = initialSnapshot.Content[(startPosition + charsToReplace)..];
            var newText = preText + replaceWith + postText;
            var initialVersionNumber = initialSnapshot.Version.VersionNumber;
            var changedSnapshot = new StringTextSnapshot(newText, initialVersionNumber + 1);
            var edit = new TestEdit(startPosition, charsToReplace, initialSnapshot, changedSnapshot, replaceWith);
            _edits.Add(edit);

            _editSnapshot = changedSnapshot;

            return true;
        }
    }
}
