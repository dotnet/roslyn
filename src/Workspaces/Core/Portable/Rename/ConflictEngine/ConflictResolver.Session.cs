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
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    internal static partial class ConflictResolver
    {
        /// <summary>
        /// Helper class to track the state necessary for finding/resolving conflicts in a 
        /// rename session.
        /// </summary>
        private abstract class Session
        {
            private readonly Solution _baseSolution;
            private readonly CodeCleanupOptionsProvider _fallbackOptions;

            private readonly ImmutableArray<SymbolKey> _nonConflictSymbolKeys;
            private readonly ImmutableArray<ProjectId> _topologicallySortedProjects;
            private readonly AnnotationTable<RenameAnnotation> _renameAnnotations = new(RenameAnnotation.Kind);

            /// <summary>
            /// Rename symbol to its corresponding rename locations.
            /// </summary>
            private readonly ImmutableDictionary<ISymbol, SymbolicRenameLocations> _symbolToRenameLocations;

            /// <summary>
            /// All the immutable information to rename and annotated a document during the rename and conflict resolution.
            /// </summary>
            private readonly ImmutableDictionary<DocumentId, DocumentRenameInfo> _documentIdToRenameInfo;

            /// <summary>
            /// All the documents need to be visited during rename and conflict resolution.
            /// </summary>
            private readonly ImmutableHashSet<DocumentId> _documentsIdsToBeCheckedForConflict;

            /// <summary>
            /// Rename symbol to the replacement text.
            /// </summary>
            private readonly ImmutableDictionary<ISymbol, string> _symbolToReplacementText;

            /// <summary>
            /// Rename symbol to whether the replacement text is valid or not.
            /// </summary>
            private readonly ImmutableDictionary<ISymbol, bool> _symbolToReplacementTextValid;

            /// <summary>
            /// Rename symbol to its declaration documentId and declaration location.
            /// </summary>
            private readonly ImmutableDictionary<ISymbol, (DocumentId declarationDocumentId, Location declarationLocation)> _symbolToDeclarationDocumentAndLocation;

            private ISet<ConflictLocationInfo> _conflictLocations = SpecializedCollections.EmptySet<ConflictLocationInfo>();
            private readonly CancellationToken _cancellationToken;

            protected Session(
                Solution solution,
                ImmutableArray<SymbolicRenameLocations> symbolicRenameLocations,
                ImmutableArray<SymbolKey> nonConflictSymbolKeys,
                ImmutableDictionary<DocumentId, DocumentRenameInfo> documentIdToRenameInfo,
                ImmutableDictionary<ISymbol, string> symbolToReplacementText,
                ImmutableDictionary<ISymbol, bool> symbolToReplacementTextValid,
                ImmutableDictionary<ISymbol, (DocumentId declarationDocumentId, Location declarationLocation)> symbolToDeclarationDocumentAndLocation,
                CodeCleanupOptionsProvider fallBackOptions,
                CancellationToken cancellationToken)
            {
                var dependencyGraph = solution.GetProjectDependencyGraph();
                _symbolToRenameLocations = symbolicRenameLocations.ToImmutableDictionary(symbolicRenameLocations => symbolicRenameLocations.Symbol);
                _baseSolution = solution;
                _topologicallySortedProjects = dependencyGraph.GetTopologicallySortedProjects(cancellationToken).ToImmutableArray();
                _cancellationToken = cancellationToken;
                _nonConflictSymbolKeys = nonConflictSymbolKeys;
                _fallbackOptions = fallBackOptions;

                _documentIdToRenameInfo = documentIdToRenameInfo;
                _documentsIdsToBeCheckedForConflict = documentIdToRenameInfo.Keys.ToImmutableHashSet();
                _symbolToReplacementText = symbolToReplacementText;
                _symbolToReplacementTextValid = symbolToReplacementTextValid;
                _symbolToDeclarationDocumentAndLocation = symbolToDeclarationDocumentAndLocation;
            }

            /// <summary>
            /// Whether the <param name="newReferencedSymbol"/> has conflict with <param name="renameDeclarationLocationReference"/>
            /// </summary>
            protected abstract bool HasConflictForMetadataReference(RenameDeclarationLocationReference renameDeclarationLocationReference, ISymbol newReferencedSymbol);

            private readonly struct ConflictLocationInfo
            {
                // The span of the Node that needs to be complexified 
                public readonly TextSpan ComplexifiedSpan;
                public readonly DocumentId DocumentId;

                // The identifier span that needs to be checked for conflict
                public readonly TextSpan OriginalIdentifierSpan;

                public ConflictLocationInfo(RelatedLocation location)
                {
                    Debug.Assert(location.ComplexifiedTargetSpan.Contains(location.ConflictCheckSpan) || location.Type is RelatedLocationType.UnresolvableConflict);
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
                    // Process rename one project at a time to improve caching and reduce syntax tree serialization.
                    var documentsGroupedByTopologicallySortedProjectId = _documentsIdsToBeCheckedForConflict
                        .GroupBy(d => d.ProjectId)
                        .OrderBy(g => _topologicallySortedProjects.IndexOf(g.Key));

                    var renamedSpansTracker = new RenamedSpansTracker();
                    var conflictResolution = new MutableConflictResolution(
                        _baseSolution,
                        renamedSpansTracker,
                        _symbolToReplacementText,
                        _symbolToReplacementTextValid);

                    var intermediateSolution = conflictResolution.OldSolution;
                    foreach (var documentsByProject in documentsGroupedByTopologicallySortedProjectId)
                    {
                        var documentIdsThatGetsAnnotatedAndRenamed = new HashSet<DocumentId>(documentsByProject);
                        var projectId = documentsByProject.Key;
                        var cacheService = _baseSolution.Services.GetService<IProjectCacheHostService>();
                        using (cacheService?.EnableCaching(documentsByProject.Key))
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
                                    _baseSolution,
                                    conflictResolution.CurrentSolution,
                                    documentIdsThatGetsAnnotatedAndRenamed,
                                    _conflictLocations,
                                    renamedSpansTracker,
                                    _renameAnnotations).ConfigureAwait(false);

                                // If the documents do not change then remove it from the conflict checking list
                                documentIdsThatGetsAnnotatedAndRenamed.RemoveRange(unchangedDocuments);
                                conflictResolution.UpdateCurrentSolution(partiallyRenamedSolution);

                                // Step 2: Check for conflicts in the renamed solution
                                var foundResolvableConflicts = await IdentifyConflictsAsync(
                                    documentIdsForConflictResolution: documentIdsThatGetsAnnotatedAndRenamed,
                                    allDocumentIdsInProject: documentsByProject,
                                    projectId: projectId,
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
                                        .Select(l => (l.ComplexifiedTargetSpan, l.DocumentId)).Distinct();

                                    _conflictLocations = _conflictLocations.Where(l => !unresolvedLocations.Any(c => c.DocumentId == l.DocumentId && c.ComplexifiedTargetSpan.Contains(l.OriginalIdentifierSpan))).ToSet();
                                }

                                // Clean up side effects from rename before entering the next phase
                                conflictResolution.ClearDocuments(documentIdsThatGetsAnnotatedAndRenamed);
                            }

                            var shouldSimplify = _symbolToReplacementTextValid.All(pair => pair.Value);
                            // Step 3: Simplify the project
                            conflictResolution.UpdateCurrentSolution(await renamedSpansTracker.SimplifyAsync(
                                conflictResolution.CurrentSolution,
                                documentsByProject,
                                shouldSimplify,
                                _renameAnnotations,
                                _fallbackOptions, _cancellationToken).ConfigureAwait(false));

                            intermediateSolution = await conflictResolution.RemoveAllRenameAnnotationsAsync(
                                intermediateSolution, documentsByProject, _renameAnnotations, _cancellationToken).ConfigureAwait(false);
                            conflictResolution.UpdateCurrentSolution(intermediateSolution);
                        }
                    }

                    // This rename could break implicit references of this symbol (e.g. rename MoveNext on a collection like type in a 
                    // foreach/for each statement
                    var originalSymbolToValidRenamedSymbolsInNewSolution = await GetValidRenamedSymbolsMapInCurrentSolutionAsync(conflictResolution).ConfigureAwait(false);
                    if (!originalSymbolToValidRenamedSymbolsInNewSolution.IsEmpty)
                    {
                        foreach (var (originalSymbol, renamedSymbolInNewSolution) in originalSymbolToValidRenamedSymbolsInNewSolution)
                        {
                            var implicitLocations = _symbolToRenameLocations[originalSymbol].ImplicitLocations;
                            var (originalSymbolDeclarationDocumentId, originalSymbolDeclarationLocation) = _symbolToDeclarationDocumentAndLocation[originalSymbol];
                            await AddImplicitConflictsAsync(
                                renamedSymbolInNewSolution,
                                originalSymbol,
                                implicitLocations,
                                await conflictResolution.CurrentSolution.GetRequiredDocument(originalSymbolDeclarationDocumentId).GetRequiredSemanticModelAsync(_cancellationToken).ConfigureAwait(false),
                                originalSymbolDeclarationLocation,
                                renamedSpansTracker.GetAdjustedPosition(originalSymbolDeclarationLocation.SourceSpan.Start, originalSymbolDeclarationDocumentId),
                                conflictResolution,
                                _cancellationToken).ConfigureAwait(false);
                        }
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
                    foreach (var (symbol, symbolicRenameLocation) in _symbolToRenameLocations)
                    {
                        if (symbolicRenameLocation.Options.RenameFile && _symbolToReplacementTextValid[symbol])
                        {
                            var definitionLocations = symbol.Locations;
                            var definitionDocuments = definitionLocations
                                .Select(l => conflictResolution.OldSolution.GetRequiredDocument(l.SourceTree!))
                                .Distinct();

                            if (definitionDocuments.Count() == 1)
                            {
                                // At the moment, only single document renaming is allowed
                                conflictResolution.RenameDocumentToMatchNewSymbol(symbol, definitionDocuments.Single());
                            }
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
                var ignoreErrorCodes = new List<string>();
                ignoreErrorCodes.Add("BC30420"); // BC30420 - Sub Main missing in VB Project
                ignoreErrorCodes.Add("CS5001"); // CS5001 - Missing Main in C# Project

                // only check if rename thinks it was successful
                if (conflictResolution.SymbolToReplacementTextValid.All(symbolToReplacementTextValid => symbolToReplacementTextValid.Value)
                    && conflictResolution.RelatedLocations.All(loc => (loc.Type & RelatedLocationType.UnresolvableConflict) == 0))
                {
                    foreach (var documentId in documents)
                    {
                        // only check documents that had no errors before rename (we might have 
                        // fixed them because of rename).  Also, don't bother checking if a custom
                        // callback was provided.  The caller might be ok with a rename that introduces
                        // errors.
                        if (!documentIdErrorStateLookup[documentId] && _nonConflictSymbolKeys.IsDefaultOrEmpty)
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
                MutableConflictResolution conflictResolution)
            {
                try
                {
                    // Get the renamed symbol in complexified new solution
                    var validRenamedSymbolsMapInNewSolution = await GetValidRenamedSymbolsMapInCurrentSolutionAsync(conflictResolution).ConfigureAwait(false);

                    // if the text replacement is invalid, we just did a simple token replacement.
                    // Therefore we don't need more mapping information and can skip the rest of 
                    // the loop body.
                    if (validRenamedSymbolsMapInNewSolution.IsEmpty)
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
                    var validRenamedSymbolsInNewSolution = validRenamedSymbolsMapInNewSolution.SelectAsArray(pair => pair.Value);

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

                        var complexifiedLocationSpanForThisDocument =
                            _conflictLocations
                            .Where(t => t.DocumentId == documentId)
                            .Select(t => t.OriginalIdentifierSpan).ToSet();

                        foreach (var (syntax, annotation) in nodesOrTokensWithConflictCheckAnnotations)
                        {
                            var tokenOrNode = syntax;
                            var conflictAnnotation = annotation;
                            reverseMappedLocations[tokenOrNode.GetLocation()!] = baseSyntaxTree.GetLocation(conflictAnnotation.OriginalSpan);
                            var originalLocation = conflictAnnotation.OriginalSpan;

                            var hasConflict = _renameAnnotations.HasAnnotation(tokenOrNode, RenameInvalidIdentifierAnnotation.Instance);
                            if (!hasConflict)
                            {
                                newDocumentSemanticModel ??= await newDocument.GetRequiredSemanticModelAsync(_cancellationToken).ConfigureAwait(false);
                                var newReferencedSymbols = GetSymbolsInNewSolution(newDocument, newDocumentSemanticModel, conflictAnnotation, tokenOrNode);

                                // The semantic correctness, after rename, for each token of interest in the
                                // rename context is performed by getting the symbol pointed by each token 
                                // and obtain the Symbol's First Ordered Location's  Span-Start and check to
                                // see if it is the same as before from the base solution. During rename, 
                                // the spans would have been modified and so we need to adjust the old position
                                // to the new position for which we use the renameSpanTracker, which was tracking
                                // & mapping the old span -> new span during rename
                                hasConflict =
                                    !IsConflictFreeChange(newReferencedSymbols, nonConflictSymbols) &&
                                    await CheckForConflictAsync(
                                        conflictResolution,
                                        validRenamedSymbolsInNewSolution,
                                        conflictAnnotation,
                                        newReferencedSymbols).ConfigureAwait(false);

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
                                        isReference: true));
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
                                            isReference: false));
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
                                    isReference: conflictAnnotation.IsRenameLocation,
                                    complexifiedTargetSpan: complexifiedTarget != null ? complexifiedTarget.Span : default));
                            }
                        }
                    }

                    var originalSymbolToDeclarationChangedSymbol = await GetDeclarationChangedSymbolsMapAsync(conflictResolution, projectId).ConfigureAwait(false);
                    // there are more conflicts that cannot be identified by checking if the tokens still reference the same
                    // symbol. These conflicts are mostly language specific. A good example is a member with the same name
                    // as the parent (yes I know, this is a simplification).
                    if (!originalSymbolToDeclarationChangedSymbol.IsEmpty)
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

                        foreach (var (originalSymbol, renamedSymbolInNewSolution) in originalSymbolToDeclarationChangedSymbol)
                        {
                            var referencedSymbols = _symbolToRenameLocations[originalSymbol].ReferencedSymbols;
                            await AddDeclarationConflictsAsync(
                                renamedSymbolInNewSolution, originalSymbol, referencedSymbols, conflictResolution, reverseMappedLocations, _cancellationToken).ConfigureAwait(false);
                        }
                    }

                    return conflictResolution.RelatedLocations.Any(r => r.Type == RelatedLocationType.PossiblyResolvableConflict);
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, ErrorSeverity.Critical))
                {
                    throw ExceptionUtilities.Unreachable();
                }
            }

            private async Task<ImmutableHashSet<ISymbol>> GetNonConflictSymbolsAsync(Project currentProject)
            {
                if (_nonConflictSymbolKeys.IsDefaultOrEmpty)
                    return ImmutableHashSet<ISymbol>.Empty;

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
                ImmutableArray<ISymbol> renamedSymbolsInNewSolution,
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
                                    if (overridingSymbol != null && !renamedSymbolsInNewSolution.Contains(overridingSymbol))
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
                                if (newLocation == null
                                    || newLocation.IsInSource
                                    || HasConflictForMetadataReference(conflictAnnotation.RenameDeclarationLocationReferences[symbolIndex], symbol))
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

            private async Task<ISymbol> GetRenamedSymbolInCurrentSolutionAsync(
                MutableConflictResolution conflictResolution,
                DocumentId documentIdOfRenameSymbolDeclaration,
                Location renameSymbolDeclarationLocation)
            {
                try
                {
                    // get the renamed symbol in complexified new solution
                    var start = conflictResolution.IsDocumentChanged(documentIdOfRenameSymbolDeclaration)
                        ? conflictResolution.GetAdjustedTokenStartingPosition(renameSymbolDeclarationLocation.SourceSpan.Start, documentIdOfRenameSymbolDeclaration)
                        : renameSymbolDeclarationLocation.SourceSpan.Start;

                    var document = conflictResolution.CurrentSolution.GetRequiredDocument(documentIdOfRenameSymbolDeclaration);
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
                        solution.Services.GetLanguageServices(language).GetService<IRenameRewriterLanguageService>()
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
                    throw ExceptionUtilities.Unreachable();
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
                    throw ExceptionUtilities.Unreachable();
                }
            }

            private async Task<(Solution partiallyRenamedSolution, ImmutableHashSet<DocumentId> unchangedDocuments)> AnnotateAndRename_WorkerAsync(
                Solution originalSolution,
                Solution partiallyRenamedSolution,
                HashSet<DocumentId> documentIdsToRename,
                ISet<ConflictLocationInfo> conflictLocations,
                RenamedSpansTracker renamedSpansTracker,
                AnnotationTable<RenameAnnotation> annotationTable)
            {
                try
                {
                    using var _ = PooledHashSet<DocumentId>.GetInstance(out var unchangedDocumentIds);
                    foreach (var documentId in documentIdsToRename)
                    {
                        _cancellationToken.ThrowIfCancellationRequested();

                        var document = originalSolution.GetRequiredDocument(documentId);
                        var semanticModel = await document.GetRequiredSemanticModelAsync(_cancellationToken).ConfigureAwait(false);
                        var originalSyntaxRoot = await semanticModel.SyntaxTree.GetRootAsync(_cancellationToken).ConfigureAwait(false);
                        var documentRenameInfo = _documentIdToRenameInfo[documentId];
                        var conflictLocationSpans = conflictLocations
                                                    .Where(t => t.DocumentId == documentId)
                                                    .Select(t => t.ComplexifiedSpan).ToSet();

                        // Annotate all nodes with a RenameLocation annotations to record old locations & old referenced symbols.
                        // Also annotate nodes that should get complexified (nodes for rename locations + conflict locations)
                        var parameters = new RenameRewriterParameters(
                            conflictLocationSpans,
                            originalSolution,
                            renamedSpansTracker,
                            originalSyntaxRoot,
                            document,
                            semanticModel,
                            annotationTable,
                            documentRenameInfo,
                            _cancellationToken);

                        var renameRewriterLanguageService = document.GetRequiredLanguageService<IRenameRewriterLanguageService>();
                        var newRoot = renameRewriterLanguageService.AnnotateAndRename(parameters);

                        if (newRoot == originalSyntaxRoot)
                        {
                            // Update the list for the current phase, some files with strings containing the original or replacement
                            // text may have been filtered out.
                            unchangedDocumentIds.Add(documentId);
                        }
                        else
                        {
                            partiallyRenamedSolution = partiallyRenamedSolution.WithDocumentSyntaxRoot(documentId, newRoot, PreservationMode.PreserveIdentity);
                        }
                    }

                    return (partiallyRenamedSolution, unchangedDocumentIds.ToImmutableHashSet());
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable();
                }
            }

            /// <summary>
            /// Get a map which contains the old symbol to its mapping new symbol in solution after renaming. Only valid renamed symbol would be in the map.
            /// </summary>
            private async Task<ImmutableDictionary<ISymbol, ISymbol>> GetValidRenamedSymbolsMapInCurrentSolutionAsync(MutableConflictResolution conflictResolution)
            {
                using var _ = PooledDictionary<ISymbol, ISymbol>.GetInstance(out var originalSymbolToNewSymbolBuilder);
                foreach (var (originalSymbol, (declarationDocumentId, declarationLocation)) in _symbolToDeclarationDocumentAndLocation)
                {
                    if (!_symbolToReplacementTextValid[originalSymbol])
                    {
                        continue;
                    }

                    // if we rename an identifier and it now binds to a symbol from metadata this should be treated as
                    // an invalid rename.
                    var renamedSymbolInNewSolution = await GetRenamedSymbolInCurrentSolutionAsync(conflictResolution, declarationDocumentId, declarationLocation).ConfigureAwait(false);
                    if (renamedSymbolInNewSolution == null || renamedSymbolInNewSolution.Locations.All(location => !location.IsInSource))
                    {
                        continue;
                    }

                    originalSymbolToNewSymbolBuilder[originalSymbol] = renamedSymbolInNewSolution;
                }

                return originalSymbolToNewSymbolBuilder.ToImmutableDictionary();
            }

            /// <summary>
            /// Get a map which contains the old symbol to its mapping new symbol in solution after renaming if the old symbol is declared in the <param name="projectId"/>.
            /// </summary>
            private async Task<ImmutableDictionary<ISymbol, ISymbol>> GetDeclarationChangedSymbolsMapAsync(
                MutableConflictResolution conflictResolution,
                ProjectId projectId)
            {
                using var _ = PooledDictionary<ISymbol, ISymbol>.GetInstance(out var originalSymbolToNewSymbolBuilder);
                foreach (var (originalSymbol, (declarationDocumentId, declarationLocation)) in _symbolToDeclarationDocumentAndLocation)
                {
                    if (declarationDocumentId.ProjectId.Equals(projectId))
                    {
                        var renamedSymbolInNewSolution = await GetRenamedSymbolInCurrentSolutionAsync(conflictResolution, declarationDocumentId, declarationLocation).ConfigureAwait(false);
                        if (renamedSymbolInNewSolution != null)
                        {
                            originalSymbolToNewSymbolBuilder[originalSymbol] = renamedSymbolInNewSolution;
                        }
                    }
                }

                return originalSymbolToNewSymbolBuilder.ToImmutableDictionary();
            }
        }
    }
}
