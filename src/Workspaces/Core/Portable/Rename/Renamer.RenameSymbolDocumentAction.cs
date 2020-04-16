// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using System.Linq;
using System;
using System.IO;

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
                ImmutableArray<ErrorResource> errors)
                : base(errors)
            {
                _analysis = analysis;
            }

            public override string GetDescription(CultureInfo? culture)
                => string.Format(WorkspacesResources.ResourceManager.GetString("Rename_0_to_1", culture ?? WorkspacesResources.Culture)!, _analysis.OriginalDocumentName, _analysis.NewDocumentName);

            internal override async Task<Solution> GetModifiedSolutionAsync(Document document, OptionSet optionSet, CancellationToken cancellationToken)
            {
                var solution = document.Project.Solution;
                var matchingTypeDeclaration = await GetMatchingTypeDeclarationAsync(document, _analysis.OriginalSymbolName!, cancellationToken).ConfigureAwait(false);

                if (matchingTypeDeclaration is object)
                {
                    var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    var symbol = semanticModel.GetDeclaredSymbol(matchingTypeDeclaration, cancellationToken);

                    solution = await RenameSymbolAsync(solution, symbol, _analysis.NewSymbolName, optionSet, cancellationToken).ConfigureAwait(false);
                }

                return solution;
            }

            private static async Task<SyntaxNode?> GetMatchingTypeDeclarationAsync(Document document, string name, CancellationToken cancellationToken)
            {
                var syntaxRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

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
                }

                return null;
            }

            private readonly struct AnalysisResult
            {
                public string OriginalDocumentName { get; }
                public string NewDocumentName { get; }
                public string NewSymbolName { get; }
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
                }
            }
        }

    }
}
