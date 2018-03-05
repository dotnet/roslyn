// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.GoToDefinition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Undo;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.Lists;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices
{
    [Export(typeof(VisualStudioWorkspace))]
    [Export(typeof(VisualStudioWorkspaceImpl))]
    internal class RoslynVisualStudioWorkspace : VisualStudioWorkspaceImpl
    {
        private readonly IEnumerable<Lazy<IStreamingFindUsagesPresenter>> _streamingPresenters;

        [ImportingConstructor]
        private RoslynVisualStudioWorkspace(
            ExportProvider exportProvider,
            [ImportMany] IEnumerable<Lazy<IStreamingFindUsagesPresenter>> streamingPresenters,
            [ImportMany] IEnumerable<IDocumentOptionsProviderFactory> documentOptionsProviderFactories)
            : base(exportProvider.AsExportProvider())
        {
            _streamingPresenters = streamingPresenters;

            foreach (var providerFactory in documentOptionsProviderFactories)
            {
                Services.GetRequiredService<IOptionService>().RegisterDocumentOptionsProvider(providerFactory.Create(this));
            }
        }

        public override EnvDTE.FileCodeModel GetFileCodeModel(DocumentId documentId)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            if (DeferredState == null)
            {
                // We haven't gotten any projects added yet, so we don't know where this came from
                throw new ArgumentException(ServicesVSResources.The_given_DocumentId_did_not_come_from_the_Visual_Studio_workspace, nameof(documentId));
            }

            var project = DeferredState.ProjectTracker.GetProject(documentId.ProjectId);
            if (project == null)
            {
                throw new ArgumentException(ServicesVSResources.The_given_DocumentId_did_not_come_from_the_Visual_Studio_workspace, nameof(documentId));
            }

            var document = project.GetDocumentOrAdditionalDocument(documentId);
            if (document == null)
            {
                throw new ArgumentException(ServicesVSResources.The_given_DocumentId_did_not_come_from_the_Visual_Studio_workspace, nameof(documentId));
            }

            if (project is IProjectCodeModelProvider provider)
            {
                var projectCodeModel = provider.ProjectCodeModel;
                if (projectCodeModel.CanCreateFileCodeModelThroughProject(document.FilePath))
                {
                    return (EnvDTE.FileCodeModel)projectCodeModel.CreateFileCodeModelThroughProject(document.FilePath);
                }
            }

            return null;
        }

        internal override bool RenameFileCodeModelInstance(DocumentId documentId, string newFilePath)
        {
            if (documentId == null)
            {
                return false;
            }

            var project = DeferredState.ProjectTracker.GetProject(documentId.ProjectId);
            if (project == null)
            {
                return false;
            }

            var document = project.GetDocumentOrAdditionalDocument(documentId);
            if (document == null)
            {
                return false;
            }

            var codeModelProvider = project as IProjectCodeModelProvider;
            if (codeModelProvider == null)
            {
                return false;
            }

            var codeModelCache = codeModelProvider.ProjectCodeModel.GetCodeModelCache();
            if (codeModelCache == null)
            {
                return false;
            }

            codeModelCache.OnSourceFileRenaming(document.FilePath, newFilePath);

            return true;
        }

        internal override IInvisibleEditor OpenInvisibleEditor(DocumentId documentId)
        {
            var hostDocument = GetHostDocument(documentId);
            return OpenInvisibleEditor(hostDocument);
        }

        internal override IInvisibleEditor OpenInvisibleEditor(IVisualStudioHostDocument hostDocument)
        {
            var globalUndoService = this.Services.GetService<IGlobalUndoService>();
            var needsUndoDisabled = false;

            // Do not save the file if is open and there is not a global undo transaction.
            var needsSave = globalUndoService.IsGlobalTransactionOpen(this) || !hostDocument.IsOpen;
            if (needsSave)
            {
                if (this.CurrentSolution.ContainsDocument(hostDocument.Id))
                {
                    // Disable undo on generated documents
                    needsUndoDisabled = this.CurrentSolution.GetDocument(hostDocument.Id).IsGeneratedCode(CancellationToken.None);
                }
                else
                {
                    // Enable undo on "additional documents" or if no document can be found.
                    needsUndoDisabled = false;
                }
            }

            return new InvisibleEditor(DeferredState.ServiceProvider, hostDocument.FilePath, hostDocument.Project, needsSave, needsUndoDisabled);
        }

        private static bool TryResolveSymbol(ISymbol symbol, Project project, CancellationToken cancellationToken, out ISymbol resolvedSymbol, out Project resolvedProject)
        {
            resolvedSymbol = null;
            resolvedProject = null;

            var currentProject = project.Solution.Workspace.CurrentSolution.GetProject(project.Id);
            if (currentProject == null)
            {
                return false;
            }

            var originalCompilation = project.GetCompilationAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var symbolId = SymbolKey.Create(symbol, cancellationToken);
            var currentCompilation = currentProject.GetCompilationAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var symbolInfo = symbolId.Resolve(currentCompilation, cancellationToken: cancellationToken);

            if (symbolInfo.Symbol == null)
            {
                return false;
            }

            resolvedSymbol = symbolInfo.Symbol;
            resolvedProject = currentProject;

            return true;
        }

        public override bool TryGoToDefinition(
            ISymbol symbol, Project project, CancellationToken cancellationToken)
        {
            if (!_streamingPresenters.Any())
            {
                return false;
            }

            if (!TryResolveSymbol(symbol, project, cancellationToken, 
                    out var searchSymbol, out var searchProject))
            {
                return false;
            }

            return GoToDefinitionHelpers.TryGoToDefinition(
                searchSymbol, searchProject, 
                _streamingPresenters, cancellationToken);
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

        internal override object GetBrowseObject(SymbolListItem symbolListItem)
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

            var codeModelService = project.LanguageServices.GetService<ICodeModelService>();
            if (codeModelService == null)
            {
                return null;
            }

            var tree = sourceLocation.SourceTree;
            var document = project.GetDocument(tree);

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
