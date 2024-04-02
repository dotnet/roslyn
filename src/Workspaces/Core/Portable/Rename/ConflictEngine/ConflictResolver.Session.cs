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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine;

internal static partial class ConflictResolver
{
    /// <summary>
    /// Helper class to track the state necessary for finding/resolving conflicts in a 
    /// rename session.
    /// </summary>
    private class Session
    {
        // Set of All Locations that will be renamed (does not include non-reference locations that need to be checked for conflicts)
        private readonly SymbolicRenameLocations _renameLocationSet;

        // Rename Symbol's Source Location
        private readonly Location _renameSymbolDeclarationLocation;
        private readonly DocumentId _documentIdOfRenameSymbolDeclaration;
        private readonly string _originalText;
        private readonly string _replacementText;
        private readonly ImmutableArray<SymbolKey> _nonConflictSymbolKeys;
        private readonly CancellationToken _cancellationToken;

        private readonly RenameAnnotation _renamedSymbolDeclarationAnnotation = new();

        private readonly AnnotationTable<RenameAnnotation> _renameAnnotations;

        private bool _replacementTextValid;
        private bool _documentOfRenameSymbolHasBeenRenamed;

        public Session(
            SymbolicRenameLocations renameLocationSet,
            CodeCleanupOptionsProvider fallbackOptions,
            Location renameSymbolDeclarationLocation,
            string replacementText,
            ImmutableArray<SymbolKey> nonConflictSymbolKeys,
            CancellationToken cancellationToken)
        {
            _renameLocationSet = renameLocationSet;
            this.FallbackOptions = fallbackOptions;
            _renameSymbolDeclarationLocation = renameSymbolDeclarationLocation;
            _originalText = renameLocationSet.Symbol.Name;
            _replacementText = replacementText;
            _nonConflictSymbolKeys = nonConflictSymbolKeys;
            _cancellationToken = cancellationToken;

            _replacementTextValid = true;

            // only process documents which possibly contain the identifiers.
            _documentIdOfRenameSymbolDeclaration = renameLocationSet.Solution.GetRequiredDocument(renameSymbolDeclarationLocation.SourceTree!).Id;

            _renameAnnotations = new AnnotationTable<RenameAnnotation>(RenameAnnotation.Kind);
        }

        private SymbolRenameOptions RenameOptions => _renameLocationSet.Options;
        private CodeCleanupOptionsProvider FallbackOptions { get; }

        private readonly struct ConflictLocationInfo
        {
            // The span of the Node that needs to be complexified 
            public readonly TextSpan ComplexifiedSpan;
            public readonly DocumentId DocumentId;

            // The identifier span that needs to be checked for conflict
            public readonly TextSpan OriginalIdentifierSpan;

            public ConflictLocationInfo(RelatedLocation location)
            {
                Debug.Assert(location.ComplexifiedTargetSpan.Contains(location.ConflictCheckSpan) || location.Type == RelatedLocationType.UnresolvableConflict);
                this.ComplexifiedSpan = location.ComplexifiedTargetSpan;
                this.DocumentId = location.DocumentId;
                this.OriginalIdentifierSpan = location.ConflictCheckSpan;
            }
        }

        // The method which performs rename, resolves the conflict locations and returns the result of the rename operation
        public async Task<MutableConflictResolution> ResolveConflictsAsync()
        {
            try
            {
                var (documentsIdsToBeCheckedForConflict, possibleNameConflicts) = await FindDocumentsAndPossibleNameConflictsAsync().ConfigureAwait(false);
                var baseSolution = _renameLocationSet.Solution;

                var dependencyGraph = baseSolution.GetProjectDependencyGraph();
                var topologicallySortedProjects = dependencyGraph.GetTopologicallySortedProjects(_cancellationToken).ToList();

                // Process rename one project at a time to improve caching and reduce syntax tree serialization.
                var documentsGroupedByTopologicallySortedProjectId = documentsIdsToBeCheckedForConflict
                    .GroupBy(d => d.ProjectId)
                    .OrderBy(g => topologicallySortedProjects.IndexOf(g.Key));

                _replacementTextValid = IsIdentifierValid_Worker(baseSolution, _replacementText, documentsGroupedByTopologicallySortedProjectId.Select(g => g.Key));
                var renamedSpansTracker = new RenamedSpansTracker();
                var conflictResolution = new MutableConflictResolution(baseSolution, renamedSpansTracker, _replacementText, _replacementTextValid);

                var intermediateSolution = conflictResolution.OldSolution;
                foreach (var documentsByProject in documentsGroupedByTopologicallySortedProjectId)
                {
                    var conflictLocations = ImmutableHashSet<ConflictLocationInfo>.Empty;

                    var documentIdsThatGetsAnnotatedAndRenamed = new HashSet<DocumentId>(documentsByProject);
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
                        conflictResolution.UpdateCurrentSolution(await AnnotateAndRename_WorkerAsync(
                            baseSolution,
                            conflictResolution.CurrentSolution,
                            documentIdsThatGetsAnnotatedAndRenamed,
                            _renameLocationSet.Locations,
                            renamedSpansTracker,
                            _replacementTextValid,
                            conflictLocations,
                            possibleNameConflicts).ConfigureAwait(false));

                        // Step 2: Check for conflicts in the renamed solution
                        var foundResolvableConflicts = await IdentifyConflictsAsync(
                            documentIdsForConflictResolution: documentIdsThatGetsAnnotatedAndRenamed,
                            allDocumentIdsInProject: documentsByProject,
                            projectId: documentsByProject.Key,
                            conflictResolution,
                            conflictLocations).ConfigureAwait(false);

                        if (!foundResolvableConflicts || phase == 3)
                        {
                            break;
                        }

                        if (phase == 0)
                        {
                            Contract.ThrowIfTrue(conflictLocations.Count != 0, "We're the first phase, so we should have no conflict locations yet");

                            conflictLocations = conflictResolution.RelatedLocations
                                .Where(loc => documentIdsThatGetsAnnotatedAndRenamed.Contains(loc.DocumentId) && loc.Type == RelatedLocationType.PossiblyResolvableConflict && loc.IsReference)
                                .Select(loc => new ConflictLocationInfo(loc))
                                .ToImmutableHashSet();

                            // If there were no conflicting locations in references, then the first conflict phase has to be skipped.
                            if (conflictLocations.Count == 0)
                            {
                                phase++;
                            }
                        }

                        if (phase == 1)
                        {
                            conflictLocations = conflictLocations.Concat(conflictResolution.RelatedLocations
                                .Where(loc => documentIdsThatGetsAnnotatedAndRenamed.Contains(loc.DocumentId) && loc.Type == RelatedLocationType.PossiblyResolvableConflict)
                                .Select(loc => new ConflictLocationInfo(loc)))
                                .ToImmutableHashSet();
                        }

                        // Set the documents with conflicts that need to be processed in the next phase.
                        // Note that we need to get the conflictLocations here since we're going to remove some locations below if phase == 2
                        documentIdsThatGetsAnnotatedAndRenamed = new HashSet<DocumentId>(conflictLocations.Select(l => l.DocumentId));

                        if (phase == 2)
                        {
                            // After phase 2, if there are still conflicts then remove the conflict locations from being expanded
                            var unresolvedLocations = conflictResolution.RelatedLocations
                                .Where(l => (l.Type & RelatedLocationType.UnresolvedConflict) != 0)
                                .Select(l => Tuple.Create(l.ComplexifiedTargetSpan, l.DocumentId)).Distinct();

                            conflictLocations = conflictLocations
                                .Where(l => !unresolvedLocations.Any(c => c.Item2 == l.DocumentId && c.Item1.Contains(l.OriginalIdentifierSpan)))
                                .ToImmutableHashSet();
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

                // This rename could break implicit references of this symbol (e.g. rename MoveNext on a collection like type in a 
                // foreach/for each statement
                var renamedSymbolInNewSolution = await GetRenamedSymbolInCurrentSolutionAsync(conflictResolution).ConfigureAwait(false);

                if (IsRenameValid(conflictResolution, renamedSymbolInNewSolution))
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
                await DebugVerifyNoErrorsAsync(conflictResolution, documentsIdsToBeCheckedForConflict).ConfigureAwait(false);
#endif

                // Step 5: Rename declaration files
                if (_replacementTextValid && RenameOptions.RenameFile)
                {
                    var definitionLocations = _renameLocationSet.Symbol.Locations;
                    var definitionDocuments = definitionLocations
                        .Select(l => conflictResolution.OldSolution.GetRequiredDocument(l.SourceTree!))
                        .Distinct();

                    if (definitionDocuments.Count() == 1)
                    {
                        // At the moment, only single document renaming is allowed
                        conflictResolution.RenameDocumentToMatchNewSymbol(definitionDocuments.Single());
                    }
                }

                return conflictResolution;
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, ErrorSeverity.Critical))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

#if DEBUG
        private async Task DebugVerifyNoErrorsAsync(MutableConflictResolution conflictResolution, IEnumerable<DocumentId> documents)
        {
            var documentIdErrorStateLookup = new Dictionary<DocumentId, bool>();

            // we only check for the documentIds we add annotations to, which is a subset of the ones we're going 
            // to change the syntax in.
            foreach (var documentId in documents)
            {
                // remember if there were issues in the document prior to renaming it.
                var originalDoc = conflictResolution.OldSolution.GetRequiredDocument(documentId);
                documentIdErrorStateLookup.Add(documentId, await originalDoc.HasAnyErrorsAsync(_cancellationToken).ConfigureAwait(false));
            }

            // We want to ignore few error message introduced by rename because the user is wantedly doing it.
            var ignoreErrorCodes = new List<string>
            {
                "BC30420", // BC30420 - Sub Main missing in VB Project
                "CS5001" // CS5001 - Missing Main in C# Project
            };

            // only check if rename thinks it was successful
            if (conflictResolution.ReplacementTextValid && conflictResolution.RelatedLocations.All(loc => (loc.Type & RelatedLocationType.UnresolvableConflict) == 0))
            {
                foreach (var documentId in documents)
                {
                    // only check documents that had no errors before rename (we might have 
                    // fixed them because of rename).  Also, don't bother checking if a custom
                    // callback was provided.  The caller might be ok with a rename that introduces
                    // errors.
                    if (!documentIdErrorStateLookup[documentId] && _nonConflictSymbolKeys.IsDefault)
                    {
                        await conflictResolution.CurrentSolution.GetRequiredDocument(documentId).VerifyNoErrorsAsync("Rename introduced errors in error-free code", _cancellationToken, ignoreErrorCodes).ConfigureAwait(false);
                    }
                }
            }
        }
#endif

        /// <summary>
        /// Find conflicts in the new solution 
        /// </summary>
        private async Task<bool> IdentifyConflictsAsync(
            HashSet<DocumentId> documentIdsForConflictResolution,
            IEnumerable<DocumentId> allDocumentIdsInProject,
            ProjectId projectId,
            MutableConflictResolution conflictResolution,
            ImmutableHashSet<ConflictLocationInfo> conflictLocations)
        {
            try
            {
                _documentOfRenameSymbolHasBeenRenamed |= documentIdsForConflictResolution.Contains(_documentIdOfRenameSymbolDeclaration);

                // Get the renamed symbol in complexified new solution
                var renamedSymbolInNewSolution = await GetRenamedSymbolInCurrentSolutionAsync(conflictResolution).ConfigureAwait(false);

                // if the text replacement is invalid, we just did a simple token replacement.
                // Therefore we don't need more mapping information and can skip the rest of 
                // the loop body.
                if (!IsRenameValid(conflictResolution, renamedSymbolInNewSolution))
                {
                    foreach (var documentId in documentIdsForConflictResolution)
                    {
                        var newDocument = conflictResolution.CurrentSolution.GetRequiredDocument(documentId);
                        var syntaxRoot = await newDocument.GetRequiredSyntaxRootAsync(_cancellationToken).ConfigureAwait(false);

                        var nodesOrTokensWithConflictCheckAnnotations = GetNodesOrTokensToCheckForConflicts(syntaxRoot);
                        foreach (var (syntax, annotation) in nodesOrTokensWithConflictCheckAnnotations)
                        {
                            if (annotation.IsRenameLocation)
                            {
                                conflictResolution.AddRelatedLocation(new RelatedLocation(
                                    annotation.OriginalSpan, documentId, RelatedLocationType.UnresolvedConflict));
                            }
                        }
                    }

                    return false;
                }

                var reverseMappedLocations = new Dictionary<Location, Location>();

                // If we were giving any non-conflict-symbols then ensure that we know what those symbols are in
                // the current project post after our edits so far.
                var currentProject = conflictResolution.CurrentSolution.GetRequiredProject(projectId);
                var nonConflictSymbols = await GetNonConflictSymbolsAsync(currentProject).ConfigureAwait(false);

                foreach (var documentId in documentIdsForConflictResolution)
                {
                    var newDocument = conflictResolution.CurrentSolution.GetRequiredDocument(documentId);
                    var syntaxRoot = await newDocument.GetRequiredSyntaxRootAsync(_cancellationToken).ConfigureAwait(false);
                    var baseDocument = conflictResolution.OldSolution.GetRequiredDocument(documentId);
                    var baseSyntaxTree = await baseDocument.GetRequiredSyntaxTreeAsync(_cancellationToken).ConfigureAwait(false);
                    var baseRoot = await baseDocument.GetRequiredSyntaxRootAsync(_cancellationToken).ConfigureAwait(false);
                    SemanticModel? newDocumentSemanticModel = null;
                    var syntaxFactsService = newDocument.Project.Services.GetRequiredService<ISyntaxFactsService>();

                    // Get all tokens that need conflict check
                    var nodesOrTokensWithConflictCheckAnnotations = GetNodesOrTokensToCheckForConflicts(syntaxRoot);

                    var complexifiedLocationSpanForThisDocument = conflictLocations
                        .Where(t => t.DocumentId == documentId)
                        .Select(t => t.OriginalIdentifierSpan)
                        .ToSet();

                    foreach (var (syntax, annotation) in nodesOrTokensWithConflictCheckAnnotations)
                    {
                        var tokenOrNode = syntax;
                        var conflictAnnotation = annotation;
                        reverseMappedLocations[tokenOrNode.GetLocation()!] = baseSyntaxTree.GetLocation(conflictAnnotation.OriginalSpan);
                        var originalLocation = conflictAnnotation.OriginalSpan;
                        ImmutableArray<ISymbol> newReferencedSymbols = default;

                        var hasConflict = _renameAnnotations.HasAnnotation(tokenOrNode, RenameInvalidIdentifierAnnotation.Instance);
                        if (!hasConflict)
                        {
                            newDocumentSemanticModel ??= await newDocument.GetRequiredSemanticModelAsync(_cancellationToken).ConfigureAwait(false);
                            newReferencedSymbols = GetSymbolsInNewSolution(newDocument, newDocumentSemanticModel, conflictAnnotation, tokenOrNode);

                            // The semantic correctness, after rename, for each token of interest in the
                            // rename context is performed by getting the symbol pointed by each token 
                            // and obtain the Symbol's First Ordered Location's  Span-Start and check to
                            // see if it is the same as before from the base solution. During rename, 
                            // the spans would have been modified and so we need to adjust the old position
                            // to the new position for which we use the renameSpanTracker, which was tracking
                            // & mapping the old span -> new span during rename
                            hasConflict =
                                !IsConflictFreeChange(newReferencedSymbols, nonConflictSymbols) &&
                                await CheckForConflictAsync(conflictResolution, renamedSymbolInNewSolution, conflictAnnotation, newReferencedSymbols).ConfigureAwait(false);

                            if (!hasConflict && !conflictAnnotation.IsInvocationExpression)
                                hasConflict = LocalVariableConflictPerLanguage((SyntaxToken)tokenOrNode, newDocument, newReferencedSymbols);
                        }

                        if (!hasConflict)
                        {
                            if (conflictAnnotation.IsRenameLocation)
                            {
                                conflictResolution.AddRelatedLocation(
                                    new RelatedLocation(originalLocation,
                                    documentId,
                                    complexifiedLocationSpanForThisDocument.Contains(originalLocation) ? RelatedLocationType.ResolvedReferenceConflict : RelatedLocationType.NoConflict,
                                    IsReference: true));
                            }
                            else
                            {
                                // if a complexified location was not a reference location, then it was a resolved conflict of a non reference location
                                if (!conflictAnnotation.IsOriginalTextLocation && complexifiedLocationSpanForThisDocument.Contains(originalLocation))
                                {
                                    conflictResolution.AddRelatedLocation(
                                        new RelatedLocation(originalLocation,
                                        documentId,
                                        RelatedLocationType.ResolvedNonReferenceConflict,
                                        IsReference: false));
                                }
                            }
                        }
                        else
                        {
                            var baseToken = baseRoot.FindToken(conflictAnnotation.OriginalSpan.Start, true);
                            var complexifiedTarget = GetExpansionTargetForLocationPerLanguage(baseToken, baseDocument);
                            conflictResolution.AddRelatedLocation(new RelatedLocation(
                                originalLocation,
                                documentId,
                                complexifiedTarget != null ? RelatedLocationType.PossiblyResolvableConflict : RelatedLocationType.UnresolvableConflict,
                                IsReference: conflictAnnotation.IsRenameLocation,
                                ComplexifiedTargetSpan: complexifiedTarget != null ? complexifiedTarget.Span : default));
                        }
                    }
                }

                // there are more conflicts that cannot be identified by checking if the tokens still reference the same
                // symbol. These conflicts are mostly language specific. A good example is a member with the same name
                // as the parent (yes I know, this is a simplification).
                if (_documentIdOfRenameSymbolDeclaration.ProjectId == projectId)
                {
                    // Calculating declaration conflicts may require location mapping in documents
                    // that were not otherwise being processed in the current rename phase, so add
                    // the annotated spans in these documents to reverseMappedLocations.
                    foreach (var unprocessedDocumentIdWithPotentialDeclarationConflicts in allDocumentIdsInProject.Where(d => !documentIdsForConflictResolution.Contains(d)))
                    {
                        var newDocument = conflictResolution.CurrentSolution.GetRequiredDocument(unprocessedDocumentIdWithPotentialDeclarationConflicts);
                        var syntaxRoot = await newDocument.GetRequiredSyntaxRootAsync(_cancellationToken).ConfigureAwait(false);
                        var baseDocument = conflictResolution.OldSolution.GetRequiredDocument(unprocessedDocumentIdWithPotentialDeclarationConflicts);
                        var baseSyntaxTree = await baseDocument.GetRequiredSyntaxTreeAsync(_cancellationToken).ConfigureAwait(false);

                        var nodesOrTokensWithConflictCheckAnnotations = GetNodesOrTokensToCheckForConflicts(syntaxRoot);
                        foreach (var (syntax, annotation) in nodesOrTokensWithConflictCheckAnnotations)
                        {
                            var tokenOrNode = syntax;
                            var conflictAnnotation = annotation;
                            reverseMappedLocations[tokenOrNode.GetLocation()!] = baseSyntaxTree.GetLocation(conflictAnnotation.OriginalSpan);
                        }
                    }

                    var referencedSymbols = _renameLocationSet.ReferencedSymbols;
                    var renameSymbol = _renameLocationSet.Symbol;
                    await AddDeclarationConflictsAsync(
                        renamedSymbolInNewSolution, renameSymbol, referencedSymbols, conflictResolution, reverseMappedLocations, _cancellationToken).ConfigureAwait(false);
                }

                return conflictResolution.RelatedLocations.Any(r => r.Type == RelatedLocationType.PossiblyResolvableConflict);
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, ErrorSeverity.Critical))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        private async Task<ImmutableHashSet<ISymbol>?> GetNonConflictSymbolsAsync(Project currentProject)
        {
            if (_nonConflictSymbolKeys.IsDefault)
                return null;

            var compilation = await currentProject.GetRequiredCompilationAsync(_cancellationToken).ConfigureAwait(false);
            return ImmutableHashSet.CreateRange(
                _nonConflictSymbolKeys.Select(s => s.Resolve(compilation).GetAnySymbol()).WhereNotNull());
        }

        private static bool IsConflictFreeChange(
            ImmutableArray<ISymbol> symbols, ImmutableHashSet<ISymbol>? nonConflictSymbols)
        {
            if (nonConflictSymbols != null)
            {
                foreach (var symbol in symbols)
                {
                    // Reference not points at a symbol in the conflict-free list.  This is a conflict-free change.
                    if (nonConflictSymbols.Contains(symbol))
                        return true;
                }
            }

            // Just do the default check.
            return false;
        }

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

        private async Task<bool> CheckForConflictAsync(
            MutableConflictResolution conflictResolution,
            ISymbol renamedSymbolInNewSolution,
            RenameActionAnnotation conflictAnnotation,
            ImmutableArray<ISymbol> newReferencedSymbols)
        {
            try
            {
                bool hasConflict;
                var solution = conflictResolution.CurrentSolution;

                if (conflictAnnotation.IsNamespaceDeclarationReference)
                {
                    hasConflict = false;
                }
                else if (conflictAnnotation.IsMemberGroupReference)
                {
                    if (!conflictAnnotation.RenameDeclarationLocationReferences.Any())
                    {
                        hasConflict = false;
                    }
                    else
                    {
                        // Ensure newReferencedSymbols contains at least one of the original referenced
                        // symbols, and allow any new symbols to be added to the set of references.

                        hasConflict = true;

                        var newLocationTasks = newReferencedSymbols.Select(async symbol => await GetSymbolLocationAsync(solution, symbol, _cancellationToken).ConfigureAwait(false));
                        var newLocations = (await Task.WhenAll(newLocationTasks).ConfigureAwait(false)).WhereNotNull().Where(loc => loc.IsInSource);
                        foreach (var originalReference in conflictAnnotation.RenameDeclarationLocationReferences.Where(loc => loc.IsSourceLocation))
                        {
                            var adjustedStartPosition = conflictResolution.GetAdjustedTokenStartingPosition(originalReference.TextSpan.Start, originalReference.DocumentId);
                            if (newLocations.Any(loc => loc.SourceSpan.Start == adjustedStartPosition))
                            {
                                hasConflict = false;
                                break;
                            }
                        }
                    }
                }
                else if (!conflictAnnotation.IsRenameLocation && conflictAnnotation.IsOriginalTextLocation && conflictAnnotation.RenameDeclarationLocationReferences.Length > 1 && newReferencedSymbols.Length == 1)
                {
                    // an ambiguous situation was resolved through rename in non reference locations
                    hasConflict = false;
                }
                else if (newReferencedSymbols.Length != conflictAnnotation.RenameDeclarationLocationReferences.Length)
                {
                    // Don't show conflicts for errors in the old solution that now bind in the new solution.
                    if (newReferencedSymbols.Length != 0 && conflictAnnotation.RenameDeclarationLocationReferences.Length == 0)
                    {
                        hasConflict = false;
                    }
                    else
                    {
                        hasConflict = true;
                    }
                }
                else
                {
                    hasConflict = false;
                    var symbolIndex = 0;
                    foreach (var symbol in newReferencedSymbols)
                    {
                        if (conflictAnnotation.RenameDeclarationLocationReferences[symbolIndex].SymbolLocationsCount != symbol.Locations.Length)
                        {
                            hasConflict = true;
                            break;
                        }

                        var newLocation = await GetSymbolLocationAsync(solution, symbol, _cancellationToken).ConfigureAwait(false);

                        if (newLocation != null && conflictAnnotation.RenameDeclarationLocationReferences[symbolIndex].IsSourceLocation)
                        {
                            // location was in source before, but not after rename
                            if (!newLocation.IsInSource)
                            {
                                hasConflict = true;
                                break;
                            }

                            var renameDeclarationLocationReference = conflictAnnotation.RenameDeclarationLocationReferences[symbolIndex];
                            var newAdjustedStartPosition = conflictResolution.GetAdjustedTokenStartingPosition(renameDeclarationLocationReference.TextSpan.Start, renameDeclarationLocationReference.DocumentId);
                            if (newAdjustedStartPosition != newLocation.SourceSpan.Start)
                            {
                                hasConflict = true;
                                break;
                            }

                            if (conflictAnnotation.RenameDeclarationLocationReferences[symbolIndex].IsOverriddenFromMetadata)
                            {
                                var overridingSymbol = await SymbolFinder.FindSymbolAtPositionAsync(solution.GetRequiredDocument(newLocation.SourceTree), newLocation.SourceSpan.Start, cancellationToken: _cancellationToken).ConfigureAwait(false);
                                if (overridingSymbol != null && !Equals(renamedSymbolInNewSolution, overridingSymbol))
                                {
                                    if (!overridingSymbol.IsOverride)
                                    {
                                        hasConflict = true;
                                        break;
                                    }
                                    else
                                    {
                                        var overriddenSymbol = overridingSymbol.GetOverriddenMember();
                                        if (overriddenSymbol == null || !overriddenSymbol.Locations.All(loc => loc.IsInMetadata))
                                        {
                                            hasConflict = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            var newMetadataName = symbol.ToDisplayString(s_metadataSymbolDisplayFormat);
                            var oldMetadataName = conflictAnnotation.RenameDeclarationLocationReferences[symbolIndex].Name;
                            if (newLocation == null ||
                                newLocation.IsInSource ||
                                !HeuristicMetadataNameEquivalenceCheck(
                                    oldMetadataName,
                                    newMetadataName,
                                    _originalText,
                                    _replacementText))
                            {
                                hasConflict = true;
                                break;
                            }
                        }

                        symbolIndex++;
                    }
                }

                return hasConflict;
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, ErrorSeverity.Critical))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        private ImmutableArray<ISymbol> GetSymbolsInNewSolution(Document newDocument, SemanticModel newDocumentSemanticModel, RenameActionAnnotation conflictAnnotation, SyntaxNodeOrToken tokenOrNode)
        {
            var newReferencedSymbols = RenameUtilities.GetSymbolsTouchingPosition(tokenOrNode.Span.Start, newDocumentSemanticModel, newDocument.Project.Solution.Services, _cancellationToken);

            if (conflictAnnotation.IsInvocationExpression)
            {
                if (tokenOrNode.IsNode)
                {
                    var invocationReferencedSymbols = SymbolsForEnclosingInvocationExpressionWorker((SyntaxNode)tokenOrNode!, newDocumentSemanticModel, _cancellationToken);
                    if (!invocationReferencedSymbols.IsDefault)
                        newReferencedSymbols = invocationReferencedSymbols;
                }
            }

            // if there are more than one symbol, then remove the alias symbols.
            // When using (not declaring) an alias, the alias symbol and the target symbol are returned
            // by GetSymbolsTouchingPosition
            if (newReferencedSymbols.Length >= 2)
                newReferencedSymbols = newReferencedSymbols.WhereAsArray(a => a.Kind != SymbolKind.Alias);

            return newReferencedSymbols;
        }

        private async Task<ISymbol> GetRenamedSymbolInCurrentSolutionAsync(MutableConflictResolution conflictResolution)
        {
            try
            {
                // get the renamed symbol in complexified new solution
                var start = _documentOfRenameSymbolHasBeenRenamed
                    ? conflictResolution.GetAdjustedTokenStartingPosition(_renameSymbolDeclarationLocation.SourceSpan.Start, _documentIdOfRenameSymbolDeclaration)
                    : _renameSymbolDeclarationLocation.SourceSpan.Start;

                var document = conflictResolution.CurrentSolution.GetRequiredDocument(_documentIdOfRenameSymbolDeclaration);
                var newSymbol = await SymbolFinder.FindSymbolAtPositionAsync(document, start, cancellationToken: _cancellationToken).ConfigureAwait(false);
                return newSymbol;
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, ErrorSeverity.Critical))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        /// <summary>
        /// The method determines the set of documents that need to be processed for Rename and also determines
        /// the possible set of names that need to be checked for conflicts.
        /// The list will contains Strings like Bar -> BarAttribute ; Property Bar -> Bar , get_Bar, set_Bar
        /// </summary>
        private async Task<(ImmutableHashSet<DocumentId> documentIds, ImmutableArray<string> possibleNameConflicts)> FindDocumentsAndPossibleNameConflictsAsync()
        {
            try
            {
                var documentIds = new HashSet<DocumentId>();
                var possibleNameConflictsList = new List<string>();

                var symbol = _renameLocationSet.Symbol;
                var solution = _renameLocationSet.Solution;

                var allRenamedDocuments = _renameLocationSet.Locations.Select(loc => loc.Location.SourceTree!).Distinct().Select(solution.GetRequiredDocument);
                documentIds.AddRange(allRenamedDocuments.Select(d => d.Id));

                var documentsFromAffectedProjects = RenameUtilities.GetDocumentsAffectedByRename(symbol, solution, _renameLocationSet.Locations);
                foreach (var language in documentsFromAffectedProjects.Select(d => d.Project.Language).Distinct())
                {
                    solution.Services.GetLanguageServices(language).GetService<IRenameRewriterLanguageService>()
                        ?.TryAddPossibleNameConflicts(symbol, _replacementText, possibleNameConflictsList);
                }

                var possibleNameConflicts = possibleNameConflictsList.ToImmutableArray();
                await AddDocumentsWithPotentialConflictsAsync(documentsFromAffectedProjects, documentIds, possibleNameConflicts).ConfigureAwait(false);
                return (documentIds.ToImmutableHashSet(), possibleNameConflicts);
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, ErrorSeverity.Critical))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        private async Task AddDocumentsWithPotentialConflictsAsync(
            IEnumerable<Document> documents,
            HashSet<DocumentId> documentIds,
            ImmutableArray<string> possibleNameConflicts)
        {
            try
            {
                foreach (var document in documents)
                {
                    if (documentIds.Contains(document.Id))
                        continue;

                    if (!document.SupportsSyntaxTree)
                        continue;

                    var info = await SyntaxTreeIndex.GetRequiredIndexAsync(document, _cancellationToken).ConfigureAwait(false);
                    if (info.ProbablyContainsEscapedIdentifier(_originalText))
                    {
                        documentIds.Add(document.Id);
                        continue;
                    }

                    if (info.ProbablyContainsIdentifier(_replacementText))
                    {
                        documentIds.Add(document.Id);
                        continue;
                    }

                    foreach (var replacementName in possibleNameConflicts)
                    {
                        if (info.ProbablyContainsIdentifier(replacementName))
                        {
                            documentIds.Add(document.Id);
                            break;
                        }
                    }
                }
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, ErrorSeverity.Critical))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        // The rename process and annotation for the bookkeeping is performed in one-step
        private async Task<Solution> AnnotateAndRename_WorkerAsync(
            Solution originalSolution,
            Solution partiallyRenamedSolution,
            HashSet<DocumentId> documentIdsToRename,
            ImmutableArray<RenameLocation> renameLocations,
            RenamedSpansTracker renameSpansTracker,
            bool replacementTextValid,
            ISet<ConflictLocationInfo> conflictLocations,
            ImmutableArray<string> possibleNameConflicts)
        {
            try
            {
                foreach (var documentId in documentIdsToRename.ToList())
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    var document = originalSolution.GetRequiredDocument(documentId);
                    var semanticModel = await document.GetRequiredSemanticModelAsync(_cancellationToken).ConfigureAwait(false);
                    var originalSyntaxRoot = await semanticModel.SyntaxTree.GetRootAsync(_cancellationToken).ConfigureAwait(false);

                    // Get all rename locations for the current document.
                    var allTextSpansInSingleSourceTree = renameLocations
                        .Where(l => l.DocumentId == documentId && ShouldIncludeLocation(renameLocations, l))
                        .ToImmutableDictionary(l => l.Location.SourceSpan);

                    // All textspan in the document documentId, that requires rename in String or Comment
                    var stringAndCommentTextSpansInSingleSourceTree = renameLocations
                        .Where(l => l.DocumentId == documentId && l.IsRenameInStringOrComment)
                        .GroupBy(l => l.ContainingLocationForStringOrComment)
                        .ToImmutableDictionary(
                            g => g.Key,
                            g => GetSubSpansToRenameInStringAndCommentTextSpans(g.Key, g));

                    var conflictLocationSpans = conflictLocations
                        .Where(t => t.DocumentId == documentId)
                        .Select(t => t.ComplexifiedSpan)
                        .ToImmutableHashSet();

                    // Annotate all nodes with a RenameLocation annotations to record old locations & old referenced symbols.
                    // Also annotate nodes that should get complexified (nodes for rename locations + conflict locations)
                    var parameters = new RenameRewriterParameters(
                        _renamedSymbolDeclarationAnnotation,
                        document,
                        semanticModel,
                        originalSyntaxRoot,
                        _replacementText,
                        _originalText,
                        possibleNameConflicts,
                        allTextSpansInSingleSourceTree,
                        stringAndCommentTextSpansInSingleSourceTree,
                        conflictLocationSpans,
                        originalSolution,
                        _renameLocationSet.Symbol,
                        replacementTextValid,
                        renameSpansTracker,
                        RenameOptions.RenameInStrings,
                        RenameOptions.RenameInComments,
                        _renameAnnotations,
                        _cancellationToken);

                    var renameRewriterLanguageService = document.GetRequiredLanguageService<IRenameRewriterLanguageService>();
                    var newRoot = renameRewriterLanguageService.AnnotateAndRename(parameters);

                    if (newRoot == originalSyntaxRoot)
                    {
                        // Update the list for the current phase, some files with strings containing the original or replacement
                        // text may have been filtered out.
                        documentIdsToRename.Remove(documentId);
                    }
                    else
                    {
                        partiallyRenamedSolution = partiallyRenamedSolution.WithDocumentSyntaxRoot(documentId, newRoot, PreservationMode.PreserveIdentity);
                    }
                }

                return partiallyRenamedSolution;
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        /// We try to rewrite all locations that are invalid candidate locations. If there is only
        /// one location it must be the correct one (the symbol is ambiguous to something else)
        /// and we always try to rewrite it.  If there are multiple locations, we only allow it
        /// if the candidate reason allows for it).
        private static bool ShouldIncludeLocation(ImmutableArray<RenameLocation> renameLocations, RenameLocation location)
        {
            if (location.IsRenameInStringOrComment)
            {
                return false;
            }

            if (renameLocations.Length == 1)
            {
                return true;
            }

            return RenameLocation.ShouldRename(location);
        }

        /// <summary>
        /// We try to compute the sub-spans to rename within the given <paramref name="containingLocationForStringOrComment"/>.
        /// If we are renaming within a string, the locations to rename are always within this containing string location
        /// and we can identify these sub-spans.
        /// However, if we are renaming within a comment, the rename locations can be anywhere in trivia,
        /// so we return null and the rename rewriter will perform a complete regex match within comment trivia
        /// and rename all matches instead of specific matches.
        /// </summary>
        private static ImmutableSortedSet<TextSpan>? GetSubSpansToRenameInStringAndCommentTextSpans(
            TextSpan containingLocationForStringOrComment,
            IEnumerable<RenameLocation> locationsToRename)
        {
            var builder = ImmutableSortedSet.CreateBuilder<TextSpan>();
            foreach (var renameLocation in locationsToRename)
            {
                if (!containingLocationForStringOrComment.Contains(renameLocation.Location.SourceSpan))
                {
                    // We found a location outside the 'containingLocationForStringOrComment',
                    // which is likely in trivia.
                    // Bail out from computing specific sub-spans and let the rename rewriter
                    // do a full regex match and replace.
                    return null;
                }

                // Compute the sub-span within 'containingLocationForStringOrComment' that needs to be renamed.
                var offset = renameLocation.Location.SourceSpan.Start - containingLocationForStringOrComment.Start;
                var length = renameLocation.Location.SourceSpan.Length;
                var subSpan = new TextSpan(offset, length);
                builder.Add(subSpan);
            }

            return builder.ToImmutable();
        }
    }
}
