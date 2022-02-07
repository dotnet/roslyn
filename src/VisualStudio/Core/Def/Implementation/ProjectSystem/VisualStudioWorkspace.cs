// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.Lists;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices
{
    /// <summary>
    /// A Workspace specific to Visual Studio.
    /// </summary>
    public abstract class VisualStudioWorkspace : Workspace
    {
        private BackgroundCompiler? _backgroundCompiler;
        private readonly BackgroundParser _backgroundParser;

        static VisualStudioWorkspace()
        {
            FaultReporter.InitializeFatalErrorHandlers();
        }

        internal VisualStudioWorkspace(HostServices hostServices)
            : base(hostServices, WorkspaceKind.Host)
        {
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
            => _backgroundParser.Parse(document);

        protected override void OnDocumentClosing(DocumentId documentId)
            => _backgroundParser.CancelParse(documentId);

        internal override bool IgnoreUnchangeableDocumentsWhenApplyingChanges => true;

        /// <summary>
        /// Returns the hierarchy for a given project. 
        /// </summary>
        /// <param name="projectId">The <see cref="ProjectId"/> for the project.</param>
        /// <returns>The <see cref="IVsHierarchy"/>, or null if the project doesn't have one.</returns>
        public abstract IVsHierarchy? GetHierarchy(ProjectId projectId);

        internal abstract Guid GetProjectGuid(ProjectId projectId);

        public virtual string? GetFilePath(DocumentId documentId)
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

        internal abstract object? GetBrowseObject(SymbolListItem symbolListItem);

        [Obsolete("Use TryGoToDefinitionAsync instead", error: false)]
        public abstract bool TryGoToDefinition(ISymbol symbol, Project project, CancellationToken cancellationToken);
        public abstract Task<bool> TryGoToDefinitionAsync(ISymbol symbol, Project project, CancellationToken cancellationToken);

        public abstract bool TryFindAllReferences(ISymbol symbol, Project project, CancellationToken cancellationToken);

        public abstract void DisplayReferencedSymbols(Solution solution, IEnumerable<ReferencedSymbol> referencedSymbols);

        /// <summary>
        /// Creates a <see cref="PortableExecutableReference" /> that correctly retrieves the Visual Studio context,
        /// such as documentation comments in the correct language.
        /// </summary>
        /// <param name="filePath">The file path of the assembly or module.</param>
        /// <param name="properties">The properties for the reference.</param>
        public PortableExecutableReference CreatePortableExecutableReference(string filePath, MetadataReferenceProperties properties)
            => this.Services.GetRequiredService<IMetadataService>().GetReference(filePath, properties);

        internal abstract string? TryGetRuleSetPathForProject(ProjectId projectId);
    }
}
