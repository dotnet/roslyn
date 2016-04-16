// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Implementation.GoToDefinition;
using Microsoft.CodeAnalysis.Editor.Undo;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.GeneratedCodeRecognition;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.Lists;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices
{
    [Export(typeof(VisualStudioWorkspace))]
    [Export(typeof(VisualStudioWorkspaceImpl))]
    internal class RoslynVisualStudioWorkspace : VisualStudioWorkspaceImpl
    {
        private readonly IEnumerable<Lazy<INavigableItemsPresenter>> _navigableItemsPresenters;
        private readonly IEnumerable<Lazy<IReferencedSymbolsPresenter>> _referencedSymbolsPresenters;

        [ImportingConstructor]
        private RoslynVisualStudioWorkspace(
            SVsServiceProvider serviceProvider,
            SaveEventsService saveEventsService,
            [ImportMany] IEnumerable<Lazy<INavigableItemsPresenter>> navigableItemsPresenters,
            [ImportMany] IEnumerable<Lazy<IReferencedSymbolsPresenter>> referencedSymbolsPresenters)
            : base(
                serviceProvider,
                backgroundWork: WorkspaceBackgroundWork.ParseAndCompile)
        {
            PrimaryWorkspace.Register(this);

            InitializeStandardVisualStudioWorkspace(serviceProvider, saveEventsService);

            _navigableItemsPresenters = navigableItemsPresenters;
            _referencedSymbolsPresenters = referencedSymbolsPresenters;
        }

        public override EnvDTE.FileCodeModel GetFileCodeModel(DocumentId documentId)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            var project = ProjectTracker.GetProject(documentId.ProjectId);
            if (project == null)
            {
                throw new ArgumentException(ServicesVSResources.DocumentIdNotFromWorkspace, nameof(documentId));
            }

            var document = project.GetDocumentOrAdditionalDocument(documentId);
            if (document == null)
            {
                throw new ArgumentException(ServicesVSResources.DocumentIdNotFromWorkspace, nameof(documentId));
            }

            var provider = project as IProjectCodeModelProvider;
            if (provider != null)
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

            var project = ProjectTracker.GetProject(documentId.ProjectId);
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
            // We need to ensure the file is saved, only if a global undo transaction is open
            var globalUndoService = this.Services.GetService<IGlobalUndoService>();
            var needsSave = globalUndoService.IsGlobalTransactionOpen(this);

            var needsUndoDisabled = false;
            if (needsSave)
            {
                if (this.CurrentSolution.ContainsDocument(hostDocument.Id))
                {
                    // Disable undo on generated documents
                    needsUndoDisabled = this.Services.GetService<IGeneratedCodeRecognitionService>().IsGeneratedCode(this.CurrentSolution.GetDocument(hostDocument.Id));
                }
                else
                {
                    // Enable undo on "additional documents" or if no document can be found.
                    needsUndoDisabled = false;
                }
            }

            return new InvisibleEditor(ServiceProvider, hostDocument.FilePath, needsSave, needsUndoDisabled);
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
            var symbolId = SymbolKey.Create(symbol, originalCompilation, cancellationToken);
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

        public override bool TryGoToDefinition(ISymbol symbol, Project project, CancellationToken cancellationToken)
        {
            if (!_navigableItemsPresenters.Any())
            {
                return false;
            }

            ISymbol searchSymbol;
            Project searchProject;
            if (!TryResolveSymbol(symbol, project, cancellationToken, out searchSymbol, out searchProject))
            {
                return false;
            }

            return GoToDefinitionHelpers.TryGoToDefinition(
                searchSymbol, searchProject, _navigableItemsPresenters, cancellationToken: cancellationToken);
        }

        public override bool TryFindAllReferences(ISymbol symbol, Project project, CancellationToken cancellationToken)
        {
            if (!_referencedSymbolsPresenters.Any())
            {
                return false;
            }

            ISymbol searchSymbol;
            Project searchProject;
            if (!TryResolveSymbol(symbol, project, cancellationToken, out searchSymbol, out searchProject))
            {
                return false;
            }

            var searchSolution = searchProject.Solution;

            var result = SymbolFinder
                .FindReferencesAsync(searchSymbol, searchSolution, cancellationToken)
                .WaitAndGetResult(cancellationToken).ToList();

            if (result != null)
            {
                DisplayReferencedSymbols(searchSolution, result);
                return true;
            }

            return false;
        }

        public override void DisplayReferencedSymbols(Solution solution, IEnumerable<ReferencedSymbol> referencedSymbols)
        {
            foreach (var presenter in _referencedSymbolsPresenters)
            {
                presenter.Value.DisplayResult(solution, referencedSymbols);
            }
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
