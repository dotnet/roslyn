// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Undo;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.GoToDefinition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.Lists;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Roslyn.Utilities;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;

namespace Microsoft.VisualStudio.LanguageServices
{
    [Export(typeof(VisualStudioWorkspace))]
    [Export(typeof(VisualStudioWorkspaceImpl))]
    internal class RoslynVisualStudioWorkspace : VisualStudioWorkspaceImpl
    {
        private readonly IThreadingContext _threadingContext;

        /// <remarks>
        /// Must be lazily constructed since the <see cref="IStreamingFindUsagesPresenter"/> implementation imports a
        /// backreference to <see cref="VisualStudioWorkspace"/>.
        /// </remarks>
        private readonly Lazy<IStreamingFindUsagesPresenter> _streamingPresenter;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RoslynVisualStudioWorkspace(
            ExportProvider exportProvider,
            IThreadingContext threadingContext,
            Lazy<IStreamingFindUsagesPresenter> streamingPresenter,
            [Import(typeof(SVsServiceProvider))] IAsyncServiceProvider asyncServiceProvider)
            : base(exportProvider, asyncServiceProvider)
        {
            _threadingContext = threadingContext;
            _streamingPresenter = streamingPresenter;
        }

        internal override IInvisibleEditor OpenInvisibleEditor(DocumentId documentId)
        {
            var globalUndoService = this.Services.GetRequiredService<IGlobalUndoService>();
            var needsUndoDisabled = false;

            var textDocument = this.CurrentSolution.GetTextDocument(documentId);

            if (textDocument == null)
            {
                throw new InvalidOperationException(string.Format(WorkspacesResources._0_is_not_part_of_the_workspace, documentId));
            }

            // Do not save the file if is open and there is not a global undo transaction.
            var needsSave = globalUndoService.IsGlobalTransactionOpen(this) || !this.IsDocumentOpen(documentId);
            if (needsSave)
            {
                if (textDocument is Document document)
                {
                    // Disable undo on generated documents
                    needsUndoDisabled = document.IsGeneratedCode(CancellationToken.None);
                }
                else
                {
                    // Enable undo on "additional documents" or if no document can be found.
                    needsUndoDisabled = false;
                }
            }

            // Documents in the VisualStudioWorkspace always have file paths since that's how we get files given
            // to us from the project system.
            Contract.ThrowIfNull(textDocument.FilePath);

            return new InvisibleEditor(ServiceProvider.GlobalProvider, textDocument.FilePath, GetHierarchy(documentId.ProjectId), needsSave, needsUndoDisabled);
        }

        [Obsolete("Use TryGoToDefinitionAsync instead", error: true)]
        public override bool TryGoToDefinition(ISymbol symbol, Project project, CancellationToken cancellationToken)
            => _threadingContext.JoinableTaskFactory.Run(() => TryGoToDefinitionAsync(symbol, project, cancellationToken));

        public override async Task<bool> TryGoToDefinitionAsync(
            ISymbol symbol, Project project, CancellationToken cancellationToken)
        {
            var currentProject = project.Solution.Workspace.CurrentSolution.GetProject(project.Id);
            if (currentProject == null)
                return false;

            var symbolId = SymbolKey.Create(symbol, cancellationToken);
            var currentCompilation = await currentProject.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var symbolInfo = symbolId.Resolve(currentCompilation, cancellationToken: cancellationToken);
            if (symbolInfo.Symbol == null)
                return false;

            return await GoToDefinitionHelpers.TryNavigateToLocationAsync(
                symbolInfo.Symbol, currentProject.Solution,
                _threadingContext, _streamingPresenter.Value, cancellationToken).ConfigureAwait(false);
        }

        public override bool TryFindAllReferences(ISymbol symbol, Project project, CancellationToken cancellationToken)
        {
            // Legacy API.  Previously used by ObjectBrowser to support 'FindRefs' off of an
            // object browser item.  Now ObjectBrowser goes through the streaming-FindRefs system.
            return false;
        }

        public override void DisplayReferencedSymbols(Solution solution, IEnumerable<ReferencedSymbol> referencedSymbols)
        {
            // Legacy API.  Previously used by ObjectBrowser to support 'FindRefs' off of an
            // object browser item.  Now ObjectBrowser goes through the streaming-FindRefs system.
        }

        internal override object? GetBrowseObject(SymbolListItem symbolListItem)
        {
            var compilation = symbolListItem.GetCompilation(this);
            if (compilation == null)
            {
                return null;
            }

            var symbol = symbolListItem.ResolveSymbol(compilation);
            var sourceLocation = symbol.Locations.Where(l => l.IsInSource).FirstOrDefault();

            if (sourceLocation == null)
            {
                return null;
            }

            var projectId = symbolListItem.ProjectId;
            if (projectId == null)
            {
                return null;
            }

            var project = this.CurrentSolution.GetProject(projectId);
            if (project == null)
            {
                return null;
            }

            var codeModelService = project.Services.GetService<ICodeModelService>();
            if (codeModelService == null)
            {
                return null;
            }

            var tree = sourceLocation.SourceTree;
            Contract.ThrowIfNull(tree, "We have a location that was in source, but doesn't have a SourceTree.");

            var document = project.GetDocument(tree);
            Contract.ThrowIfNull(document, "We have a symbol coming from a tree, and that tree isn't in the Project it supposedly came from.");

            var vsFileCodeModel = this.GetFileCodeModel(document.Id);

            var fileCodeModel = ComAggregate.GetManagedObject<FileCodeModel>(vsFileCodeModel);
            if (fileCodeModel != null)
            {
                var syntaxNode = tree.GetRoot().FindNode(sourceLocation.SourceSpan);
                while (syntaxNode != null)
                {
                    if (!codeModelService.TryGetNodeKey(syntaxNode).IsEmpty)
                    {
                        break;
                    }

                    syntaxNode = syntaxNode.Parent;
                }

                if (syntaxNode != null)
                {
                    var codeElement = fileCodeModel.GetOrCreateCodeElement<EnvDTE.CodeElement>(syntaxNode);
                    if (codeElement != null)
                    {
                        return codeElement;
                    }
                }
            }

            return null;
        }
    }
}
