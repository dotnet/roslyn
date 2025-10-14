// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Utilities;

namespace Microsoft.CodeAnalysis.Rename;

public static partial class Renamer
{
    /// <summary>
    /// Action that will rename a type to match the current document name. Works by finding a type matching the origanl name of the document (case insensitive) 
    /// and updating that type.
    /// </summary>
    //  https://github.com/dotnet/roslyn/issues/43461 tracks adding more complicated heuristics to matching type and file name. 
    internal sealed class RenameSymbolDocumentAction : RenameDocumentAction
    {
        private readonly AnalysisResult _analysis;

        private RenameSymbolDocumentAction(
            AnalysisResult analysis)
            : base([])
        {
            _analysis = analysis;
        }

        public override string GetDescription(CultureInfo? culture)
            => string.Format(WorkspacesResources.ResourceManager.GetString("Rename_0_to_1", culture ?? WorkspacesResources.Culture)!, _analysis.OriginalDocumentName, _analysis.NewDocumentName);

        internal override async Task<Solution> GetModifiedSolutionAsync(Document document, DocumentRenameOptions options, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;

            // Get only types matching the original document name by
            // passing a document back with the original name. That way
            // even if the document name changed, we're updating the types
            // that are the same name as the analysis
            var matchingTypeDeclaration = await GetMatchingTypeDeclarationAsync(
                document.WithName(_analysis.OriginalDocumentName),
                cancellationToken).ConfigureAwait(false);

            if (matchingTypeDeclaration is object)
            {
                var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var symbol = semanticModel.GetRequiredDeclaredSymbol(matchingTypeDeclaration, cancellationToken);

                var symbolRenameOptions = new SymbolRenameOptions(
                    RenameOverloads: false,
                    RenameInComments: options.RenameMatchingTypeInComments,
                    RenameInStrings: options.RenameMatchingTypeInStrings,
                    RenameFile: false);

                solution = await RenameSymbolAsync(solution, symbol, symbolRenameOptions, _analysis.NewSymbolName, cancellationToken).ConfigureAwait(false);
            }

            return solution;
        }

        /// <summary>
        /// Finds a matching type such that the display name of the type matches the name passed in, ignoring case. Case isn't used because
        /// documents with name "Foo.cs" and "foo.cs" should still have the same type name.
        /// Also supports nested types following the Outer.Inner.cs convention.
        /// </summary>
        private static async Task<SyntaxNode?> GetMatchingTypeDeclarationAsync(Document document, CancellationToken cancellationToken)
        {
            var syntaxRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var typeDeclarations = syntaxRoot.DescendantNodesAndSelf(n => !syntaxFacts.IsMethodBody(n)).Where(syntaxFacts.IsTypeDeclaration);
            
            // First check for simple type name match (e.g., "Foo.cs" with type "Foo")
            var simpleMatch = typeDeclarations.FirstOrDefault(d => WorkspacePathUtilities.TypeNameMatchesDocumentName(document, d, syntaxFacts));
            if (simpleMatch != null)
                return simpleMatch;
            
            // Then check for nested type match (e.g., "Outer.Inner.cs" with type Inner inside Outer)
            foreach (var declaration in typeDeclarations)
            {
                var symbol = semanticModel.GetDeclaredSymbol(declaration, cancellationToken);
                if (symbol != null && WorkspacePathUtilities.SymbolMatchesDocumentName(document, symbol))
                {
                    return declaration;
                }
            }
            
            return null;
        }

        public static async Task<RenameSymbolDocumentAction?> TryCreateAsync(Document document, string newName, CancellationToken cancellationToken)
        {
            var analysis = await AnalyzeAsync(document, newName, cancellationToken).ConfigureAwait(false);

            return analysis.HasValue
                ? new RenameSymbolDocumentAction(analysis.Value)
                : null;
        }

        private static async Task<AnalysisResult?> AnalyzeAsync(Document document, string newDocumentName, CancellationToken cancellationToken)
        {
            // TODO: Detect naming conflicts ahead of time
            var documentWithNewName = document.WithName(newDocumentName);
            var originalSymbolName = WorkspacePathUtilities.GetTypeNameFromDocumentName(document);
            var newTypeName = WorkspacePathUtilities.GetTypeNameFromDocumentName(documentWithNewName);

            if (originalSymbolName is null || newTypeName is null)
            {
                return null;
            }

            var matchingDeclaration = await GetMatchingTypeDeclarationAsync(document, cancellationToken).ConfigureAwait(false);

            if (matchingDeclaration is null)
            {
                return null;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbol = semanticModel.GetDeclaredSymbol(matchingDeclaration, cancellationToken);

            if (symbol is null)
            {
                return null;
            }

            // Check if the type already matches the new document name (no rename needed)
            // For simple types: "Foo.cs" -> type "Foo"
            if (WorkspacePathUtilities.TypeNameMatchesDocumentName(documentWithNewName, symbol.Name))
            {
                return null;
            }

            // For nested types: "Outer.Inner.cs" -> type Inner inside Outer
            if (WorkspacePathUtilities.SymbolMatchesDocumentName(documentWithNewName, symbol))
            {
                return null;
            }

            return new AnalysisResult(
                document,
                newDocumentName,
                newTypeName,
                symbol.Name);
        }

        private readonly struct AnalysisResult(
            Document document,
            string newDocumentName,
            string newSymbolName,
            string originalSymbolName)
        {
            /// <summary>
            /// Name of the document that the action was produced for.
            /// </summary>
            public string OriginalDocumentName { get; } = document.Name;

            /// <summary>
            /// The new document name that will be used.
            /// </summary>
            public string NewDocumentName { get; } = newDocumentName;

            /// <summary>
            /// The original name of the symbol that will be changed.
            /// </summary>
            public string OriginalSymbolName { get; } = originalSymbolName;

            /// <summary>
            /// The new name for the symbol.
            /// </summary>
            public string NewSymbolName { get; } = newSymbolName;
        }
    }
}
