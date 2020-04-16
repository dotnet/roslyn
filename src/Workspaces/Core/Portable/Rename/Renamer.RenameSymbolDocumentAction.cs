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
        internal sealed class RenameSymbolDocumentAction : RenameDocumentAction
        {
            private readonly AnalysisResult _analysis;

            private RenameSymbolDocumentAction(
                AnalysisResult analysis,
<<<<<<< HEAD
                ImmutableArray<ErrorResource> errors)
                : base(errors)
=======
                OptionSet optionSet,
                ImmutableArray<ErrorResource> errors)
                : base(errors, optionSet)
>>>>>>> d7785e81292987663a30efef90f6d988cd9bce2c
            {
                _analysis = analysis;
            }

            public override string GetDescription(CultureInfo? culture)
                => string.Format(WorkspacesResources.ResourceManager.GetString("Rename_0_to_1", culture ?? WorkspacesResources.Culture)!, _analysis.OriginalDocumentName, _analysis.NewDocumentName);

<<<<<<< HEAD
            internal override async Task<Solution> GetModifiedSolutionAsync(Document document, OptionSet optionSet, CancellationToken cancellationToken)
=======
            internal override async Task<Solution> GetModifiedSolutionAsync(Document document, CancellationToken cancellationToken)
>>>>>>> d7785e81292987663a30efef90f6d988cd9bce2c
            {
                var solution = document.Project.Solution;
                var matchingTypeDeclaration = await GetMatchingTypeDeclarationAsync(document, _analysis.OriginalSymbolName!, cancellationToken).ConfigureAwait(false);

                if (matchingTypeDeclaration is object)
                {
                    var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    var symbol = semanticModel.GetDeclaredSymbol(matchingTypeDeclaration, cancellationToken);

<<<<<<< HEAD
                    solution = await RenameSymbolAsync(solution, symbol, _analysis.NewSymbolName, optionSet, cancellationToken).ConfigureAwait(false);
=======
                    solution = await RenameSymbolAsync(solution, symbol, _analysis.NewSymbolName, OptionSet, cancellationToken).ConfigureAwait(false);
>>>>>>> d7785e81292987663a30efef90f6d988cd9bce2c
                }

                return solution;
            }

            private static async Task<SyntaxNode?> GetMatchingTypeDeclarationAsync(Document document, string name, CancellationToken cancellationToken)
            {
                var syntaxRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

<<<<<<< HEAD
                var typeDeclarations = syntaxRoot.DescendantNodesAndSelf(n => !syntaxFacts.IsMethodBody(n)).Where(syntaxFacts.IsTypeDeclaration);
                return typeDeclarations.FirstOrDefault(d => syntaxFacts.GetDisplayName(d, DisplayNameOptions.None).Equals(name, StringComparison.OrdinalIgnoreCase));
            }

            public static async Task<RenameSymbolDocumentAction?> TryCreateAsync(Document document, string newName, CancellationToken cancellationToken)
            {
                var analysis = await AnalyzeAsync(document, newName, cancellationToken).ConfigureAwait(false);

                if (analysis.HasValue)
                {
                    return new RenameSymbolDocumentAction(analysis.Value, ImmutableArray<ErrorResource>.Empty);
                }

                return null;
            }

            private static async Task<AnalysisResult?> AnalyzeAsync(Document document, string newName, CancellationToken cancellationToken)
            {
                // TODO: Detect naming conflicts ahead of time
                var originalSymbolName = Path.GetFileNameWithoutExtension(document.Name);
                var matchingDeclaration = await GetMatchingTypeDeclarationAsync(document, originalSymbolName, cancellationToken).ConfigureAwait(false);

                if (matchingDeclaration is object)
                {
                    var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    var symbol = semanticModel.GetDeclaredSymbol(matchingDeclaration, cancellationToken);
                    var newSymbolName = Path.GetFileNameWithoutExtension(newName);

                    if (symbol is null || symbol.Name == newSymbolName)
                    {
                        return null;
                    }

                    return new AnalysisResult(document, newName, newSymbolName, symbol.Name);
=======
                var typeDeclarations = syntaxRoot.DescendantNodesAndSelf().Where(syntaxFacts.IsTypeDeclaration);
                return typeDeclarations.FirstOrDefault(d => syntaxFacts.GetDisplayName(d, DisplayNameOptions.None).Equals(name, StringComparison.OrdinalIgnoreCase));
            }

            public static async Task<RenameSymbolDocumentAction?> TryCreateAsync(Document document, string newName, OptionSet optionSet, CancellationToken cancellationToken)
            {
                var analysis = await AnalysisResult.CreateAsync(document, newName, optionSet, cancellationToken).ConfigureAwait(false);

                if (analysis.ShouldApplyAction)
                {
                    return new RenameSymbolDocumentAction(analysis, optionSet, ImmutableArray<ErrorResource>.Empty);
>>>>>>> d7785e81292987663a30efef90f6d988cd9bce2c
                }

                return null;
            }

            private readonly struct AnalysisResult
            {
                public string OriginalDocumentName { get; }
                public string NewDocumentName { get; }
                public string NewSymbolName { get; }
<<<<<<< HEAD
                public string OriginalSymbolName { get; }

                public AnalysisResult(
                    Document document,
                    string newDocumentName,
                    string newSymbolName,
                    string originalSymbolName)
                {
                    OriginalDocumentName = document.Name;
                    NewDocumentName = newDocumentName;
                    NewSymbolName = newSymbolName;
                    OriginalSymbolName = originalSymbolName;
=======
                public string? OriginalSymbolName { get; }
                public bool ShouldApplyAction => OriginalSymbolName != null && NewSymbolName != OriginalSymbolName;

                private AnalysisResult(
                    Document document,
                    string newName,
                    ISymbol? symbol = null)
                {
                    OriginalDocumentName = document.Name;
                    NewDocumentName = newName;
                    NewSymbolName = Path.GetFileNameWithoutExtension(newName);
                    OriginalSymbolName = symbol?.Name;
                }

                public static async Task<AnalysisResult> CreateAsync(Document document, string newName, OptionSet optionSet, CancellationToken cancellationToken)
                {
                    // TODO: Detect naming conflicts ahead of time
                    var originalSymbolName = Path.GetFileNameWithoutExtension(document.Name);
                    var matchingDeclaration = await GetMatchingTypeDeclarationAsync(document, originalSymbolName, cancellationToken).ConfigureAwait(false);

                    if (matchingDeclaration is object)
                    {
                        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                        var symbol = semanticModel.GetDeclaredSymbol(matchingDeclaration, cancellationToken);
                        return new AnalysisResult(document, newName, symbol);
                    }

                    return new AnalysisResult(document, newName);
>>>>>>> d7785e81292987663a30efef90f6d988cd9bce2c
                }
            }
        }

    }
}
