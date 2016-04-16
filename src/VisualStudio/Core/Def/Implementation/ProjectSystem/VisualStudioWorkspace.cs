// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.Lists;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices
{
    /// <summary>
    /// A Workspace specific to Visual Studio.
    /// </summary>
    public abstract class VisualStudioWorkspace : Workspace
    {
        private BackgroundCompiler _backgroundCompiler;
        private readonly BackgroundParser _backgroundParser;

        internal VisualStudioWorkspace(HostServices hostServices, WorkspaceBackgroundWork backgroundWork)
            : base(hostServices, WorkspaceKind.Host)
        {
            if ((backgroundWork & WorkspaceBackgroundWork.Compile) != 0)
            {
                _backgroundCompiler = new BackgroundCompiler(this);

                var cacheService = Services.GetService<IWorkspaceCacheService>();
                if (cacheService != null)
                {
                    cacheService.CacheFlushRequested += OnCacheFlushRequested;
                }
            }

            if ((backgroundWork & WorkspaceBackgroundWork.Parse) != 0)
            {
                _backgroundParser = new BackgroundParser(this);
                _backgroundParser.Start();
            }
        }

        private void OnCacheFlushRequested(object sender, EventArgs e)
        {
            if (_backgroundCompiler != null)
            {
                _backgroundCompiler.Dispose();
                _backgroundCompiler = null; // PartialSemanticsEnabled will now return false
            }

            // No longer need cache notifications
            var cacheService = Services.GetService<IWorkspaceCacheService>();
            if (cacheService != null)
            {
                cacheService.CacheFlushRequested -= OnCacheFlushRequested;
            }
        }

        protected internal override bool PartialSemanticsEnabled
        {
            get { return _backgroundCompiler != null; }
        }

        protected override void OnDocumentTextChanged(Document document)
        {
            if (_backgroundParser != null)
            {
                _backgroundParser.Parse(document);
            }
        }

        protected override void OnDocumentClosing(DocumentId documentId)
        {
            if (_backgroundParser != null)
            {
                _backgroundParser.CancelParse(documentId);
            }
        }

        public abstract IVsHierarchy GetHierarchy(ProjectId projectId);
        public abstract string GetFilePath(DocumentId documentId);

        /// <summary>
        /// Given a document id, opens an invisible editor for the document.
        /// </summary>
        /// <returns>A unique instance of IInvisibleEditor that must be disposed by the caller.</returns>
        internal abstract IInvisibleEditor OpenInvisibleEditor(DocumentId documentId);
        internal abstract IInvisibleEditor OpenInvisibleEditor(IVisualStudioHostDocument document);

        /// <summary>
        /// Returns the <see cref="EnvDTE.FileCodeModel"/> for a given document.
        /// </summary>
        public abstract EnvDTE.FileCodeModel GetFileCodeModel(DocumentId documentId);

        internal abstract bool RenameFileCodeModelInstance(DocumentId documentId, string newFilePath);

        internal abstract object GetBrowseObject(SymbolListItem symbolListItem);

        public abstract bool TryGoToDefinition(ISymbol symbol, Project project, CancellationToken cancellationToken);
        public abstract bool TryFindAllReferences(ISymbol symbol, Project project, CancellationToken cancellationToken);

        public abstract void DisplayReferencedSymbols(Solution solution, IEnumerable<ReferencedSymbol> referencedSymbols);

        /// <summary>
        /// Creates a <see cref="PortableExecutableReference" /> that correctly retrieves the Visual Studio context,
        /// such as documentation comments in the correct language.
        /// </summary>
        /// <param name="filePath">The file path of the assembly or module.</param>
        /// <param name="properties">The properties for the reference.</param>
        public PortableExecutableReference CreatePortableExecutableReference(string filePath, MetadataReferenceProperties properties)
        {
            return this.Services.GetService<IMetadataService>().GetReference(filePath, properties);
        }
    }
}
