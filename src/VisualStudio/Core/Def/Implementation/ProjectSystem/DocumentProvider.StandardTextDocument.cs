// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Undo;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class DocumentProvider
    {
        [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
        private class StandardTextDocument : ForegroundThreadAffinitizedObject, IVisualStudioHostDocument
        {
            /// <summary>
            /// The IDocumentProvider that created us.
            /// </summary>
            private readonly DocumentProvider _documentProvider;
            private readonly string _itemMoniker;
            private readonly ITextUndoHistoryRegistry _textUndoHistoryRegistry;
            private readonly FileChangeTracker _fileChangeTracker;
            private readonly ReiteratedVersionSnapshotTracker _snapshotTracker;
            private readonly TextLoader _doNotAccessDirectlyLoader;
            private readonly bool _isGenerated;

            /// <summary>
            /// The text buffer that is open in the editor. When the file is closed, this is null.
            /// </summary>
            private ITextBuffer _openTextBuffer;

            public DocumentId Id { get; }
            public IReadOnlyList<string> Folders { get; }
            public IVisualStudioHostProject Project { get; }
            public SourceCodeKind SourceCodeKind { get; }
            public DocumentKey Key { get; }

            public event EventHandler UpdatedOnDisk;
            public event EventHandler<bool> Opened;
            public event EventHandler<bool> Closing;

            public StandardTextDocument(
                DocumentProvider documentProvider,
                IVisualStudioHostProject project,
                DocumentKey documentKey,
                uint itemId,
                SourceCodeKind sourceCodeKind,
                ITextUndoHistoryRegistry textUndoHistoryRegistry,
                IVsFileChangeEx fileChangeService,
                ITextBuffer openTextBuffer,
                DocumentId id,
                bool isGenerated)
            {
                Contract.ThrowIfNull(documentProvider);

                this.Project = project;
                this.Id = id ?? DocumentId.CreateNewId(project.Id, documentKey.Moniker);
                this.Folders = project.GetFolderNames(itemId);

                _documentProvider = documentProvider;

                this.Key = documentKey;
                this.SourceCodeKind = sourceCodeKind;
                _itemMoniker = documentKey.Moniker;
                _textUndoHistoryRegistry = textUndoHistoryRegistry;
                _fileChangeTracker = new FileChangeTracker(fileChangeService, this.FilePath);
                _fileChangeTracker.UpdatedOnDisk += OnUpdatedOnDisk;

                _openTextBuffer = openTextBuffer;
                _snapshotTracker = new ReiteratedVersionSnapshotTracker(openTextBuffer);
                _isGenerated = isGenerated;

                // The project system does not tell us the CodePage specified in the proj file, so
                // we use null to auto-detect.
                _doNotAccessDirectlyLoader = new FileTextLoader(documentKey.Moniker, defaultEncoding: null);

                // If we aren't already open in the editor, then we should create a file change notification
                if (openTextBuffer == null)
                {
                    _fileChangeTracker.StartFileChangeListeningAsync();
                }
            }

            public bool IsOpen
            {
                get { return _openTextBuffer != null; }
            }

            public string FilePath
            {
                get { return Key.Moniker; }
            }

            public string Name
            {
                get
                {
                    try
                    {
                        return Path.GetFileName(this.FilePath);
                    }
                    catch (ArgumentException)
                    {
                        return this.FilePath;
                    }
                }
            }

            public TextLoader Loader
            {
                get
                {
                    _fileChangeTracker.EnsureSubscription();
                    return _doNotAccessDirectlyLoader;
                }
            }

            public DocumentInfo GetInitialState()
            {
                return DocumentInfo.Create(
                    id: this.Id,
                    name: this.Name,
                    folders: this.Folders,
                    sourceCodeKind: this.SourceCodeKind,
                    loader: this.Loader,
                    filePath: this.FilePath,
                    isGenerated: _isGenerated);
            }

            internal void ProcessOpen(ITextBuffer openedBuffer, bool isCurrentContext)
            {
                Debug.Assert(openedBuffer != null);

                _fileChangeTracker.StopFileChangeListening();
                _snapshotTracker.StartTracking(openedBuffer);

                _openTextBuffer = openedBuffer;
                Opened?.Invoke(this, isCurrentContext);
            }

            internal void ProcessClose(bool updateActiveContext)
            {
                // Todo: it might already be closed...
                // For now, continue asserting as it can be clicked through.
                Debug.Assert(_openTextBuffer != null);
                Closing?.Invoke(this, updateActiveContext);

                var buffer = _openTextBuffer;
                _openTextBuffer = null;

                _snapshotTracker.StopTracking(buffer);
                _fileChangeTracker.StartFileChangeListeningAsync();
            }

            public SourceTextContainer GetOpenTextContainer()
            {
                return _openTextBuffer.AsTextContainer();
            }

            public ITextBuffer GetOpenTextBuffer()
            {
                return _openTextBuffer;
            }

            private void OnUpdatedOnDisk(object sender, EventArgs e)
            {
                UpdatedOnDisk?.Invoke(this, EventArgs.Empty);
            }

            public void Dispose()
            {
                _fileChangeTracker.Dispose();
                _fileChangeTracker.UpdatedOnDisk -= OnUpdatedOnDisk;

                _documentProvider.StopTrackingDocument(this);
            }

            public void UpdateText(SourceText newText)
            {
                // Avoid opening the invisible editor if we already have a buffer.  It takes a relatively
                // expensive source control check if the file is already checked out.
                if (_openTextBuffer != null)
                {
                    UpdateText(newText, _openTextBuffer, EditOptions.DefaultMinimalChange);
                }
                else
                {
                    using (var invisibleEditor = ((VisualStudioWorkspaceImpl)this.Project.Workspace).OpenInvisibleEditor(this))
                    {
                        UpdateText(newText, invisibleEditor.TextBuffer, EditOptions.None);
                    }
                }
            }

            private static void UpdateText(SourceText newText, ITextBuffer buffer, EditOptions options)
            {
                using (var edit = buffer.CreateEdit(options, reiteratedVersionNumber: null, editTag: null))
                {
                    var oldSnapshot = buffer.CurrentSnapshot;
                    var oldText = oldSnapshot.AsText();
                    var changes = newText.GetTextChanges(oldText);

                    Workspace workspace = null;
                    if (Workspace.TryGetWorkspace(oldText.Container, out workspace))
                    {
                        var undoService = workspace.Services.GetService<ISourceTextUndoService>();
                        undoService.BeginUndoTransaction(oldSnapshot);
                    }

                    foreach (var change in changes)
                    {
                        edit.Replace(change.Span.Start, change.Span.Length, change.NewText);
                    }

                    edit.Apply();
                }
            }

            public ITextUndoHistory GetTextUndoHistory()
            {
                return _textUndoHistoryRegistry.GetHistory(GetOpenTextBuffer());
            }

            private string GetDebuggerDisplay()
            {
                return this.Name;
            }

            public uint GetItemId()
            {
                AssertIsForeground();

                if (_itemMoniker == null)
                {
                    return (uint)VSConstants.VSITEMID.Nil;
                }

                uint itemId;
                return Project.Hierarchy.ParseCanonicalName(_itemMoniker, out itemId) == VSConstants.S_OK
                    ? itemId
                    : (uint)VSConstants.VSITEMID.Nil;
            }
        }
    }
}
