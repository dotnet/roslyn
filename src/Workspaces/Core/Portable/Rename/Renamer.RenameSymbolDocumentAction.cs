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
                RenameSymbolDocumentActionAnalysis analysis,
                OptionSet optionSet,
                ImmutableArray<ErrorResource> errors)
                : base(errors, optionSet)
            {
                _analysis = analysis;
            }

            public override string GetDescription(CultureInfo? culture)
                => string.Format(WorkspacesResources.ResourceManager.GetString("Rename_0_to_1", culture ?? WorkspacesResources.Culture)!, _analysis.OriginalDocumentName, _analysis.NewDocumentName);

            internal override async Task<Solution> GetModifiedSolutionAsync(Document document, CancellationToken cancellationToken)
            {
                var solution = document.Project.Solution;
                var matchingTypeDeclaration = await GetMatchingTypeDeclarationAsync(document, _analysis.OriginalSymbolName!, cancellationToken).ConfigureAwait(false);

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

            public static async Task<RenameSymbolDocumentAction?> CreateAsync(Document document, string newName, OptionSet optionSet, CancellationToken cancellationToken)
            {
                var analysis = await RenameSymbolDocumentActionAnalysis.CreateAsync(document, newName, optionSet, cancellationToken).ConfigureAwait(false);

                if (analysis.ShouldApplyAction)
                {
                    return new RenameSymbolDocumentAction(analysis, optionSet, ImmutableArray<ErrorResource>.Empty);
                }

                return null;
            }

            private readonly struct RenameSymbolDocumentActionAnalysis
            {
                public string OriginalDocumentName { get; }
                public string NewDocumentName { get; }
                public string NewSymbolName { get; }
                public string? OriginalSymbolName { get; }
                public bool ShouldApplyAction => OriginalSymbolName != null && NewSymbolName != OriginalSymbolName;
                private RenameSymbolDocumentActionAnalysis(
                    Document document,
                    string newName,
                    ISymbol? symbol = null)
                {
                    OriginalDocumentName = document.Name;
                    NewDocumentName = newName;
                    NewSymbolName = Path.GetFileNameWithoutExtension(newName);
                    OriginalSymbolName = symbol?.Name;
                }

                public static async Task<RenameSymbolDocumentActionAnalysis> CreateAsync(Document document, string newName, OptionSet optionSet, CancellationToken cancellationToken)
                {
                    // TODO: Detect naming conflicts ahead of time
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
