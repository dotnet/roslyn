// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
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

        internal static ImmutableArray<ISymbol> GetSymbolsTouchingPosition(
            int position, SemanticModel semanticModel, HostWorkspaceServices services, CancellationToken cancellationToken)
        {
            var bindableToken = semanticModel.SyntaxTree.GetRoot(cancellationToken).FindToken(position, findInsideTrivia: true);
            var semanticInfo = semanticModel.GetSemanticInfo(bindableToken, services, cancellationToken);
            var symbols = semanticInfo.DeclaredSymbol != null
                ? ImmutableArray.Create<ISymbol>(semanticInfo.DeclaredSymbol)
                : semanticInfo.GetSymbols(includeType: false);

            // if there are more than one symbol, then remove the alias symbols.
            // When using (not declaring) an alias, the alias symbol and the target symbol are returned
            // by GetSymbols
            if (symbols.Length > 1)
            {
                symbols = symbols.WhereAsArray(s => s.Kind != SymbolKind.Alias);
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
                symbol.Kind is SymbolKind.Local or
                SymbolKind.Label or
                SymbolKind.RangeVariable or
                SymbolKind.Parameter;
        }

        internal static IEnumerable<Document> GetDocumentsAffectedByRename(ISymbol symbol, Solution solution, IEnumerable<RenameLocation> renameLocations)
        {
            if (IsSymbolDefinedInsideMethod(symbol))
            {
                // if the symbol was declared inside of a method, don't check for conflicts in non-renamed documents.
                return renameLocations.Select(l => solution.GetRequiredDocument(l.DocumentId));
            }
            else
            {
                var documentsOfRenameSymbolDeclaration = symbol.Locations.Where(l => l.IsInSource).Select(l => solution.GetRequiredDocument(l.SourceTree!));
                var projectIdsOfRenameSymbolDeclaration =
                    documentsOfRenameSymbolDeclaration.SelectMany(d => d.GetLinkedDocumentIds())
                    .Concat(documentsOfRenameSymbolDeclaration.First().Id)
                    .Select(d => d.ProjectId).Distinct();

                // perf optimization: only look in declaring project when possible
                if (ShouldRenameOnlyAffectDeclaringProject(symbol))
                {
                    var isSubset = renameLocations.Select(l => l.DocumentId.ProjectId).Distinct().Except(projectIdsOfRenameSymbolDeclaration).IsEmpty();
                    Contract.ThrowIfFalse(isSubset);
                    return projectIdsOfRenameSymbolDeclaration.SelectMany(p => solution.GetRequiredProject(p).Documents);
                }
                else
                {
                    // We are trying to figure out the projects that directly depend on the project that contains the declaration for
                    // the rename symbol.  Other projects should not be affected by the rename.
                    var relevantProjects = projectIdsOfRenameSymbolDeclaration.Concat(projectIdsOfRenameSymbolDeclaration.SelectMany(p =>
                       solution.GetProjectDependencyGraph().GetProjectsThatDirectlyDependOnThisProject(p))).Distinct();
                    return relevantProjects.SelectMany(p => solution.GetRequiredProject(p).Documents);
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
            if (symbol.DeclaredAccessibility != Accessibility.Private)
            {
                // non-private members can influence other projects.
                return false;
            }

            if (symbol.ExplicitInterfaceImplementations().Any())
            {
                // Explicit interface implementations can cascade to other projects
                return false;
            }

            if (symbol.IsOverride)
            {
                // private-overrides aren't actually legal.  But if we see one, we tolerate it and search other projects in case
                // they override it.
                // https://github.com/dotnet/roslyn/issues/25682
                return false;
            }

            return true;
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
                if (symbolInfo.Symbol.IsTupleType())
                {
                    return TokenRenameInfo.NoSymbolsTokenInfo;
                }

                return TokenRenameInfo.CreateSingleSymbolTokenInfo(symbolInfo.Symbol);
            }

            if (symbolInfo.CandidateReason == CandidateReason.MemberGroup && symbolInfo.CandidateSymbols.Any())
            {
                // This is a reference from a nameof expression. Allow the rename but set the RenameOverloads option
                return TokenRenameInfo.CreateMemberGroupTokenInfo(symbolInfo.CandidateSymbols);
            }

            if (RenameLocation.ShouldRename(symbolInfo.CandidateReason) &&
                symbolInfo.CandidateSymbols.Length == 1)
            {
                // TODO(cyrusn): We're allowing rename here, but we likely should let the user
                // know that there is an error in the code and that rename results might be
                // inaccurate.
                return TokenRenameInfo.CreateSingleSymbolTokenInfo(symbolInfo.CandidateSymbols[0]);
            }

            return TokenRenameInfo.NoSymbolsTokenInfo;
        }

        internal static Dictionary<SymbolKey, RenameSymbolContext> GroupRenameContextBySymbolKey(
            ImmutableArray<RenameSymbolContext> symbolContexts)
        {
            var renameContexts = new Dictionary<SymbolKey, RenameSymbolContext>();
            foreach (var context in symbolContexts)
            {
                renameContexts[context.RenamedSymbol.GetSymbolKey()] = context;
            }

            return renameContexts;
        }

        internal static Dictionary<TextSpan, TextSpanRenameContext> GroupTextRenameContextsByTextSpan(
            ImmutableArray<TextSpanRenameContext> textSpanRenameContexts)
        {
            var textSpanToRenameContext = new Dictionary<TextSpan, TextSpanRenameContext>();
            foreach (var context in textSpanRenameContexts)
            {
                var textSpan = context.RenameLocation.Location.SourceSpan;
                if (!textSpanToRenameContext.ContainsKey(textSpan))
                {
                    textSpanToRenameContext[textSpan] = context;
                }
            }

            return textSpanToRenameContext;
        }

        internal static Dictionary<TextSpan, HashSet<TextSpanRenameContext>> GroupStringAndCommentsTextSpanRenameContexts(
            ImmutableArray<TextSpanRenameContext> renameSymbolContexts)
        {
            var textSpanToRenameContexts = new Dictionary<TextSpan, HashSet<TextSpanRenameContext>>();
            foreach (var context in renameSymbolContexts)
            {
                var containingSpan = context.RenameLocation.ContainingLocationForStringOrComment;
                if (textSpanToRenameContexts.TryGetValue(containingSpan, out var existingContexts))
                {
                    existingContexts.Add(context);
                }
                else
                {
                    textSpanToRenameContexts[containingSpan] = new HashSet<TextSpanRenameContext>() { context };
                }
            }

            return textSpanToRenameContexts;
        }

        internal static ImmutableHashSet<RenameSymbolContext> GetMatchedContexts(
            IEnumerable<RenameSymbolContext> renameContexts, Func<RenameSymbolContext, bool> predicate)
        {
            using var _ = PooledHashSet<RenameSymbolContext>.GetInstance(out var builder);

            foreach (var renameSymbolContext in renameContexts)
            {
                if (predicate(renameSymbolContext))
                    builder.Add(renameSymbolContext);
            }

            return builder.ToImmutableHashSet();
        }

        internal static ImmutableSortedDictionary<TextSpan, (string replacementText, string matchText)> CreateSubSpanToReplacementTextDictionary(
            HashSet<TextSpanRenameContext> textSpanRenameContexts)
        {
            var subSpanToReplacementTextBuilder = ImmutableSortedDictionary.CreateBuilder<TextSpan, (string replacementText, string matchText)>();
            foreach (var context in textSpanRenameContexts.OrderByDescending(c => c.Priority))
            {
                var renameLocation = context.RenameLocation;
                var location = renameLocation.Location;
                if (location.IsInSource && renameLocation.IsRenameInStringOrComment)
                {
                    var sourceSpan = location.SourceSpan;

                    // SourceSpan should be a part of the containing location.
                    RoslynDebug.Assert(sourceSpan.Start >= renameLocation.ContainingLocationForStringOrComment.Start);
                    RoslynDebug.Assert(sourceSpan.End <= renameLocation.ContainingLocationForStringOrComment.End);

                    // Calculate the relative postion within the containg location.
                    var subSpan = new TextSpan(
                        sourceSpan.Start - renameLocation.ContainingLocationForStringOrComment.Start,
                        sourceSpan.Length);
                    // If two symbols tries to rename a same sub span,
                    // e.g.
                    //      // Comment Hello
                    // class Hello
                    // {
                    //    
                    // }
                    // class World
                    // {
                    //    void Hello() { }
                    // }
                    // If try to rename both 'class Hello' to 'Bar' and 'void Hello()' to 'Goo'.
                    // For '// Comment Hello', igore the one with lower priority
                    if (!subSpanToReplacementTextBuilder.ContainsKey(subSpan))
                    {
                        subSpanToReplacementTextBuilder[subSpan] = (context.SymbolContext.ReplacementText, context.SymbolContext.OriginalText);
                    }
                }
            }

            return subSpanToReplacementTextBuilder.ToImmutable();
        }
    }
}
