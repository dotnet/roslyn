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
    public class SymbolEditor
    {
        private readonly Solution originalSolution;
        private Solution currentSolution;

        public SymbolEditor(Solution solution)
        {
            this.originalSolution = solution;
            this.currentSolution = solution;
        }

        public SymbolEditor(Document document)
            : this(document.Project.Solution)
        {
        }

        /// <summary>
        /// The original solution.
        /// </summary>
        public Solution OriginalSolution
        {
            get { return this.originalSolution; }
        }

        /// <summary>
        /// The current solution.
        /// </summary>
        public Solution CurrentSolution
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
        /// Get's the current symbol for a declaration at a specified position within a documment.
        /// </summary>
        private Task<ISymbol> GetCurrentSymbolAsync(DocumentId docId, int position, DeclarationKind kind, CancellationToken cancellationToken)
        {
            return this.GetSymbolAsync(this.currentSolution, docId, position, kind, cancellationToken);
        }

        private DeclarationKind GetKind(SyntaxNode declaration)
        {
            return SyntaxGenerator.GetGenerator(this.currentSolution.Workspace, declaration.Language).GetDeclarationKind(declaration);
        }

        private async Task<ISymbol> GetSymbolAsync(Solution solution, DocumentId docId, int position, DeclarationKind kind, CancellationToken cancellationToken)
        {
            var doc = solution.GetDocument(docId);
            var model = await doc.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var generator = SyntaxGenerator.GetGenerator(doc);
            var node = model.SyntaxTree.GetRoot().FindToken(position).Parent;
            var decl = generator.GetDeclaration(node, kind);

            if (decl != null)
            {
                return model.GetDeclaredSymbol(decl, cancellationToken);
            }
            else
            {
                return null;
            }
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
        /// <param name="declarationEditor">The function that produces the changed declaration.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
        /// <returns>The new symbol including the changes.</returns>
        public async Task<ISymbol> EditOneDeclarationAsync(
            ISymbol symbol,
            Func<SyntaxNode, SyntaxGenerator, SyntaxNode> declarationEditor,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var currentSymbol = await this.GetCurrentSymbolAsync(symbol, cancellationToken).ConfigureAwait(false);

            CheckSymbolArgument(currentSymbol, symbol);

            SyntaxNode declaration;
            if (TryGetBestDeclarationForSingleEdit(currentSymbol, out declaration))
            {
                return await this.EditDeclarationAsync(currentSymbol, declaration, declarationEditor, cancellationToken).ConfigureAwait(false);
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

        private async Task<ISymbol> EditDeclarationAsync(ISymbol currentSymbol, SyntaxNode declaration, Func<SyntaxNode, SyntaxGenerator, SyntaxNode> declarationEditor, CancellationToken cancellationToken)
        {
            var doc = this.currentSolution.GetDocument(declaration.SyntaxTree);
            var root = declaration.SyntaxTree.GetRoot();
            var generator = SyntaxGenerator.GetGenerator(this.currentSolution.Workspace, declaration.Language);
            var newDecl = declarationEditor(declaration, generator);

            SyntaxNode newRoot = generator.ReplaceDeclaration(root, declaration, newDecl);

            var newDoc = doc.WithSyntaxRoot(newRoot);
            this.currentSolution = newDoc.Project.Solution;

            if (newDecl != null)
            {
                return await this.GetCurrentSymbolAsync(doc.Id, declaration.Span.Start, GetKind(newDecl), cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await this.GetCurrentSymbolAsync(currentSymbol, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Enables editting the definition of one of the symbol's declarations.
        /// Partial types and methods may have more than one declaration.
        /// </summary>
        /// <param name="symbol">The symbol to edit.</param>
        /// <param name="location">A location within one of the symbol's declarations.</param>
        /// <param name="declarationEditor">The function that produces the changed declaration.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
        /// <returns>The new symbol including the changes.</returns>
        public async Task<ISymbol> EditOneDeclarationAsync(
            ISymbol symbol,
            Location location,
            Func<SyntaxNode, SyntaxGenerator, SyntaxNode> declarationEditor,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var sourceTree = location.SourceTree;

            var doc = this.currentSolution.GetDocument(sourceTree);
            if (doc != null)
            {
                return await this.EditOneDeclarationAsync(symbol, doc.Id, location.SourceSpan.Start, declarationEditor, cancellationToken).ConfigureAwait(false);
            }

            doc = this.originalSolution.GetDocument(sourceTree);
            if (doc != null)
            {
                return await this.EditOneDeclarationAsync(symbol, doc.Id, location.SourceSpan.Start, declarationEditor, cancellationToken).ConfigureAwait(false);
            }

            throw new ArgumentException("The location specified is not part of the solution.", nameof(location));
        }

        private async Task<ISymbol> EditOneDeclarationAsync(
            ISymbol symbol,
            DocumentId documentId,
            int position,
            Func<SyntaxNode, SyntaxGenerator, SyntaxNode> declarationEditor,
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

            return await this.EditDeclarationAsync(currentSymbol, decl, declarationEditor, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Enables editting the symbol's declaration where the member is also declared.
        /// Partial types and methods may have more than one declaration.
        /// </summary>
        /// <param name="symbol">The symbol to edit.</param>
        /// <param name="member">A symbol whose declaration is contained within one of the primary symbol's declarations.</param>
        /// <param name="declarationEditor">The function that produces the changed declaration.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
        /// <returns>The new symbol including the changes.</returns>
        public async Task<ISymbol> EditOneDeclarationAsync(
            ISymbol symbol,
            ISymbol member,
            Func<SyntaxNode, SyntaxGenerator, SyntaxNode> declarationEditor,
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

            return await this.EditDeclarationAsync(currentSymbol, declaration, declarationEditor, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Enables editting all the symbol's declarations. 
        /// Partial types and methods may have more than one declaration.
        /// </summary>
        /// <param name="symbol">The symbol to be editted.</param>
        /// <param name="declarationEditor">The function that produces a changed declaration.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
        /// <returns>The new symbol including the changes.</returns>
        public async Task<ISymbol> EditAllDeclarationsAsync(ISymbol symbol, Func<SyntaxNode, SyntaxGenerator, SyntaxNode> declarationEditor, CancellationToken cancellationToken = default(CancellationToken))
        {
            var currentSymbol = await this.GetCurrentSymbolAsync(symbol, cancellationToken).ConfigureAwait(false);

            var docMap = new Dictionary<SyntaxTree, Document>();
            var changeMap = new Dictionary<SyntaxNode, SyntaxNode>();

            foreach (var decls in this.GetDeclarations(currentSymbol).GroupBy(d => d.SyntaxTree))
            {
                var doc = this.currentSolution.GetDocument(decls.Key);
                docMap.Add(decls.Key, doc);

                var root = decls.Key.GetRoot();

                var generator = SyntaxGenerator.GetGenerator(doc);
                var changes = decls.Select(d => new KeyValuePair<SyntaxNode, SyntaxNode>(d, declarationEditor(d, generator)));
                changeMap.AddRange(changes);

                var newRoot = root.ReplaceNodes(decls, (original, rewritten) => changeMap[original]);
                var newDoc = doc.WithSyntaxRoot(newRoot);

                this.currentSolution = newDoc.Project.Solution;
            }

            // try to find new symbol using the first lexically changed decl in one of the trees, because the position will not have changed
            var firstTreeChanges = changeMap.GroupBy(kvp => kvp.Key.SyntaxTree).FirstOrDefault();
            if (firstTreeChanges != null)
            {
                var doc = docMap[firstTreeChanges.Key];

                var firstChangedDecl = firstTreeChanges.OrderBy(kvp => kvp.Key.SpanStart).FirstOrDefault().Value;
                if (firstChangedDecl != null)
                {
                    return await GetCurrentSymbolAsync(doc.Id, firstChangedDecl.SpanStart, GetKind(firstChangedDecl), cancellationToken).ConfigureAwait(false);
                }
            }

            // if prior method fails (possibly due to declaration being removed), attempt to rebind the original symbol
            return await GetCurrentSymbolAsync(symbol, cancellationToken).ConfigureAwait(false);
        }
    }
}
