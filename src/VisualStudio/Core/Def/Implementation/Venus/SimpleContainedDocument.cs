// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Experiment;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Venus
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    /// <summary>
    /// An IVisualStudioDocument which represents the secondary buffer to the workspace API.
    /// </summary>
    internal sealed class SimpleContainedDocument : ForegroundThreadAffinitizedObject, IVisualStudioHostDocument
    {
        /// <summary>
        /// The IDocumentProvider that created us.
        /// </summary>
        private readonly DocumentProvider _documentProvider;
        private readonly FileChangeTracker _fileChangeTracker;
        private readonly TextLoader _doNotAccessDirectlyLoader;

        public DocumentId Id { get; }
        public IReadOnlyList<string> Folders { get; }
        public AbstractProject Project { get; }
        public SourceCodeKind SourceCodeKind { get; }
        public DocumentKey Key { get; }
        public IDocumentServiceFactory DocumentServiceFactory { get; }

        public SimpleContainedDocument(
            DocumentProvider documentProvider,
            AbstractProject project,
            DocumentKey documentKey,
            Func<uint, IReadOnlyList<string>> getFolderNames,
            SourceCodeKind sourceCodeKind,
            IVsFileChangeEx fileChangeService,
            DocumentId id,
            EventHandler updatedOnDiskHandler,
            IDocumentServiceFactory documentServiceFactory)
        {
            Contract.ThrowIfNull(documentProvider);

            this.Project = project;
            this.Id = id ?? DocumentId.CreateNewId(project.Id, documentKey.Moniker);

            var itemid = this.GetItemId();
            this.Folders = itemid == (uint)VSConstants.VSITEMID.Nil
                ? SpecializedCollections.EmptyReadOnlyList<string>()
                : getFolderNames(itemid);

            _documentProvider = documentProvider;

            this.Key = documentKey;
            this.SourceCodeKind = sourceCodeKind;

            _fileChangeTracker = new FileChangeTracker(fileChangeService, this.FilePath);
            _fileChangeTracker.UpdatedOnDisk += OnUpdatedOnDisk;

            // The project system does not tell us the CodePage specified in the proj file, so
            // we use null to auto-detect.
            _doNotAccessDirectlyLoader = new FileTextLoader(documentKey.Moniker, defaultEncoding: null);

            _fileChangeTracker.StartFileChangeListeningAsync();

            if (updatedOnDiskHandler != null)
            {
                UpdatedOnDisk += updatedOnDiskHandler;
            }

            this.DocumentServiceFactory = documentServiceFactory;
        }

        public TextLoader Loader
        {
            get
            {
                _fileChangeTracker.EnsureSubscription();
                return _doNotAccessDirectlyLoader;
            }
        }

        public uint GetItemId()
        {
            AssertIsForeground();

            if (this.Key.Moniker == null || Project.Hierarchy == null)
            {
                return (uint)VSConstants.VSITEMID.Nil;
            }

            return Project.Hierarchy.ParseCanonicalName(this.Key.Moniker, out var itemId) == VSConstants.S_OK
                ? itemId
                : (uint)VSConstants.VSITEMID.Nil;
        }

        public DocumentInfo GetInitialState()
        {
            return DocumentInfo.Create(
                this.Id,
                this.Name,
                folders: this.Folders,
                sourceCodeKind: SourceCodeKind,
                loader: this.Loader,
                filePath: this.Key.Moniker,
                isGenerated: true,
                documentServiceFactory: DocumentServiceFactory);
        }

        public string FilePath => Key.Moniker;
        public bool IsOpen => false;

#pragma warning disable 67

        public event EventHandler UpdatedOnDisk;
        public event EventHandler<bool> Opened;
        public event EventHandler<bool> Closing;

#pragma warning restore 67

        public ITextBuffer GetOpenTextBuffer() => null;
        public SourceTextContainer GetOpenTextContainer() => null;

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

        public void Dispose()
        {
            _fileChangeTracker.Dispose();
            _fileChangeTracker.UpdatedOnDisk -= OnUpdatedOnDisk;

            _documentProvider.StopTrackingDocument(this);
        }
        public void UpdateText(SourceText newText) => throw new NotSupportedException();
        public ITextBuffer GetTextUndoHistoryBuffer() => null;

        private void OnUpdatedOnDisk(object sender, EventArgs e)
        {
            UpdatedOnDisk?.Invoke(this, EventArgs.Empty);
        }
    }
}
