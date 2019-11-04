// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionSize;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.Lists;
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

        internal VisualStudioWorkspace(HostServices hostServices)
            : base(hostServices, WorkspaceKind.Host)
        {
            // Compute the size of a solution in the background in vs workspaces.  This will ensure
            // that we create a persistence service in-proc for VS if the solution size warrants it.
            Options = Options.WithChangedOption(SolutionSizeOptions.ComputeSolutionSize, true);

            _backgroundCompiler = new BackgroundCompiler(this);

            var cacheService = Services.GetService<IWorkspaceCacheService>();
            if (cacheService != null)
            {
                cacheService.CacheFlushRequested += OnCacheFlushRequested;
            }

            _backgroundParser = new BackgroundParser(this);
            _backgroundParser.Start();
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

        /// <summary>
        /// Returns the hierarchy for a given project. 
        /// </summary>
        /// <param name="projectId">The <see cref="ProjectId"/> for the project.</param>
        /// <returns>The <see cref="IVsHierarchy"/>, or null if the project doesn't have one.</returns>
        public abstract IVsHierarchy GetHierarchy(ProjectId projectId);

        internal abstract Guid GetProjectGuid(ProjectId projectId);

        public virtual string GetFilePath(DocumentId documentId)
            => CurrentSolution.GetTextDocument(documentId)?.FilePath;

        /// <summary>
        /// Given a document id, opens an invisible editor for the document.
        /// </summary>
        /// <returns>A unique instance of IInvisibleEditor that must be disposed by the caller.</returns>
        internal abstract IInvisibleEditor OpenInvisibleEditor(DocumentId documentId);

        /// <summary>
        /// Returns the <see cref="EnvDTE.FileCodeModel"/> for a given document.
        /// </summary>
        public abstract EnvDTE.FileCodeModel GetFileCodeModel(DocumentId documentId);

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

        internal abstract string TryGetRuleSetPathForProject(ProjectId projectId);
    }
}
