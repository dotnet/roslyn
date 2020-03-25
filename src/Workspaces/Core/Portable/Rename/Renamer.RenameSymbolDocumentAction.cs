// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Rename
{
    public static partial class Renamer
    {
        /// <summary>
        /// Action that will rename a type to match the current document name
        /// </summary>
        internal class RenameSymbolDocumentAction : RenameDocumentAction
        {
            private readonly RenameSymbolDocumentActionAnalysis _analysis;

            private RenameSymbolDocumentAction(
                Document document,
                RenameSymbolDocumentActionAnalysis analysis,
                OptionSet optionSet,
                ImmutableArray<string> errors)
                : base(document.Id, optionSet, errors)
            {
                _analysis = analysis;
            }

            public override string GetDescription(CultureInfo? culture)
            => string.Format(WorkspacesResources.ResourceManager.GetString("Rename_0_to_1", culture ?? WorkspacesResources.Culture), _analysis.OriginalDocumentName, _analysis.NewDocumentName);

            internal override async Task<Solution> GetModifiedSolutionAsync(Solution solution, CancellationToken cancellationToken)
            {
                // Always make sure the document name is correctly updated
                solution = solution.WithDocumentName(DocumentId, _analysis.NewDocumentName);

                if (_analysis.Symbol == null)
                {
                    // If the analysis couldn't find the correct original symbol, 
                    // shortcut anymore work and just return the solution
                    return solution;
                }

                var document = solution.GetRequiredDocument(DocumentId);
                var matchingTypeDeclaration = await GetMatchingTypeDeclarationAsync(document, _analysis.Symbol.Name, cancellationToken).ConfigureAwait(false);

                if (matchingTypeDeclaration is object)
                {
                    var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    var symbol = semanticModel.GetDeclaredSymbol(matchingTypeDeclaration, cancellationToken);

                    solution = await RenameSymbolAsync(solution, symbol, _analysis.NewSymbolName, OptionSet, cancellationToken).ConfigureAwait(false);
                }

                return solution;
            }

            private static async Task<SyntaxNode> GetMatchingTypeDeclarationAsync(Document document, string name, CancellationToken cancellationToken)
            {
                var syntaxRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

                var typeDeclarations = syntaxRoot.DescendantNodesAndSelf().Where(syntaxFacts.IsTypeDeclaration);
                return typeDeclarations.FirstOrDefault(d => syntaxFacts.GetDisplayName(d, DisplayNameOptions.None).Equals(name, StringComparison.OrdinalIgnoreCase));
            }

            public static async Task<RenameSymbolDocumentAction> CreateAsync(Document document, string newName, OptionSet optionSet, CancellationToken cancellationToken)
            {
                var analysis = await RenameSymbolDocumentActionAnalysis.CreateAsync(document, newName, optionSet, cancellationToken).ConfigureAwait(false);

                // TODO: Detect naming conflicts ahead of time
                return new RenameSymbolDocumentAction(document, analysis, optionSet, ImmutableArray<string>.Empty);
            }

            private readonly struct RenameSymbolDocumentActionAnalysis
            {
                public string OriginalDocumentName { get; }
                public string NewDocumentName { get; }
                public string NewSymbolName { get; }
                public ISymbol? Symbol { get; }
                private RenameSymbolDocumentActionAnalysis(
                    Document document,
                    string newName,
                    ISymbol? symbol = null)
                {
                    OriginalDocumentName = document.Name;
                    NewDocumentName = newName;
                    NewSymbolName = Path.GetFileNameWithoutExtension(newName);
                    Symbol = symbol;
                }

                public static async Task<RenameSymbolDocumentActionAnalysis> CreateAsync(Document document, string newName, OptionSet optionSet, CancellationToken cancellationToken)
                {
                    var originalSymbolName = Path.GetFileNameWithoutExtension(document.Name);
                    var matchingDeclaration = await GetMatchingTypeDeclarationAsync(document, originalSymbolName, cancellationToken).ConfigureAwait(false);

                    if (matchingDeclaration is object)
                    {
                        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                        var symbol = semanticModel.GetDeclaredSymbol(matchingDeclaration, cancellationToken);
                        return new RenameSymbolDocumentActionAnalysis(document, newName, symbol);
                    }

                    return new RenameSymbolDocumentActionAnalysis(document, newName);
                }
            }
        }

    }
}
