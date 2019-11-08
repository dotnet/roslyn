// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editing
{
    /// <summary>
    /// An editor for making changes to symbol source declarations.
    /// </summary>
    public sealed class SymbolEditor
    {
        private readonly Solution _originalSolution;
        private Solution _currentSolution;

        private SymbolEditor(Solution solution)
        {
            _originalSolution = solution;
            _currentSolution = solution;
        }

        /// <summary>
        /// Creates a new <see cref="SymbolEditor"/> instance.
        /// </summary>
        public static SymbolEditor Create(Solution solution)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            return new SymbolEditor(solution);
        }

        /// <summary>
        /// Creates a new <see cref="SymbolEditor"/> instance.
        /// </summary>
        public static SymbolEditor Create(Document document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return new SymbolEditor(document.Project.Solution);
        }

        /// <summary>
        /// The original solution.
        /// </summary>
        public Solution OriginalSolution => _originalSolution;

        /// <summary>
        /// The solution with the edits applied.
        /// </summary>
        public Solution ChangedSolution => _currentSolution;

        /// <summary>
        /// The documents changed since the <see cref="SymbolEditor"/> was constructed.
        /// </summary>
        public IEnumerable<Document> GetChangedDocuments()
        {
            var solutionChanges = _currentSolution.GetChanges(_originalSolution);

            foreach (var projectChanges in solutionChanges.GetProjectChanges())
            {
                foreach (var id in projectChanges.GetAddedDocuments())
                {
                    yield return _currentSolution.GetDocument(id);
                }

                foreach (var id in projectChanges.GetChangedDocuments())
                {
                    yield return _currentSolution.GetDocument(id);
                }
            }

            foreach (var project in solutionChanges.GetAddedProjects())
            {
                foreach (var id in project.DocumentIds)
                {
                    yield return project.GetDocument(id);
                }
            }
        }

        /// <summary>
        /// Gets the current symbol for a source symbol.
        /// </summary>
        public async Task<ISymbol> GetCurrentSymbolAsync(ISymbol symbol, CancellationToken cancellationToken = default)
        {
            var symbolId = DocumentationCommentId.CreateDeclarationId(symbol);

            // check to see if symbol is from current solution
            var project = _currentSolution.GetProject(symbol.ContainingAssembly, cancellationToken);
            if (project != null)
            {
                return await GetSymbolAsync(_currentSolution, project.Id, symbolId, cancellationToken).ConfigureAwait(false);
            }

            // check to see if it is from original solution
            project = _originalSolution.GetProject(symbol.ContainingAssembly, cancellationToken);
            if (project != null)
            {
                return await GetSymbolAsync(_currentSolution, project.Id, symbolId, cancellationToken).ConfigureAwait(false);
            }

            // try to find symbol from any project (from current solution) with matching assembly name
            foreach (var projectId in this.GetProjectsForAssembly(symbol.ContainingAssembly))
            {
                var currentSymbol = await GetSymbolAsync(_currentSolution, projectId, symbolId, cancellationToken).ConfigureAwait(false);
                if (currentSymbol != null)
                {
                    return currentSymbol;
                }
            }

            return null;
        }

        private ImmutableDictionary<string, ImmutableArray<ProjectId>> _assemblyNameToProjectIdMap;

        private ImmutableArray<ProjectId> GetProjectsForAssembly(IAssemblySymbol assembly)
        {
            if (_assemblyNameToProjectIdMap == null)
            {
                _assemblyNameToProjectIdMap = _originalSolution.Projects
                    .ToLookup(p => p.AssemblyName, p => p.Id)
                    .ToImmutableDictionary(g => g.Key, g => ImmutableArray.CreateRange(g));
            }

            if (!_assemblyNameToProjectIdMap.TryGetValue(assembly.Name, out var projectIds))
            {
                projectIds = ImmutableArray<ProjectId>.Empty;
            }

            return projectIds;
        }

        private async Task<ISymbol> GetSymbolAsync(Solution solution, ProjectId projectId, string symbolId, CancellationToken cancellationToken)
        {
            var project = solution.GetProject(projectId);
            if (project.SupportsCompilation)
            {
                var comp = await solution.GetProject(projectId).GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var symbols = DocumentationCommentId.GetSymbolsForDeclarationId(symbolId, comp).ToList();

                if (symbols.Count == 1)
                {
                    return symbols[0];
                }
                else if (symbols.Count > 1)
                {
#if false
                // if we have multiple matches, use the same index that it appeared as in the original solution.
                var originalComp = await this.originalSolution.GetProject(projectId).GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var originalSymbols = DocumentationCommentId.GetSymbolsForDeclarationId(symbolId, originalComp).ToList();
                var index = originalSymbols.IndexOf(originalSymbol);
                if (index >= 0 && index <= symbols.Count)
                {
                    return symbols[index];
                }
#else
                    return symbols[0];
#endif
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the current declarations for the specified symbol.
        /// </summary>
        public async Task<IReadOnlyList<SyntaxNode>> GetCurrentDeclarationsAsync(ISymbol symbol, CancellationToken cancellationToken = default)
        {
            var currentSymbol = await this.GetCurrentSymbolAsync(symbol, cancellationToken).ConfigureAwait(false);
            return this.GetDeclarations(currentSymbol).ToImmutableReadOnlyListOrEmpty();
        }

        /// <summary>
        /// Gets the declaration syntax nodes for a given symbol.
        /// </summary>
        private IEnumerable<SyntaxNode> GetDeclarations(ISymbol symbol)
        {
            return symbol.DeclaringSyntaxReferences
                         .Select(sr => sr.GetSyntax())
                         .Select(n => SyntaxGenerator.GetGenerator(_originalSolution.Workspace, n.Language).GetDeclaration(n))
                         .Where(d => d != null);
        }

        /// <summary>
        /// Gets the best declaration node for adding members.
        /// </summary>
        private bool TryGetBestDeclarationForSingleEdit(ISymbol symbol, out SyntaxNode declaration)
        {
            declaration = GetDeclarations(symbol).FirstOrDefault();
            return declaration != null;
        }

        /// <summary>
        /// An action that make changes to a declaration node within a <see cref="SyntaxTree"/>.
        /// </summary>
        /// <param name="editor">The <see cref="DocumentEditor"/> to apply edits to.</param>
        /// <param name="declaration">The declaration to edit.</param>
        /// <returns></returns>
        public delegate void DeclarationEditAction(DocumentEditor editor, SyntaxNode declaration);

        /// <summary>
        /// An action that make changes to a declaration node within a <see cref="SyntaxTree"/>.
        /// </summary>
        /// <param name="editor">The <see cref="DocumentEditor"/> to apply edits to.</param>
        /// <param name="declaration">The declaration to edit.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns></returns>
        public delegate Task AsyncDeclarationEditAction(DocumentEditor editor, SyntaxNode declaration, CancellationToken cancellationToken);

        /// <summary>
        /// Enables editing the definition of one of the symbol's declarations.
        /// Partial types and methods may have more than one declaration.
        /// </summary>
        /// <param name="symbol">The symbol to edit.</param>
        /// <param name="editAction">The action that makes edits to the declaration.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
        /// <returns>The new symbol including the changes.</returns>
        public async Task<ISymbol> EditOneDeclarationAsync(
            ISymbol symbol,
            AsyncDeclarationEditAction editAction,
            CancellationToken cancellationToken = default)
        {
            var currentSymbol = await this.GetCurrentSymbolAsync(symbol, cancellationToken).ConfigureAwait(false);

            CheckSymbolArgument(currentSymbol, symbol);
            if (TryGetBestDeclarationForSingleEdit(currentSymbol, out var declaration))
            {
                return await this.EditDeclarationAsync(currentSymbol, declaration, editAction, cancellationToken).ConfigureAwait(false);
            }

            return null;
        }

        /// <summary>
        /// Enables editing the definition of one of the symbol's declarations.
        /// Partial types and methods may have more than one declaration.
        /// </summary>
        /// <param name="symbol">The symbol to edit.</param>
        /// <param name="editAction">The action that makes edits to the declaration.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
        /// <returns>The new symbol including the changes.</returns>
        public Task<ISymbol> EditOneDeclarationAsync(
            ISymbol symbol,
            DeclarationEditAction editAction,
            CancellationToken cancellationToken = default)
        {
            return this.EditOneDeclarationAsync(
                symbol,
                (e, d, c) =>
            {
                editAction(e, d);
                return Task.CompletedTask;
            },
            cancellationToken);
        }

        private void CheckSymbolArgument(ISymbol currentSymbol, ISymbol argSymbol)
        {
            if (currentSymbol == null)
            {
                throw new ArgumentException(string.Format(WorkspacesResources.The_symbol_0_cannot_be_located_within_the_current_solution, argSymbol.Name));
            }
        }

        private async Task<ISymbol> EditDeclarationAsync(
            ISymbol currentSymbol,
            SyntaxNode declaration,
            AsyncDeclarationEditAction editAction,
            CancellationToken cancellationToken)
        {
            var doc = _currentSolution.GetDocument(declaration.SyntaxTree);
            var editor = await DocumentEditor.CreateAsync(doc, cancellationToken).ConfigureAwait(false);

            editor.TrackNode(declaration);
            await editAction(editor, declaration, cancellationToken).ConfigureAwait(false);

            var newDoc = editor.GetChangedDocument();
            _currentSolution = newDoc.Project.Solution;

            // try to find new symbol by looking up via original declaration
            var model = await newDoc.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var newDeclaration = model.SyntaxTree.GetRoot(cancellationToken).GetCurrentNode(declaration);
            if (newDeclaration != null)
            {
                var newSymbol = model.GetDeclaredSymbol(newDeclaration, cancellationToken);
                if (newSymbol != null)
                {
                    return newSymbol;
                }
            }

            // otherwise fallback to rebinding with original symbol
            return await this.GetCurrentSymbolAsync(currentSymbol, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Enables editing the definition of one of the symbol's declarations.
        /// Partial types and methods may have more than one declaration.
        /// </summary>
        /// <param name="symbol">The symbol to edit.</param>
        /// <param name="location">A location within one of the symbol's declarations.</param>
        /// <param name="editAction">The action that makes edits to the declaration.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
        /// <returns>The new symbol including the changes.</returns>
        public Task<ISymbol> EditOneDeclarationAsync(
            ISymbol symbol,
            Location location,
            AsyncDeclarationEditAction editAction,
            CancellationToken cancellationToken = default)
        {
            var sourceTree = location.SourceTree;

            var doc = _currentSolution.GetDocument(sourceTree) ?? _originalSolution.GetDocument(sourceTree);
            if (doc != null)
            {
                return EditOneDeclarationAsync(symbol, doc.Id, location.SourceSpan.Start, editAction, cancellationToken);
            }

            throw new ArgumentException("The location specified is not part of the solution.", nameof(location));
        }

        /// <summary>
        /// Enables editing the definition of one of the symbol's declarations.
        /// Partial types and methods may have more than one declaration.
        /// </summary>
        /// <param name="symbol">The symbol to edit.</param>
        /// <param name="location">A location within one of the symbol's declarations.</param>
        /// <param name="editAction">The action that makes edits to the declaration.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
        /// <returns>The new symbol including the changes.</returns>
        public Task<ISymbol> EditOneDeclarationAsync(
            ISymbol symbol,
            Location location,
            DeclarationEditAction editAction,
            CancellationToken cancellationToken = default)
        {
            return this.EditOneDeclarationAsync(
                symbol,
                location,
                (e, d, c) =>
                {
                    editAction(e, d);
                    return Task.CompletedTask;
                },
                cancellationToken);
        }

        private async Task<ISymbol> EditOneDeclarationAsync(
            ISymbol symbol,
            DocumentId documentId,
            int position,
            AsyncDeclarationEditAction editAction,
            CancellationToken cancellationToken = default)
        {
            var currentSymbol = await this.GetCurrentSymbolAsync(symbol, cancellationToken).ConfigureAwait(false);
            CheckSymbolArgument(currentSymbol, symbol);

            var decl = this.GetDeclarations(currentSymbol).FirstOrDefault(d =>
            {
                var doc = _currentSolution.GetDocument(d.SyntaxTree);
                return doc is { Id: documentId } && d.FullSpan.IntersectsWith(position);
            });

            if (decl == null)
            {
                throw new ArgumentNullException(WorkspacesResources.The_position_is_not_within_the_symbol_s_declaration, nameof(position));
            }

            return await this.EditDeclarationAsync(currentSymbol, decl, editAction, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Enables editing the symbol's declaration where the member is also declared.
        /// Partial types and methods may have more than one declaration.
        /// </summary>
        /// <param name="symbol">The symbol to edit.</param>
        /// <param name="member">A symbol whose declaration is contained within one of the primary symbol's declarations.</param>
        /// <param name="editAction">The action that makes edits to the declaration.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
        /// <returns>The new symbol including the changes.</returns>
        public async Task<ISymbol> EditOneDeclarationAsync(
            ISymbol symbol,
            ISymbol member,
            AsyncDeclarationEditAction editAction,
            CancellationToken cancellationToken = default)
        {
            var currentSymbol = await this.GetCurrentSymbolAsync(symbol, cancellationToken).ConfigureAwait(false);
            CheckSymbolArgument(currentSymbol, symbol);

            var currentMember = await this.GetCurrentSymbolAsync(member, cancellationToken).ConfigureAwait(false);
            CheckSymbolArgument(currentMember, member);

            // get first symbol declaration that encompasses at least one of the member declarations
            var memberDecls = this.GetDeclarations(currentMember).ToList();
            var declaration = this.GetDeclarations(currentSymbol).FirstOrDefault(d => memberDecls.Any(md => md.SyntaxTree == d.SyntaxTree && d.FullSpan.IntersectsWith(md.FullSpan)));

            if (declaration == null)
            {
                throw new ArgumentException(string.Format(WorkspacesResources.The_member_0_is_not_declared_within_the_declaration_of_the_symbol, member.Name));
            }

            return await this.EditDeclarationAsync(currentSymbol, declaration, editAction, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Enables editing the symbol's declaration where the member is also declared.
        /// Partial types and methods may have more than one declaration.
        /// </summary>
        /// <param name="symbol">The symbol to edit.</param>
        /// <param name="member">A symbol whose declaration is contained within one of the primary symbol's declarations.</param>
        /// <param name="editAction">The action that makes edits to the declaration.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
        /// <returns>The new symbol including the changes.</returns>
        public Task<ISymbol> EditOneDeclarationAsync(
            ISymbol symbol,
            ISymbol member,
            DeclarationEditAction editAction,
            CancellationToken cancellationToken = default)
        {
            return this.EditOneDeclarationAsync(
                symbol,
                member,
                (e, d, c) =>
                {
                    editAction(e, d);
                    return Task.CompletedTask;
                },
                cancellationToken);
        }

        /// <summary>
        /// Enables editing all the symbol's declarations. 
        /// Partial types and methods may have more than one declaration.
        /// </summary>
        /// <param name="symbol">The symbol to be edited.</param>
        /// <param name="editAction">The action that makes edits to the declaration.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
        /// <returns>The new symbol including the changes.</returns>
        public async Task<ISymbol> EditAllDeclarationsAsync(
            ISymbol symbol,
            AsyncDeclarationEditAction editAction,
            CancellationToken cancellationToken = default)
        {
            var currentSymbol = await this.GetCurrentSymbolAsync(symbol, cancellationToken).ConfigureAwait(false);
            CheckSymbolArgument(currentSymbol, symbol);

            var declsByDocId = this.GetDeclarations(currentSymbol).ToLookup(d => _currentSolution.GetDocument(d.SyntaxTree).Id);

            var solutionEditor = new SolutionEditor(_currentSolution);

            foreach (var declGroup in declsByDocId)
            {
                var docId = declGroup.Key;
                var editor = await solutionEditor.GetDocumentEditorAsync(docId, cancellationToken).ConfigureAwait(false);

                foreach (var decl in declGroup)
                {
                    editor.TrackNode(decl); // ensure the declaration gets tracked
                    await editAction(editor, decl, cancellationToken).ConfigureAwait(false);
                }
            }

            _currentSolution = solutionEditor.GetChangedSolution();

            // try to find new symbol by looking up via original declarations
            foreach (var declGroup in declsByDocId)
            {
                var doc = _currentSolution.GetDocument(declGroup.Key);
                var model = await doc.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                foreach (var decl in declGroup)
                {
                    var newDeclaration = model.SyntaxTree.GetRoot(cancellationToken).GetCurrentNode(decl);
                    if (newDeclaration != null)
                    {
                        var newSymbol = model.GetDeclaredSymbol(newDeclaration);
                        if (newSymbol != null)
                        {
                            return newSymbol;
                        }
                    }
                }
            }

            // otherwise fallback to rebinding with original symbol
            return await GetCurrentSymbolAsync(symbol, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Enables editing all the symbol's declarations. 
        /// Partial types and methods may have more than one declaration.
        /// </summary>
        /// <param name="symbol">The symbol to be edited.</param>
        /// <param name="editAction">The action that makes edits to the declaration.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
        /// <returns>The new symbol including the changes.</returns>
        public Task<ISymbol> EditAllDeclarationsAsync(
            ISymbol symbol,
            DeclarationEditAction editAction,
            CancellationToken cancellationToken = default)
        {
            return this.EditAllDeclarationsAsync(
                symbol,
                (e, d, c) =>
                {
                    editAction(e, d);
                    return Task.CompletedTask;
                },
                cancellationToken);
        }
    }
}
