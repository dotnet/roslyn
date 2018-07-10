// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Experiment;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;
using Workspace = Microsoft.CodeAnalysis.Workspace;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Venus
{
    /// <summary>
    /// An IVisualStudioDocument which represents the secondary buffer to the workspace API.
    /// </summary>
    internal sealed class SimpleContainedDocument : ForegroundThreadAffinitizedObject, IVisualStudioHostDocument
    {
        /// <summary>
        /// The IDocumentProvider that created us.
        /// </summary>
        private readonly DocumentProvider _documentProvider;
        private readonly SourceTextContainer _sourceTextContainer;
        private readonly EventHandler _updatedHandler;

        public DocumentId Id { get; }
        public IReadOnlyList<string> Folders { get; }
        public AbstractProject Project { get; }
        public SourceCodeKind SourceCodeKind { get; }
        public DocumentKey Key { get; }
        public TextLoader Loader { get; }
        public IDocumentServiceFactory DocumentServiceFactory { get; }

        public SimpleContainedDocument(
            DocumentProvider documentProvider,
            AbstractProject project,
            DocumentKey documentKey,
            ImmutableArray<string> folderNames,
            SourceTextContainer sourceTextContainer,
            SourceCodeKind sourceCodeKind,
            DocumentId id,
            EventHandler updatedHandler,
            IDocumentServiceFactory documentServiceFactory)
        {
            Contract.ThrowIfNull(documentProvider);

            this.Project = project;

            this.Key = documentKey;
            this.SourceCodeKind = sourceCodeKind;

            this.Id = id ?? DocumentId.CreateNewId(project.Id, documentKey.Moniker);

            var itemid = this.GetItemId();
            this.Folders = itemid == (uint)VSConstants.VSITEMID.Nil
                ? SpecializedCollections.EmptyReadOnlyList<string>()
                : folderNames;

            _documentProvider = documentProvider;

            this.Loader = new SourceTextContainerTextLoader(sourceTextContainer, this.FilePath);

            _updatedHandler = updatedHandler;
            _sourceTextContainer = sourceTextContainer;
            _sourceTextContainer.TextChanged += OnTextChanged;

            this.DocumentServiceFactory = documentServiceFactory;
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
            _sourceTextContainer.TextChanged -= OnTextChanged;

            _documentProvider.StopTrackingDocument(this);
        }

        public void UpdateText(SourceText newText) => throw new NotSupportedException();
        public ITextBuffer GetTextUndoHistoryBuffer() => null;

        private void OnTextChanged(object sender, TextChangeEventArgs e)
        {
            _updatedHandler(this, e);
        }

        private class SourceTextContainerTextLoader : TextLoader
        {
            private readonly SourceTextContainer _sourceTextContainer;
            private readonly string _filePath;

            public SourceTextContainerTextLoader(SourceTextContainer sourceTextContainer, string filePath)
            {
                _sourceTextContainer = sourceTextContainer;
                _filePath = filePath;
            }

            public override Task<TextAndVersion> LoadTextAndVersionAsync(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
            {
                return Task.FromResult(LoadTextAndVersionSynchronously(workspace, documentId, cancellationToken));
            }

            internal override TextAndVersion LoadTextAndVersionSynchronously(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
            {
                return TextAndVersion.Create(_sourceTextContainer.CurrentText, VersionStamp.Create(), _filePath);
            }
        }
    }
}
