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
                return await GetCurrentSymbolAsync(project.Id, symbolId, cancellationToken).ConfigureAwait(false);
            }

            // check to see if it is from original solution
            project = this.originalSolution.GetProject(symbol.ContainingAssembly);
            if (project != null)
            {
                return await GetCurrentSymbolAsync(project.Id, symbolId, cancellationToken).ConfigureAwait(false);
            }

            // try to find symbol from any project (from current solution) with matching assembly name
            foreach (var projectId in this.GetProjectsForAssembly(symbol.ContainingAssembly))
            {
                var currentSymbol = await GetCurrentSymbolAsync(projectId, symbolId, cancellationToken).ConfigureAwait(false);
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

        private async Task<ISymbol> GetCurrentSymbolAsync(ProjectId projectId, string symbolId, CancellationToken cancellationToken)
        { 
            var comp = await this.currentSolution.GetProject(projectId).GetCompilationAsync(cancellationToken).ConfigureAwait(false);
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
        public IEnumerable<SyntaxNode> GetDeclarations(ISymbol symbol)
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

            SyntaxNode declaration;
            if (TryGetBestDeclarationForSingleEdit(currentSymbol, out declaration))
            {
                var doc = this.currentSolution.GetDocument(declaration.SyntaxTree);
                var root = declaration.SyntaxTree.GetRoot();
                var generator = SyntaxGenerator.GetGenerator(this.currentSolution.Workspace, declaration.Language);
                var newDecl = declarationEditor(declaration, generator);
                var newRoot = root.ReplaceNode(declaration, newDecl);
                var newDoc = doc.WithSyntaxRoot(newRoot);
                this.currentSolution = newDoc.Project.Solution;
                return await this.GetCurrentSymbolAsync(symbol, cancellationToken).ConfigureAwait(false);
            }

            return null;
        }

        /// <summary>
        /// Enables editting the definition of one of the symbol's declarations.
        /// </summary>
        /// <param name="symbol">The symbol to edit.</param>
        /// <param name="location">A location within one of the symbol's declarations.</param>
        /// <param name="declarationEditor">The function that produces the changed declaration.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
        /// <returns>The new symbol including the changes.</returns>
        public Task<ISymbol> EditOneDeclarationAsync(
            ISymbol symbol,
            Location location,
            Func<SyntaxNode, SyntaxGenerator, SyntaxNode> declarationEditor,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var sourceTree = location.SourceTree;
            return this.EditAllDeclarationsAsync(symbol,
                (d, g) =>
                {
                    if (d.SyntaxTree == sourceTree && d.FullSpan.IntersectsWith(location.SourceSpan.Start))
                    {
                        return declarationEditor(d, g);
                    }
                    else
                    {
                        return d;
                    }
                },
                cancellationToken);
        }

        /// <summary>
        /// Enables editting the symbol's declaration where the member is also declared.
        /// </summary>
        /// <param name="symbol">The symbol to edit.</param>
        /// <param name="member">A symbol whose declaration is contained within one of the primary symbol's declarations.</param>
        /// <param name="declarationEditor">The function that produces the changed declaration.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
        /// <returns>The new symbol including the changes.</returns>
        public async Task<ISymbol> EditDeclarationsWithMemberDeclaredAsync(
            ISymbol symbol,
            ISymbol member,
            Func<SyntaxNode, SyntaxGenerator, SyntaxNode> declarationEditor,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var currentMember = await this.GetCurrentSymbolAsync(member, cancellationToken).ConfigureAwait(false);
            var memberDecls = this.GetDeclarations(currentMember);

            return await this.EditAllDeclarationsAsync(symbol,
                (d, g) =>
                {
                    if (memberDecls.Any(md => md.SyntaxTree == d.SyntaxTree && d.FullSpan.IntersectsWith(md.FullSpan)))
                    {
                        return declarationEditor(d, g);
                    }
                    else
                    {
                        return d;
                    }
                },
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Enables editting all the symbol's declarations.
        /// </summary>
        /// <param name="symbol">The symbol to be editted.</param>
        /// <param name="declarationEditor">The function that produces a changed declaration.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
        /// <returns>The new symbol including the changes.</returns>
        public async Task<ISymbol> EditAllDeclarationsAsync(ISymbol symbol, Func<SyntaxNode, SyntaxGenerator, SyntaxNode> declarationEditor, CancellationToken cancellationToken = default(CancellationToken))
        {
            var currentSymbol = await this.GetCurrentSymbolAsync(symbol, cancellationToken).ConfigureAwait(false);

            foreach (var decls in this.GetDeclarations(currentSymbol).GroupBy(d => d.SyntaxTree))
            {
                var doc = this.currentSolution.GetDocument(decls.Key);
                var root = decls.Key.GetRoot();

                var generator = SyntaxGenerator.GetGenerator(doc);
                var newRoot = root.ReplaceNodes(decls, (original, rewritten) => declarationEditor(original, generator));
                var newDoc = doc.WithSyntaxRoot(newRoot);
                this.currentSolution = newDoc.Project.Solution;
            }

            return await this.GetCurrentSymbolAsync(symbol, cancellationToken).ConfigureAwait(false);
        }
    }
}
