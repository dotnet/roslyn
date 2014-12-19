// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    /// <summary>
    /// An editor for making changes to symbol source declarations.
    /// </summary>
    public sealed class SymbolEditor
    {
        private readonly Solution originalSolution;
        private Solution currentSolution;

        private SymbolEditor(Solution solution)
        {
            this.originalSolution = solution;
            this.currentSolution = solution;
        }

        public static SymbolEditor Create(Solution solution)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            return new SymbolEditor(solution);
        }

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
        public Solution OriginalSolution
        {
            get { return this.originalSolution; }
        }

        /// <summary>
        /// The solution with the edits applied.
        /// </summary>
        public Solution ChangedSolution
        {
            get { return this.currentSolution; }
        }

        /// <summary>
        /// The documents changed since the <see cref="SymbolEditor"/> was constructed.
        /// </summary>
        public IEnumerable<Document> GetChangedDocuments()
        {
            var solutionChanges = this.currentSolution.GetChanges(this.originalSolution);

            foreach (var projectChanges in solutionChanges.GetProjectChanges())
            {
                foreach (var id in projectChanges.GetAddedDocuments())
                {
                    yield return this.currentSolution.GetDocument(id);
                }

                foreach (var id in projectChanges.GetChangedDocuments())
                {
                    yield return this.currentSolution.GetDocument(id);
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
        public async Task<ISymbol> GetCurrentSymbolAsync(ISymbol symbol, CancellationToken cancellationToken = default(CancellationToken))
        {
            var symbolId = DocumentationCommentId.CreateDeclarationId(symbol);

            // check to see if symbol is from current solution
            var project = this.currentSolution.GetProject(symbol.ContainingAssembly);
            if (project != null)
            {
                return await GetSymbolAsync(this.currentSolution, project.Id, symbolId, cancellationToken).ConfigureAwait(false);
            }

            // check to see if it is from original solution
            project = this.originalSolution.GetProject(symbol.ContainingAssembly);
            if (project != null)
            {
                return await GetSymbolAsync(this.currentSolution, project.Id, symbolId, cancellationToken).ConfigureAwait(false);
            }

            // try to find symbol from any project (from current solution) with matching assembly name
            foreach (var projectId in this.GetProjectsForAssembly(symbol.ContainingAssembly))
            {
                var currentSymbol = await GetSymbolAsync(this.currentSolution, projectId, symbolId, cancellationToken).ConfigureAwait(false);
                if (currentSymbol != null)
                {
                    return currentSymbol;
                }
            }

            return null;
        }

        private ImmutableDictionary<string, ImmutableArray<ProjectId>> assemblyNameToProjectIdMap;

        private ImmutableArray<ProjectId> GetProjectsForAssembly(IAssemblySymbol assembly)
        {
            if (this.assemblyNameToProjectIdMap == null)
            {
                this.assemblyNameToProjectIdMap = this.originalSolution.Projects
                    .ToLookup(p => p.AssemblyName, p => p.Id)
                    .ToImmutableDictionary(g => g.Key, g => ImmutableArray.CreateRange(g));
            }

            ImmutableArray<ProjectId> projectIds;
            if (!this.assemblyNameToProjectIdMap.TryGetValue(assembly.Name, out projectIds))
            {
                projectIds = ImmutableArray<ProjectId>.Empty;
            }

            return projectIds;
        }

        private async Task<ISymbol> GetSymbolAsync(Solution solution, ProjectId projectId, string symbolId, CancellationToken cancellationToken)
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

            return null;
        }

        /// <summary>
        /// Gets the declaration syntax nodes for a given symbol.
        /// </summary>
        private IEnumerable<SyntaxNode> GetDeclarations(ISymbol symbol)
        {
            return symbol.DeclaringSyntaxReferences
                         .Select(sr => sr.GetSyntax())
                         .Select(n => SyntaxGenerator.GetGenerator(this.originalSolution.Workspace, n.Language).GetDeclaration(n))
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
        /// Enables editting the definition of one of the symbol's declarations.
        /// Partial types and methods may have more than one declaration.
        /// </summary>
        /// <param name="symbol">The symbol to edit.</param>
        /// <param name="editAction">The action that makes edits to the declaration.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
        /// <returns>The new symbol including the changes.</returns>
        public async Task<ISymbol> EditOneDeclarationAsync(
            ISymbol symbol,
            DeclarationEditAction editAction,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var currentSymbol = await this.GetCurrentSymbolAsync(symbol, cancellationToken).ConfigureAwait(false);

            CheckSymbolArgument(currentSymbol, symbol);

            SyntaxNode declaration;
            if (TryGetBestDeclarationForSingleEdit(currentSymbol, out declaration))
            {
                return await this.EditDeclarationAsync(currentSymbol, declaration, editAction, cancellationToken).ConfigureAwait(false);
            }

            return null;
        }

        private void CheckSymbolArgument(ISymbol currentSymbol, ISymbol argSymbol)
        {
            if (currentSymbol == null)
            {
                throw new ArgumentException(string.Format("The symbol '{0}' cannot be located within the current solution.".NeedsLocalization(), argSymbol.Name));
            }
        }

        private async Task<ISymbol> EditDeclarationAsync(ISymbol currentSymbol, SyntaxNode declaration, DeclarationEditAction editAction, CancellationToken cancellationToken)
        {
            var doc = this.currentSolution.GetDocument(declaration.SyntaxTree);
            var root = declaration.SyntaxTree.GetRoot();

            var editor = SyntaxEditor.Create(this.currentSolution.Workspace, root);
            editor.TrackNode(declaration);

            editAction(editor, declaration);

            var newRoot = editor.GetChangedRoot();
            var newDoc = doc.WithSyntaxRoot(newRoot);
            this.currentSolution = newDoc.Project.Solution;

            // try to find new symbol by looking up via original declaration
            var model = await newDoc.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var newDeclaration = model.SyntaxTree.GetRoot().GetCurrentNode(declaration);
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
        /// Enables editting the definition of one of the symbol's declarations.
        /// Partial types and methods may have more than one declaration.
        /// </summary>
        /// <param name="symbol">The symbol to edit.</param>
        /// <param name="location">A location within one of the symbol's declarations.</param>
        /// <param name="editAction">The action that makes edits to the declaration.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
        /// <returns>The new symbol including the changes.</returns>
        public async Task<ISymbol> EditOneDeclarationAsync(
            ISymbol symbol,
            Location location,
            DeclarationEditAction editAction,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var sourceTree = location.SourceTree;

            var doc = this.currentSolution.GetDocument(sourceTree);
            if (doc != null)
            {
                return await this.EditOneDeclarationAsync(symbol, doc.Id, location.SourceSpan.Start, editAction, cancellationToken).ConfigureAwait(false);
            }

            doc = this.originalSolution.GetDocument(sourceTree);
            if (doc != null)
            {
                return await this.EditOneDeclarationAsync(symbol, doc.Id, location.SourceSpan.Start, editAction, cancellationToken).ConfigureAwait(false);
            }

            throw new ArgumentException("The location specified is not part of the solution.", nameof(location));
        }

        private async Task<ISymbol> EditOneDeclarationAsync(
            ISymbol symbol,
            DocumentId documentId,
            int position,
            DeclarationEditAction editAction,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var currentSymbol = await this.GetCurrentSymbolAsync(symbol, cancellationToken).ConfigureAwait(false);
            CheckSymbolArgument(currentSymbol, symbol);

            var decl = this.GetDeclarations(currentSymbol).FirstOrDefault(d =>
            {
                var doc = this.currentSolution.GetDocument(d.SyntaxTree);
                return doc != null && doc.Id == documentId && d.FullSpan.IntersectsWith(position);
            });

            if (decl == null)
            {
                throw new ArgumentNullException("The position is not within the symbol's declaration".NeedsLocalization(), nameof(position));
            }

            return await this.EditDeclarationAsync(currentSymbol, decl, editAction, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Enables editting the symbol's declaration where the member is also declared.
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
            DeclarationEditAction editAction,
            CancellationToken cancellationToken = default(CancellationToken))
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
                throw new ArgumentException(string.Format("The member '{0}' is not declared within the declaration of the symbol.".NeedsLocalization(), member.Name));
            }

            return await this.EditDeclarationAsync(currentSymbol, declaration, editAction, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Enables editting all the symbol's declarations. 
        /// Partial types and methods may have more than one declaration.
        /// </summary>
        /// <param name="symbol">The symbol to be editted.</param>
        /// <param name="editAction">The action that makes edits to the declaration.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
        /// <returns>The new symbol including the changes.</returns>
        public async Task<ISymbol> EditAllDeclarationsAsync(ISymbol symbol, DeclarationEditAction editAction, CancellationToken cancellationToken = default(CancellationToken))
        {
            var currentSymbol = await this.GetCurrentSymbolAsync(symbol, cancellationToken).ConfigureAwait(false);
            CheckSymbolArgument(currentSymbol, symbol);

            var declsByDocId = this.GetDeclarations(currentSymbol).ToLookup(d => this.currentSolution.GetDocument(d.SyntaxTree).Id);

            foreach (var declGroup in declsByDocId)
            {
                var doc = this.currentSolution.GetDocument(declGroup.Key);
                var root = declGroup.First().SyntaxTree.GetRoot();

                var editor = SyntaxEditor.Create(this.currentSolution.Workspace, root);
                foreach (var decl in declGroup)
                {
                    editor.TrackNode(decl); // ensure the declaration gets tracked
                    editAction(editor, decl);
                }

                var newRoot = editor.GetChangedRoot();
                var newDoc = doc.WithSyntaxRoot(newRoot);
                this.currentSolution = newDoc.Project.Solution;
            }

            // try to find new symbol by looking up via original declarations
            foreach (var declGroup in declsByDocId)
            {
                var doc = this.currentSolution.GetDocument(declGroup.Key);
                var model = await doc.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                foreach (var decl in declGroup)
                {
                    var newDeclaration = model.SyntaxTree.GetRoot().GetCurrentNode(decl);
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
    }
}
