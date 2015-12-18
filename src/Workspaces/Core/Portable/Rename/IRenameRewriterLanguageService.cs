// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename
{
    internal interface IRenameRewriterLanguageService : ILanguageService
    {
        /// <summary>
        /// This method annotates the given syntax tree with all the locations that need to be checked for conflict
        /// after the rename operation.  It also renames all the reference locations and expands any conflict locations.
        /// </summary>
        /// <param name="parameters">The options describing this rename operation</param>
        /// <returns>The root of the annotated tree.</returns>
        SyntaxNode AnnotateAndRename(RenameRewriterParameters parameters);

        /// <summary>
        /// Based on the kind of the symbol and the new name, this function determines possible conflicting names that
        /// should be tracked for semantic changes during rename.
        /// </summary>
        /// <param name="symbol">The symbol that gets renamed.</param>
        /// <param name="newName">The new name for the symbol.</param>
        /// <param name="possibleNameConflicts">List where possible conflicting names will be added to.</param>
        void TryAddPossibleNameConflicts(
            ISymbol symbol,
            string newName,
            ICollection<string> possibleNameConflicts);

        /// <summary>
        /// Identifies the conflicts caused by the new declaration created during rename.
        /// </summary>
        /// <param name="replacementText">The replacementText as given from the user.</param>
        /// <param name="renamedSymbol">The new symbol (after rename).</param>
        /// <param name="renameSymbol">The original symbol that got renamed.</param>
        /// <param name="referencedSymbols">All referenced symbols that are part of this rename session.</param>
        /// <param name="baseSolution">The original solution when rename started.</param>
        /// <param name="newSolution">The resulting solution after rename.</param>
        /// <param name="reverseMappedLocations">A mapping from new to old locations.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>All locations where conflicts were caused because the new declaration.</returns>
        Task<IEnumerable<Location>> ComputeDeclarationConflictsAsync(
            string replacementText,
            ISymbol renamedSymbol,
            ISymbol renameSymbol,
            IEnumerable<ISymbol> referencedSymbols,
            Solution baseSolution,
            Solution newSolution,
            IDictionary<Location, Location> reverseMappedLocations,
            CancellationToken cancellationToken);

        /// <summary>
        /// Identifies the conflicts caused by implicitly referencing the renamed symbol.
        /// </summary>
        /// <param name="renameSymbol">The original symbol that got renamed.</param>
        /// <param name="renamedSymbol">The new symbol (after rename).</param>
        /// <param name="implicitReferenceLocations">All implicit reference locations.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of implicit conflicts.</returns>
        Task<IEnumerable<Location>> ComputeImplicitReferenceConflictsAsync(
            ISymbol renameSymbol,
            ISymbol renamedSymbol,
            IEnumerable<ReferenceLocation> implicitReferenceLocations,
            CancellationToken cancellationToken);

        /// <summary>
        /// Identifies the conflicts caused by implicitly referencing the renamed symbol.
        /// </summary>
        /// <param name="renamedSymbol">The new symbol (after rename).</param>
        /// <param name="semanticModel">The SemanticModel of the document in the new solution containing the renamedSymbol</param>
        /// <param name="originalDeclarationLocation">The location of the renamedSymbol in the old solution</param>
        /// <param name="newDeclarationLocationStartingPosition">The starting position of the renamedSymbol in the new solution</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of implicit conflicts.</returns>
        IEnumerable<Location> ComputePossibleImplicitUsageConflicts(
            ISymbol renamedSymbol,
            SemanticModel semanticModel,
            Location originalDeclarationLocation,
            int newDeclarationLocationStartingPosition,
            CancellationToken cancellationToken);

        /// <summary>
        /// Identifies potential Conflicts into the inner scope locals. This may give false positives.
        /// </summary>
        /// <param name="token">The Token that may introduce errors else where</param>
        /// <param name="newReferencedSymbols">The symbols that this token binds to after the rename
        /// has been applied</param>
        /// <returns>Returns if there is a potential conflict</returns>
        bool LocalVariableConflict(
            SyntaxToken token,
            IEnumerable<ISymbol> newReferencedSymbols);

        /// <summary>
        /// Used to find if the replacement Identifier is valid
        /// </summary>
        /// <param name="replacementText"></param>
        /// <param name="syntaxFactsService"></param>
        /// <returns></returns>
        bool IsIdentifierValid(
            string replacementText,
            ISyntaxFactsService syntaxFactsService);

        /// <summary>
        /// Gets the top most enclosing statement as target to call MakeExplicit on.
        /// It's either the enclosing statement, or if this statement is inside of a lambda expression, the enclosing
        /// statement of this lambda.
        /// </summary>
        /// <param name="token">The token to get the complexification target for.</param>
        /// <returns></returns>
        SyntaxNode GetExpansionTargetForLocation(SyntaxToken token);
    }
}
