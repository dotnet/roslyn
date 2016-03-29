// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename
{
    internal static class RenameUtilities
    {
        internal static SyntaxToken UpdateAliasAnnotation(SyntaxToken token, ISymbol aliasSymbol, string replacementText)
        {
            // If the below Single() assert fails then it means the token has gone through a rename session where
            // it obtained an AliasSyntaxAnnotation and it is going through another rename session. Make sure the token
            // has only one annotation pertaining to the current session or try to extract only the current session annotation
            var originalAliasAnnotation = token.GetAnnotations(AliasAnnotation.Kind).Single();
            var originalAliasName = AliasAnnotation.GetAliasName(originalAliasAnnotation);

            if (originalAliasName == aliasSymbol.Name)
            {
                token = token.WithoutAnnotations(originalAliasAnnotation);
                var replacementAliasAnnotation = AliasAnnotation.Create(replacementText);
                token = token.WithAdditionalAnnotations(replacementAliasAnnotation);
            }

            return token;
        }

        internal static IEnumerable<ISymbol> GetSymbolsTouchingPosition(int position, SemanticModel semanticModel, Workspace workspace, CancellationToken cancellationToken)
        {
            var bindableToken = semanticModel.SyntaxTree.GetRoot(cancellationToken).FindToken(position, findInsideTrivia: true);
            var symbols = semanticModel.GetSymbols(bindableToken, workspace, bindLiteralsToUnderlyingType: false, cancellationToken: cancellationToken).ToArray();

            // if there are more than one symbol, then remove the alias symbols.
            // When using (not declaring) an alias, the alias symbol and the target symbol are returned
            // by GetSymbols
            if (symbols.Length > 1)
            {
                symbols = symbols.Where(s => s.Kind != SymbolKind.Alias).ToArray();
            }

            if (symbols.Length == 0)
            {
                var info = semanticModel.GetSymbolInfo(bindableToken, cancellationToken);
                if (info.CandidateReason == CandidateReason.MemberGroup)
                {
                    return info.CandidateSymbols;
                }
            }

            return symbols;
        }

        private static bool IsSymbolDefinedInsideMethod(ISymbol symbol)
        {
            return
                symbol.Kind == SymbolKind.Local ||
                symbol.Kind == SymbolKind.Label ||
                symbol.Kind == SymbolKind.RangeVariable ||
                symbol.Kind == SymbolKind.Parameter;
        }

        internal static IEnumerable<Document> GetDocumentsAffectedByRename(ISymbol symbol, Solution solution, IEnumerable<RenameLocation> renameLocations)
        {
            if (IsSymbolDefinedInsideMethod(symbol))
            {
                // if the symbol was declared inside of a method, don't check for conflicts in non-renamed documents.
                return renameLocations.Select(l => solution.GetDocument(l.DocumentId));
            }
            else
            {
                var documentsOfRenameSymbolDeclaration = symbol.Locations.Where(l => l.IsInSource).Select(l => solution.GetDocument(l.SourceTree));
                var projectIdsOfRenameSymbolDeclaration =
                    documentsOfRenameSymbolDeclaration.SelectMany(d => d.GetLinkedDocumentIds())
                    .Concat(documentsOfRenameSymbolDeclaration.First().Id)
                    .Select(d => d.ProjectId).Distinct();

                if (ShouldRenameOnlyAffectDeclaringProject(symbol))
                {
                    var isSubset = renameLocations.Select(l => l.DocumentId.ProjectId).Distinct().Except(projectIdsOfRenameSymbolDeclaration).IsEmpty();
                    Contract.ThrowIfFalse(isSubset);
                    return projectIdsOfRenameSymbolDeclaration.SelectMany(p => solution.GetProject(p).Documents);
                }
                else
                {
                    // We are trying to figure out the projects that directly depend on the project that contains the declaration for 
                    // the rename symbol.  Other projects should not be affected by the rename.
                    var relevantProjects = projectIdsOfRenameSymbolDeclaration.Concat(projectIdsOfRenameSymbolDeclaration.SelectMany(p =>
                       solution.GetProjectDependencyGraph().GetProjectsThatDirectlyDependOnThisProject(p))).Distinct();
                    return relevantProjects.SelectMany(p => solution.GetProject(p).Documents);
                }
            }
        }

        /// <summary>
        /// Renaming a private symbol typically confines the set of references and potential
        /// conflicts to that symbols declaring project. However, rename may cascade to
        /// non-public symbols which may then require other projects be considered.
        /// </summary>
        private static bool ShouldRenameOnlyAffectDeclaringProject(ISymbol symbol)
        {
            // Explicit interface implementations can cascade to other projects
            return symbol.DeclaredAccessibility == Accessibility.Private && !symbol.ExplicitInterfaceImplementations().Any();
        }

        internal static TokenRenameInfo GetTokenRenameInfo(
            ISemanticFactsService semanticFacts,
            SemanticModel semanticModel,
            SyntaxToken token,
            CancellationToken cancellationToken)
        {
            var symbol = semanticFacts.GetDeclaredSymbol(semanticModel, token, cancellationToken);
            if (symbol != null)
            {
                return TokenRenameInfo.CreateSingleSymbolTokenInfo(symbol);
            }

            var symbolInfo = semanticModel.GetSymbolInfo(token, cancellationToken);
            if (symbolInfo.Symbol != null)
            {
                return TokenRenameInfo.CreateSingleSymbolTokenInfo(symbolInfo.Symbol);
            }

            if (symbolInfo.CandidateReason == CandidateReason.MemberGroup && symbolInfo.CandidateSymbols.Any())
            {
                // This is a reference from a nameof expression. Allow the rename but set the RenameOverloads option
                return TokenRenameInfo.CreateMemberGroupTokenInfo(symbolInfo.CandidateSymbols);
            }

            return TokenRenameInfo.NoSymbolsTokenInfo;
        }
    }
}
