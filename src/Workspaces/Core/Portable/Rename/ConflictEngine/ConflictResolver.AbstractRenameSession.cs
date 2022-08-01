// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    internal partial class ConflictResolver
    {
        private abstract class AbstractRenameSession
        {
            protected readonly ImmutableArray<ProjectId> _topologicallySortedProjects;
            protected readonly CancellationToken _cancellationToken;
            protected readonly AnnotationTable<RenameAnnotation> _renameAnnotations = new AnnotationTable<RenameAnnotation>(RenameAnnotation.Kind);
            protected ISet<ConflictLocationInfo> _conflictLocations = SpecializedCollections.EmptySet<ConflictLocationInfo>();

            protected AbstractRenameSession(
                Solution solution,
                CancellationToken cancellationToken)
            {
                var dependencyGraph = solution.GetProjectDependencyGraph();
                _topologicallySortedProjects = dependencyGraph.GetTopologicallySortedProjects(cancellationToken).ToImmutableArray();
                _cancellationToken = cancellationToken;
            }

            public async Task<MutableConflictResolution> ResolveConflictsCoreAsync(
                Solution baseSolution,
                ImmutableHashSet<DocumentId> documentsIdsToBeCheckedForConflict)
            {
                try
                {
                    // Process rename one project at a time to improve caching and reduce syntax tree serialization.
                    var documentsGroupedByTopologicallySortedProjectId = documentsIdsToBeCheckedForConflict
                        .GroupBy(d => d.ProjectId)
                        .OrderBy(g => _topologicallySortedProjects.IndexOf(g.Key));

                    var renamedSpansTracker = new RenamedSpansTracker();
                    var conflictResolution = new MutableConflictResolution(
                        baseSolution,
                        renamedSpansTracker,
                        _replacementText,
                        ImmutableDictionary<ISymbol, bool>.Empty.Add(_renameLocationSet.Symbol, _replacementTextValid));

                    var intermediateSolution = conflictResolution.OldSolution;
                    foreach (var documentsByProject in documentsGroupedByTopologicallySortedProjectId)
                    {
                        var documentIdsThatGetsAnnotatedAndRenamed = new HashSet<DocumentId>(documentsByProject);
                        using (baseSolution.Services.CacheService?.EnableCaching(documentsByProject.Key))
                        {
                            // Rename is going to be in 5 phases.
                            // 1st phase - Does a simple token replacement
                            // If the 1st phase results in conflict then we perform then:
                            //      2nd phase is to expand and simplify only the reference locations with conflicts
                            //      3rd phase is to expand and simplify all the conflict locations (both reference and non-reference)
                            // If there are unresolved Conflicts after the 3rd phase then in 4th phase, 
                            //      We complexify and resolve locations that were resolvable and for the other locations we perform the normal token replacement like the first the phase.
                            // If the OptionSet has RenameFile to true, we rename files with the type declaration
                            for (var phase = 0; phase < 4; phase++)
                            {
                                // Step 1:
                                // The rename process and annotation for the bookkeeping is performed in one-step
                                // The Process in short is,
                                // 1. If renaming a token which is no conflict then replace the token and make a map of the oldspan to the newspan
                                // 2. If we encounter a node that has to be expanded( because there was a conflict in previous phase), we expand it.
                                //    If the node happens to contain a token that needs to be renamed then we annotate it and rename it after expansion else just expand and proceed
                                // 3. Through the whole process we maintain a map of the oldspan to newspan. In case of expansion & rename, we map the expanded node and the renamed token
                                var (partiallyRenamedSolution, unchangedDocuments) = await AnnotateAndRename_WorkerAsync(
                                    baseSolution,
                                    conflictResolution.CurrentSolution,
                                    documentIdsThatGetsAnnotatedAndRenamed,
                                    renamedSpansTracker).ConfigureAwait(false);

                                // If the documents do not change then remove it from the conflict checking list
                                documentIdsThatGetsAnnotatedAndRenamed.RemoveRange(unchangedDocuments);
                                conflictResolution.UpdateCurrentSolution(partiallyRenamedSolution);

                                //conflictResolution.UpdateCurrentSolution(await AnnotateAndRename_WorkerAsync(
                                //    baseSolution,
                                //    conflictResolution.CurrentSolution,
                                //    documentIdsThatGetsAnnotatedAndRenamed,
                                //    _renameLocationSet.Locations,
                                //    renamedSpansTracker,
                                //    _replacementTextValid).ConfigureAwait(false));

                                // Step 2: Check for conflicts in the renamed solution
                                var foundResolvableConflicts = await IdentifyConflictsAsync(
                                    documentIdsForConflictResolution: documentIdsThatGetsAnnotatedAndRenamed,
                                    allDocumentIdsInProject: documentsByProject,
                                    projectId: documentsByProject.Key,
                                    conflictResolution: conflictResolution).ConfigureAwait(false);

                                if (!foundResolvableConflicts || phase == 3)
                                {
                                    break;
                                }

                                if (phase == 0)
                                {
                                    _conflictLocations = conflictResolution.RelatedLocations
                                        .Where(loc => (documentIdsThatGetsAnnotatedAndRenamed.Contains(loc.DocumentId) && loc.Type == RelatedLocationType.PossiblyResolvableConflict && loc.IsReference))
                                        .Select(loc => new ConflictLocationInfo(loc))
                                        .ToSet();

                                    // If there were no conflicting locations in references, then the first conflict phase has to be skipped.
                                    if (_conflictLocations.Count == 0)
                                    {
                                        phase++;
                                    }
                                }

                                if (phase == 1)
                                {
                                    _conflictLocations = _conflictLocations.Concat(conflictResolution.RelatedLocations
                                        .Where(loc => documentIdsThatGetsAnnotatedAndRenamed.Contains(loc.DocumentId) && loc.Type == RelatedLocationType.PossiblyResolvableConflict)
                                        .Select(loc => new ConflictLocationInfo(loc)))
                                        .ToSet();
                                }

                                // Set the documents with conflicts that need to be processed in the next phase.
                                // Note that we need to get the conflictLocations here since we're going to remove some locations below if phase == 2
                                documentIdsThatGetsAnnotatedAndRenamed = new HashSet<DocumentId>(_conflictLocations.Select(l => l.DocumentId));

                                if (phase == 2)
                                {
                                    // After phase 2, if there are still conflicts then remove the conflict locations from being expanded
                                    var unresolvedLocations = conflictResolution.RelatedLocations
                                        .Where(l => (l.Type & RelatedLocationType.UnresolvedConflict) != 0)
                                        .Select(l => (complexifiedSpan: l.ComplexifiedTargetSpan, documentId: l.DocumentId)).Distinct();

                                    _conflictLocations = _conflictLocations.Where(l => !unresolvedLocations.Any(c => c.documentId == l.DocumentId && c.complexifiedSpan.Contains(l.OriginalIdentifierSpan))).ToSet();
                                }

                                // Clean up side effects from rename before entering the next phase
                                conflictResolution.ClearDocuments(documentIdsThatGetsAnnotatedAndRenamed);
                            }

                            // Step 3: Simplify the project
                            conflictResolution.UpdateCurrentSolution(await renamedSpansTracker.SimplifyAsync(conflictResolution.CurrentSolution, documentsByProject, _replacementTextValid, _renameAnnotations, FallbackOptions, _cancellationToken).ConfigureAwait(false));
                            intermediateSolution = await conflictResolution.RemoveAllRenameAnnotationsAsync(
                                intermediateSolution, documentsByProject, _renameAnnotations, _cancellationToken).ConfigureAwait(false);
                            conflictResolution.UpdateCurrentSolution(intermediateSolution);
                        }
                    }

                    // This rename could break implicit references of this symbol (e.g. rename MoveNext on a collection like type in a 
                    // foreach/for each statement
                    var renamedSymbolInNewSolution = await GetRenamedSymbolInCurrentSolutionAsync(conflictResolution).ConfigureAwait(false);

                    if (IsRenameValid(conflictResolution, renamedSymbolInNewSolution, _renameLocationSet.Symbol))
                    {
                        await AddImplicitConflictsAsync(
                            renamedSymbolInNewSolution,
                            _renameLocationSet.Symbol,
                            _renameLocationSet.ImplicitLocations,
                            await conflictResolution.CurrentSolution.GetRequiredDocument(_documentIdOfRenameSymbolDeclaration).GetRequiredSemanticModelAsync(_cancellationToken).ConfigureAwait(false),
                            _renameSymbolDeclarationLocation,
                            renamedSpansTracker.GetAdjustedPosition(_renameSymbolDeclarationLocation.SourceSpan.Start, _documentIdOfRenameSymbolDeclaration),
                            conflictResolution,
                            _cancellationToken).ConfigureAwait(false);
                    }

                    for (var i = 0; i < conflictResolution.RelatedLocations.Count; i++)
                    {
                        var relatedLocation = conflictResolution.RelatedLocations[i];
                        if (relatedLocation.Type == RelatedLocationType.PossiblyResolvableConflict)
                            conflictResolution.RelatedLocations[i] = relatedLocation.WithType(RelatedLocationType.UnresolvedConflict);
                    }

#if DEBUG
                    await DebugVerifyNoErrorsAsync(conflictResolution, _documentsIdsToBeCheckedForConflict).ConfigureAwait(false);
#endif

                    // Step 5: Rename declaration files
                    if (RenameOptions.RenameFile)
                    {
                        var definitionLocations = _renameLocationSet.Symbol.Locations;
                        var definitionDocuments = definitionLocations
                            .Select(l => conflictResolution.OldSolution.GetRequiredDocument(l.SourceTree))
                            .Distinct();

                        if (definitionDocuments.Count() == 1 && _replacementTextValid)
                        {
                            // At the moment, only single document renaming is allowed
                            conflictResolution.RenameDocumentToMatchNewSymbol(definitionDocuments.Single());
                        }
                    }

                    return conflictResolution;
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, ErrorSeverity.Critical))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            protected abstract Task<(Solution partiallyRenamedSolution, ImmutableHashSet<DocumentId> unchangedDocuments)> AnnotateAndRename_WorkerAsync(
                Solution originalSolution,
                Solution partiallyRenamedSolution,
                HashSet<DocumentId> documentIdsThatGetsAnnotatedAndRenamed,
                RenamedSpansTracker renamedSpansTracker);

            protected abstract Task<bool> IdentifyConflictsAsync(
                HashSet<DocumentId> documentIdsForConflictResolution,
                IEnumerable<DocumentId> allDocumentIdsInProject,
                ProjectId projectId,
                MutableConflictResolution conflictResolution);

            /// <summary>
            /// Gets the list of the nodes that were annotated for a conflict check 
            /// </summary>
            private IEnumerable<(SyntaxNodeOrToken syntax, RenameActionAnnotation annotation)> GetNodesOrTokensToCheckForConflicts(
                SyntaxNode syntaxRoot)
            {
                return syntaxRoot.DescendantNodesAndTokens(descendIntoTrivia: true)
                    .Where(_renameAnnotations.HasAnnotations<RenameActionAnnotation>)
                    .Select(s => (s, _renameAnnotations.GetAnnotations<RenameActionAnnotation>(s).Single()));
            }

            #region Helpers
            /// <summary>
            /// The method determines the set of documents that need to be processed for Rename and also determines
            ///  the possible set of names that need to be checked for conflicts.
            /// </summary>
            protected static async Task<(ImmutableHashSet<DocumentId> documentsIdsToBeCheckedForConflict, ImmutableArray<string> possibleNameConflicts)> FindDocumentsAndPossibleNameConflictsAsync(
                SymbolicRenameLocations renameLocations,
                string replacementText,
                string originalText,
                CancellationToken cancellationToken)
            {
                try
                {
                    var symbol = renameLocations.Symbol;
                    var solution = renameLocations.Solution;

                    var allRenamedDocuments = renameLocations.Locations.Select(loc => loc.Location.SourceTree!).Distinct().Select(solution.GetRequiredDocument);
                    using var _ = PooledHashSet<DocumentId>.GetInstance(out var documentsIdsToBeCheckedForConflictBuilder);
                    documentsIdsToBeCheckedForConflictBuilder.AddRange(allRenamedDocuments.Select(d => d.Id));
                    var documentsFromAffectedProjects = RenameUtilities.GetDocumentsAffectedByRename(
                        symbol,
                        solution,
                        renameLocations.Locations);

                    var possibleNameConflicts = new List<string>();
                    foreach (var language in documentsFromAffectedProjects.Select(d => d.Project.Language).Distinct())
                    {
                        solution.Workspace.Services.GetLanguageServices(language).GetService<IRenameRewriterLanguageService>()
                            ?.TryAddPossibleNameConflicts(symbol, replacementText, possibleNameConflicts);
                    }

                    await AddDocumentsWithPotentialConflictsAsync(
                        documentsFromAffectedProjects,
                        replacementText,
                        originalText,
                        documentsIdsToBeCheckedForConflictBuilder,
                        possibleNameConflicts,
                        cancellationToken).ConfigureAwait(false);

                    return (documentsIdsToBeCheckedForConflictBuilder.ToImmutableHashSet(), possibleNameConflicts.ToImmutableArray());
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, ErrorSeverity.Critical))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            private static async Task AddDocumentsWithPotentialConflictsAsync(
                IEnumerable<Document> documents,
                string replacementText,
                string originalText,
                PooledHashSet<DocumentId> documentsIdsToBeCheckedForConflictBuilder,
                List<string> possibleNameConflicts,
                CancellationToken cancellationToken)
            {
                try
                {
                    foreach (var document in documents)
                    {
                        if (documentsIdsToBeCheckedForConflictBuilder.Contains(document.Id))
                            continue;

                        if (!document.SupportsSyntaxTree)
                            continue;

                        var info = await SyntaxTreeIndex.GetRequiredIndexAsync(document, cancellationToken).ConfigureAwait(false);
                        if (info.ProbablyContainsEscapedIdentifier(originalText))
                        {
                            documentsIdsToBeCheckedForConflictBuilder.Add(document.Id);
                            continue;
                        }

                        if (info.ProbablyContainsIdentifier(replacementText))
                        {
                            documentsIdsToBeCheckedForConflictBuilder.Add(document.Id);
                            continue;
                        }

                        foreach (var replacementName in possibleNameConflicts)
                        {
                            if (info.ProbablyContainsIdentifier(replacementName))
                            {
                                documentsIdsToBeCheckedForConflictBuilder.Add(document.Id);
                                break;
                            }
                        }
                    }
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, ErrorSeverity.Critical))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            protected static bool IsIdentifierValid_Worker(Solution solution, string replacementText, IEnumerable<ProjectId> projectIds)
            {
                foreach (var language in projectIds.Select(p => solution.GetRequiredProject(p).Language).Distinct())
                {
                    var languageServices = solution.Workspace.Services.GetLanguageServices(language);
                    var renameRewriterLanguageService = languageServices.GetRequiredService<IRenameRewriterLanguageService>();
                    var syntaxFactsLanguageService = languageServices.GetRequiredService<ISyntaxFactsService>();
                    if (!renameRewriterLanguageService.IsIdentifierValid(replacementText, syntaxFactsLanguageService))
                    {
                        return false;
                    }
                }

                return true;
            }

            #endregion
        }
    }
}
