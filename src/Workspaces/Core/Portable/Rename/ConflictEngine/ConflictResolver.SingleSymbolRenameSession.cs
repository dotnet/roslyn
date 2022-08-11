// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    internal static partial class ConflictResolver
    {
        /// <summary>
        /// Helper class to track the state necessary for finding/resolving conflicts in a 
        /// rename session.
        /// </summary>
        private class SingleSymbolRenameSession : Session
        {
            // Set of All Locations that will be renamed (does not include non-reference locations that need to be checked for conflicts)
            private readonly SymbolicRenameLocations _renameLocationSet;

            // Rename Symbol's Source Location
            private readonly Location _renameSymbolDeclarationLocation;
            private readonly DocumentId _documentIdOfRenameSymbolDeclaration;
            private readonly string _originalText;
            private readonly string _replacementText;
            private readonly ImmutableHashSet<DocumentId> _documentsIdsToBeCheckedForConflict;

            // Contains Strings like Bar -> BarAttribute ; Property Bar -> Bar , get_Bar, set_Bar
            private readonly ImmutableArray<string> _possibleNameConflicts;
            private readonly bool _replacementTextValid;

            public static async Task<SingleSymbolRenameSession> CreateAsync(
                SymbolicRenameLocations renameLocationSet,
                Location renameSymbolDeclarationLocation,
                string replacementText,
                ImmutableArray<SymbolKey> nonConflictSymbolKeys,
                CodeCleanupOptionsProvider fallbackOptions,
                CancellationToken cancellationToken)
            {
                var originalText = renameLocationSet.Symbol.Name;
                var (documentsIdsToBeCheckedForConflict, possibleNameConflicts) = await FindDocumentsAndPossibleNameConflictsAsync(
                    renameLocationSet,
                    replacementText,
                    originalText,
                    cancellationToken).ConfigureAwait(false);

                var replacementTextValid = IsIdentifierValid_Worker(
                    renameLocationSet.Solution,
                    replacementText,
                    documentsIdsToBeCheckedForConflict.Select(documentId => documentId.ProjectId).Distinct());

                return new SingleSymbolRenameSession(
                    renameLocationSet,
                    renameSymbolDeclarationLocation,
                    originalText,
                    replacementText,
                    nonConflictSymbolKeys,
                    possibleNameConflicts,
                    documentsIdsToBeCheckedForConflict,
                    replacementTextValid,
                    fallbackOptions,
                    cancellationToken);
            }

            private SingleSymbolRenameSession(
                SymbolicRenameLocations renameLocationSet,
                Location renameSymbolDeclarationLocation,
                string originalText,
                string replacementText,
                ImmutableArray<SymbolKey> nonConflictSymbolKeys,
                ImmutableArray<string> possibleNameConflicts,
                ImmutableHashSet<DocumentId> documentsIdsToBeCheckedForConflict,
                bool replacementTextValid,
                CodeCleanupOptionsProvider fallbackOptions,
                CancellationToken cancellationToken) : base(
                    renameLocationSet.Solution, nonConflictSymbolKeys, fallbackOptions, cancellationToken)

            {
                _renameLocationSet = renameLocationSet;
                _renameSymbolDeclarationLocation = renameSymbolDeclarationLocation;
                // only process documents which possibly contain the identifiers.
                _documentIdOfRenameSymbolDeclaration = renameLocationSet.Solution.GetRequiredDocument(renameSymbolDeclarationLocation.SourceTree!).Id;

                _originalText = originalText;
                _replacementText = replacementText;
                _possibleNameConflicts = possibleNameConflicts;

                _documentsIdsToBeCheckedForConflict = documentsIdsToBeCheckedForConflict;
                _replacementTextValid = replacementTextValid;
            }

            private async Task<ISymbol> GetRenamedSymbolInCurrentSolutionAsync(
                MutableConflictResolution conflictResolution)
            {
                try
                {
                    // get the renamed symbol in complexified new solution
                    var start = conflictResolution.HasDocumentChanged(_documentIdOfRenameSymbolDeclaration)
                        ? conflictResolution.GetAdjustedTokenStartingPosition(_renameSymbolDeclarationLocation.SourceSpan.Start, _documentIdOfRenameSymbolDeclaration)
                        : _renameSymbolDeclarationLocation.SourceSpan.Start;

                    var document = conflictResolution.CurrentSolution.GetRequiredDocument(_documentIdOfRenameSymbolDeclaration);
                    var newSymbol = await SymbolFinder.FindSymbolAtPositionAsync(document, start, cancellationToken: CancellationToken).ConfigureAwait(false);
                    return newSymbol;
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, ErrorSeverity.Critical))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            protected override async Task<ImmutableHashSet<RenamedSymbolInfo>> GetValidRenamedSymbolsInfoInCurrentSolutionAsync(MutableConflictResolution conflictResolution)
            {
                if (!_replacementTextValid)
                {
                    return ImmutableHashSet<RenamedSymbolInfo>.Empty;
                }

                // if we rename an identifier and it now binds to a symbol from metadata this should be treated as
                // an invalid rename.
                var renamedSymbolInNewSolution = await GetRenamedSymbolInCurrentSolutionAsync(conflictResolution).ConfigureAwait(false);
                if (renamedSymbolInNewSolution == null || renamedSymbolInNewSolution.Locations.All(location => !location.IsInSource))
                {
                    return ImmutableHashSet<RenamedSymbolInfo>.Empty;
                }

                return ImmutableHashSet.Create(new RenamedSymbolInfo(
                    renamedSymbolInNewSolution, _renameLocationSet, _documentIdOfRenameSymbolDeclaration, _renameSymbolDeclarationLocation));
            }

            protected override async Task<ImmutableArray<RenamedSymbolInfo>> GetDeclarationChangedSymbolsInfoAsync(
                MutableConflictResolution conflictResolution,
                ProjectId projectId)
            {
                if (_documentIdOfRenameSymbolDeclaration.ProjectId == projectId)
                {
                    var renamedSymbolInNewSolution = await GetRenamedSymbolInCurrentSolutionAsync(conflictResolution).ConfigureAwait(false);
                    return ImmutableArray.Create(new RenamedSymbolInfo(
                    renamedSymbolInNewSolution, _renameLocationSet, _documentIdOfRenameSymbolDeclaration, _renameSymbolDeclarationLocation));
                }

                return ImmutableArray<RenamedSymbolInfo>.Empty;
            }

            protected override bool HasConflictForMetadataReference(
                RenameDeclarationLocationReference renameDeclarationLocationReference, ISymbol newReferencedSymbol)
            {
                var newMetadataName = newReferencedSymbol.ToDisplayString(s_metadataSymbolDisplayFormat);
                var oldMetadataName = renameDeclarationLocationReference.Name;
                return !HeuristicMetadataNameEquivalenceCheck(oldMetadataName, newMetadataName, _originalText, _replacementText);
            }

            protected override bool ShouldSimplifyTheProject(ProjectId projectId)
                => _replacementTextValid;
        }
    }
}
