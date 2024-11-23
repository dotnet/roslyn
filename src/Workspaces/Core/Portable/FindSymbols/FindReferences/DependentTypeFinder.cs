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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

using SymbolSet = HashSet<INamedTypeSymbol>;

/// <summary>
/// Provides helper methods for finding dependent types (derivations, implementations, etc.) across a solution. This
/// is effectively a graph walk between INamedTypeSymbols walking down the inheritance hierarchy to find related
/// types based either on <see cref="ITypeSymbol.BaseType"/> or <see cref="ITypeSymbol.Interfaces"/>.
/// </summary>
/// <remarks>
/// While walking up the inheritance hierarchy is trivial (as the information is directly contained on the <see
/// cref="ITypeSymbol"/>'s themselves), walking down is complicated.  The general way this works is by using
/// out-of-band indices that are built that store this type information in a weak manner.  Specifically, for both
/// source and metadata types we have indices that map between the base type name and the inherited type name. i.e.
/// for the case <c>class A { } class B : A { }</c> the index stores a link saying "There is a type 'A' somewhere
/// which has derived type called 'B' somewhere".  So when the index is examined for the name 'A', it will say
/// 'examine types called 'B' to see if they're an actual match'.
/// <para/>
/// These links are then continually traversed to get the full set of results.
/// </remarks>
internal static partial class DependentTypeFinder
{
    // Static helpers so we can pass delegates around without allocations.

    private static readonly Func<Location, bool> s_isInMetadata = static loc => loc.IsInMetadata;
    private static readonly Func<Location, bool> s_isInSource = static loc => loc.IsInSource;

    private static readonly Func<INamedTypeSymbol, bool> s_isInterface = static t => t is { TypeKind: TypeKind.Interface };
    private static readonly Func<INamedTypeSymbol, bool> s_isNonSealedClass = static t => t is { TypeKind: TypeKind.Class, IsSealed: false };
    private static readonly Func<INamedTypeSymbol, bool> s_isInterfaceOrNonSealedClass = static t => s_isInterface(t) || s_isNonSealedClass(t);

    private static readonly ObjectPool<PooledHashSet<INamedTypeSymbol>> s_symbolSetPool = PooledHashSet<INamedTypeSymbol>.CreatePool(SymbolEquivalenceComparer.Instance);

    /// <summary>
    /// Walks down a <paramref name="type"/>'s inheritance tree looking for more <see cref="INamedTypeSymbol"/>'s
    /// that match the provided <paramref name="typeMatches"/> predicate.
    /// </summary>
    /// <param name="shouldContinueSearching">Called when a new match is found to check if that type's inheritance
    /// tree should also be walked down.  Can be used to stop the search early if a type could have no types that
    /// inherit from it that would match this search.</param>
    /// <param name="transitive">If this search after finding the direct inherited types that match the provided
    /// predicate, or if the search should continue recursively using those types as the starting point.</param>
    private static async Task<ImmutableArray<INamedTypeSymbol>> DescendInheritanceTreeAsync(
        INamedTypeSymbol type,
        Solution solution,
        IImmutableSet<Project>? projects,
        Func<INamedTypeSymbol, SymbolSet, bool> typeMatches,
        Func<INamedTypeSymbol, bool> shouldContinueSearching,
        bool transitive,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        type = type.OriginalDefinition;
        projects ??= ImmutableHashSet.Create(solution.Projects.ToArray());
        var searchInMetadata = type.Locations.Any(s_isInMetadata);

        // Note: it is not sufficient to just walk the list of projects passed in,
        // searching only those for derived types.
        //
        // Say we have projects: A <- B <- C, but only projects A and C are passed in.
        // We might miss a derived type in C if there's an intermediate derived type
        // in B.
        //
        // However, say we have projects A <- B <- C <- D, only projects A and C
        // are passed in.  There is no need to check D as there's no way it could
        // contribute an intermediate type that affects A or C.  We only need to check
        // A, B and C
        //
        // An exception to the above rule is if we're just searching a single project.
        // in that case there can be no intermediary projects that could add types.
        // So we can just limit ourselves to that single project.

        // First find all the projects that could potentially reference this type.
        var projectsThatCouldReferenceType = await GetProjectsThatCouldReferenceTypeAsync(
            type, solution, searchInMetadata, cancellationToken).ConfigureAwait(false);

        // Now, based on the list of projects that could actually reference the type,
        // and the list of projects the caller wants to search, find the actual list of
        // projects we need to search through.
        //
        // This list of projects is properly topologically ordered.  Because of this we
        // can just process them in order from first to last because we know no project
        // in this list could affect a prior project.
        var orderedProjectsToExamine = GetOrderedProjectsToExamine(
            solution, projects, projectsThatCouldReferenceType);

        // The final set of results we'll be returning.
        using var _1 = GetSymbolSet(out var result);

        // The current total set of matching metadata types in the descendant tree (including the initial type if it
        // is from metadata).  Will be used when examining new types to see if they inherit from any of these.
        using var _2 = GetSymbolSet(out var currentMetadataTypes);

        // Same as above, but contains source types as well.
        using var _3 = GetSymbolSet(out var currentSourceAndMetadataTypes);

        // The set of PEReferences we've examined.  We only need to examine a reference once when we encounter it
        // while walking projects.  PEReferences cannot reference source symbols, so the results from them cannot 
        // change as we examine further projects.
        using var _4 = PooledHashSet<PortableExecutableReference>.GetInstance(out var seenPEReferences);

        currentSourceAndMetadataTypes.Add(type);
        if (searchInMetadata)
            currentMetadataTypes.Add(type);

        // Now walk the projects from left to right seeing what our type cascades to. Once we 
        // reach a fixed point in that project, take all the types we've found and move to the
        // next project.  Continue this until we've exhausted all projects.
        //
        // Because there is a data-dependency between the projects, we cannot process them in
        // parallel.  (Processing linearly is also probably preferable to limit the amount of
        // cache churn we could cause creating all those compilations.
        foreach (var project in orderedProjectsToExamine)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (project.SupportsCompilation)
                await DescendInheritanceTreeInProjectAsync(project).ConfigureAwait(false);
        }

        return [.. result];

        async Task DescendInheritanceTreeInProjectAsync(Project project)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Debug.Assert(project.SupportsCompilation);

            // First see what derived metadata types we might find in this project. This is only necessary if we
            // started with a metadata type (i.e. a source type could not have a descendant type found in metadata,
            // but a metadata type could have descendant types in source and metadata).
            if (searchInMetadata)
            {
                using var _ = GetSymbolSet(out var tempBuffer);

                await AddDescendantMetadataTypesInProjectAsync(tempBuffer, project).ConfigureAwait(false);

                // Add all the matches we found to the result set.
                AssertContents(tempBuffer, assert: s_isInMetadata, "Found type was not from metadata");
                AddRange(tempBuffer, result);

                // Now, if we're doing a transitive search, add these found types to the 'current' sets we're
                // searching for more results for. These will then be used when searching for more types in the next
                // project (which our caller controls).
                if (transitive)
                {
                    AddRange(tempBuffer, currentMetadataTypes, shouldContinueSearching);
                    AddRange(tempBuffer, currentSourceAndMetadataTypes, shouldContinueSearching);
                }
            }

            {
                using var _ = GetSymbolSet(out var tempBuffer);

                // Now search the project and see what source types we can find.
                await AddDescendantSourceTypesInProjectAsync(tempBuffer, project).ConfigureAwait(false);

                // Add all the matches we found to the result set.
                AssertContents(tempBuffer, assert: s_isInSource, "Found type was not from source");
                AddRange(tempBuffer, result);

                // Now, if we're doing a transitive search, add these types to the currentSourceAndMetadataTypes
                // set. These will then be used when searching for more types in the next project (which our caller
                // controls).  We don't have to add this to currentMetadataTypes since these will all be
                // source types.
                if (transitive)
                    AddRange(tempBuffer, currentSourceAndMetadataTypes, shouldContinueSearching);
            }
        }

        async Task AddDescendantSourceTypesInProjectAsync(SymbolSet result, Project project)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // We're going to be sweeping over this project over and over until we reach a 
            // fixed point.  In order to limit GC and excess work, we cache all the semantic
            // models and DeclaredSymbolInfo for the documents we look at.
            // Because we're only processing a project at a time, this is not an issue.
            var cachedModels = new ConcurrentSet<SemanticModel>();

            using var _1 = GetSymbolSet(out var typesToSearchFor);
            using var _2 = GetSymbolSet(out var tempBuffer);

            typesToSearchFor.AddAll(currentSourceAndMetadataTypes);

            var projectIndex = await ProjectIndex.GetIndexAsync(project, cancellationToken).ConfigureAwait(false);

            // As long as there are new types to search for, keep looping.
            while (typesToSearchFor.Count > 0)
            {
                foreach (var type in typesToSearchFor)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    switch (type.SpecialType)
                    {
                        case SpecialType.System_Object:
                            await AddMatchingTypesAsync(
                                projectIndex.ClassesAndRecordsThatMayDeriveFromSystemObject,
                                tempBuffer,
                                predicate: static n => n.BaseType?.SpecialType == SpecialType.System_Object).ConfigureAwait(false);
                            break;
                        case SpecialType.System_ValueType:
                            await AddMatchingTypesAsync(
                                projectIndex.ValueTypes, tempBuffer, predicate: null).ConfigureAwait(false);
                            break;
                        case SpecialType.System_Enum:
                            await AddMatchingTypesAsync(
                                projectIndex.Enums, tempBuffer, predicate: null).ConfigureAwait(false);
                            break;
                        case SpecialType.System_MulticastDelegate:
                            await AddMatchingTypesAsync(
                                projectIndex.Delegates, tempBuffer, predicate: null).ConfigureAwait(false);
                            break;
                    }

                    await AddSourceTypesThatDeriveFromNameAsync(tempBuffer, type.Name).ConfigureAwait(false);
                }

                PropagateTemporaryResults(
                    result, typesToSearchFor, tempBuffer, transitive, shouldContinueSearching);
            }

            async ValueTask<SemanticModel> GetRequiredSemanticModelAsync(DocumentId documentId)
            {
                var document = await solution.GetRequiredDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
                var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                cachedModels.Add(semanticModel);

                return semanticModel;
            }

            async Task AddMatchingTypesAsync(
                MultiDictionary<DocumentId, DeclaredSymbolInfo> documentToInfos,
                SymbolSet result,
                Func<INamedTypeSymbol, bool>? predicate)
            {
                foreach (var (documentId, infos) in documentToInfos)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Debug.Assert(infos.Count > 0);

                    var semanticModel = await GetRequiredSemanticModelAsync(documentId).ConfigureAwait(false);
                    foreach (var info in infos)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (info.TryResolve(semanticModel, cancellationToken) is INamedTypeSymbol namedType &&
                            predicate?.Invoke(namedType) != false)
                        {
                            result.Add(namedType);
                        }
                    }
                }
            }

            async Task AddSourceTypesThatDeriveFromNameAsync(SymbolSet result, string name)
            {
                foreach (var (documentId, info) in projectIndex.NamedTypes[name])
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var semanticModel = await GetRequiredSemanticModelAsync(documentId).ConfigureAwait(false);
                    if (info.TryResolve(semanticModel, cancellationToken) is INamedTypeSymbol namedType &&
                        typeMatches(namedType, typesToSearchFor))
                    {
                        result.Add(namedType);
                    }
                }
            }
        }

        async Task AddDescendantMetadataTypesInProjectAsync(SymbolSet result, Project project)
        {
            Debug.Assert(project.SupportsCompilation);

            if (currentMetadataTypes.Count == 0)
                return;

            var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

            using var _1 = GetSymbolSet(out var typesToSearchFor);
            using var _2 = GetSymbolSet(out var tempBuffer);

            typesToSearchFor.AddAll(currentMetadataTypes);

            // As long as there are new types to search for, keep looping.
            while (typesToSearchFor.Count > 0)
            {
                foreach (var reference in compilation.References)
                {
                    if (reference is not PortableExecutableReference peReference)
                        continue;

                    // Don't look inside this reference if we already looked inside it in another project.
                    if (seenPEReferences.Contains(peReference))
                        continue;

                    cancellationToken.ThrowIfCancellationRequested();

                    await AddMatchingMetadataTypesInMetadataReferenceAsync(
                        typesToSearchFor, project, compilation, peReference, tempBuffer).ConfigureAwait(false);
                }

                PropagateTemporaryResults(
                    result, typesToSearchFor, tempBuffer, transitive, shouldContinueSearching);
            }

            // Mark all these references as having been seen.  We don't need to examine it in future projects.
            seenPEReferences.AddRange(compilation.References.OfType<PortableExecutableReference>());
        }

        async Task AddMatchingMetadataTypesInMetadataReferenceAsync(
            SymbolSet metadataTypes,
            Project project,
            Compilation compilation,
            PortableExecutableReference reference,
            SymbolSet result)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // We store an index in SymbolTreeInfo of the *simple* metadata type name to the names of the all the types
            // that either immediately derive or implement that type.  Because the mapping is from the simple name we
            // might get false positives.  But that's fine as we still use 'tpeMatches' to make sure the match is
            // correct.
            var symbolTreeInfo = await SymbolTreeInfo.GetInfoForMetadataReferenceAsync(
                project.Solution, reference, checksum: null, cancellationToken).ConfigureAwait(false);

            Contract.ThrowIfNull(symbolTreeInfo);

            // For each type we care about, see if we can find any derived types
            // in this index.
            foreach (var metadataType in metadataTypes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var baseTypeName = metadataType.Name;

                // For each derived type we find, see if we can map that back 
                // to an actual symbol.  Then check if that symbol actually fits
                // our criteria.
                foreach (var derivedType in symbolTreeInfo.GetDerivedMetadataTypes(baseTypeName, compilation, cancellationToken))
                {
                    if (derivedType != null &&
                        derivedType.Locations.Any(s_isInMetadata) &&
                        typeMatches(derivedType, metadataTypes))
                    {
                        result.Add(derivedType);
                    }
                }
            }
        }
    }

    [Conditional("DEBUG")]
    private static void AssertContents(
        SymbolSet foundTypes, Func<Location, bool> assert, string message)
    {
        foreach (var type in foundTypes)
            Debug.Assert(type.Locations.All(assert), message);
    }

    private static void AddRange(SymbolSet foundTypes, SymbolSet result)
    {
        // Directly enumerate to avoid IEnumerator allocations.
        foreach (var type in foundTypes)
            result.Add(type);
    }

    private static void AddRange(SymbolSet foundTypes, SymbolSet currentTypes, Func<INamedTypeSymbol, bool> shouldContinueSearching)
    {
        // Directly enumerate to avoid IEnumerator allocations.
        foreach (var type in foundTypes)
        {
            if (shouldContinueSearching(type))
                currentTypes.Add(type);
        }
    }

    private static async Task<ISet<ProjectId>> GetProjectsThatCouldReferenceTypeAsync(
        INamedTypeSymbol type,
        Solution solution,
        bool searchInMetadata,
        CancellationToken cancellationToken)
    {
        var dependencyGraph = solution.GetProjectDependencyGraph();

        if (searchInMetadata)
        {
            // For a metadata type, find all projects that refer to the metadata assembly that
            // the type is defined in.  Note: we pass 'null' for projects intentionally.  We
            // Need to find all the possible projects that contain this metadata.
            var projectsThatReferenceMetadataAssembly =
                await DependentProjectsFinder.GetDependentProjectsAsync(
                    solution, [type], solution.Projects.ToImmutableHashSet(), cancellationToken).ConfigureAwait(false);

            // Now collect all the dependent projects as well.
            var projectsThatCouldReferenceType =
                projectsThatReferenceMetadataAssembly.SelectMany(
                    p => GetProjectsThatCouldReferenceType(dependencyGraph, p)).ToSet();

            return projectsThatCouldReferenceType;
        }
        else
        {
            // For a source project, find the project that that type was defined in.
            var sourceProject =
                solution.GetProject(type.ContainingAssembly, cancellationToken) ??
                solution.GetOriginatingProject(type.ContainingAssembly);
            if (sourceProject == null)
                return SpecializedCollections.EmptySet<ProjectId>();

            // Now find all the dependent of those projects.
            var projectsThatCouldReferenceType = GetProjectsThatCouldReferenceType(
                dependencyGraph, sourceProject).ToSet();

            return projectsThatCouldReferenceType;
        }
    }

    private static IEnumerable<ProjectId> GetProjectsThatCouldReferenceType(
        ProjectDependencyGraph dependencyGraph, Project project)
    {
        // Get all the projects that depend on 'project' as well as 'project' itself.
        return dependencyGraph.GetProjectsThatTransitivelyDependOnThisProject(project.Id)
                              .Concat(project.Id);
    }

    private static ImmutableArray<Project> GetOrderedProjectsToExamine(
        Solution solution,
        IImmutableSet<Project> projects,
        IEnumerable<ProjectId> projectsThatCouldReferenceType)
    {
        var projectsToExamine = GetProjectsToExamineWorker(
            solution, projects, projectsThatCouldReferenceType);

        // Ensure the projects we're going to examine are ordered topologically.
        // That way we can just sweep over them from left to right as no project
        // could affect a previous project in the sweep.
        return OrderTopologically(solution, projectsToExamine);
    }

    private static ImmutableArray<Project> OrderTopologically(
        Solution solution, IEnumerable<Project> projectsToExamine)
    {
        var order = new Dictionary<ProjectId, int>(capacity: solution.ProjectIds.Count);

        var index = 0;

        var dependencyGraph = solution.GetProjectDependencyGraph();
        foreach (var projectId in dependencyGraph.GetTopologicallySortedProjects())
        {
            order.Add(projectId, index);
            index++;
        }

        return [.. projectsToExamine.OrderBy((p1, p2) => order[p1.Id] - order[p2.Id])];
    }

    private static ImmutableArray<Project> GetProjectsToExamineWorker(
        Solution solution,
        IImmutableSet<Project> projects,
        IEnumerable<ProjectId> projectsThatCouldReferenceType)
    {
        var dependencyGraph = solution.GetProjectDependencyGraph();

        // Take the projects that were passed in, and find all the projects that 
        // they depend on (including themselves).  i.e. if we have a solution that
        // looks like:
        //      A <- B <- C <- D
        //          /
        //         └
        //        E
        // and we're passed in 'B, C, E' as the project to search, then this set 
        // will be A, B, C, E.
        var allProjectsThatTheseProjectsDependOn = projects
            .SelectMany(p => dependencyGraph.GetProjectsThatThisProjectTransitivelyDependsOn(p.Id))
            .Concat(projects.Select(p => p.Id)).ToSet();

        // We then intersect this set with the actual set of projects that could reference
        // the type.  Say this list is B, C, D.  The intersection of this list and the above
        // one will then be 'B' and 'C'.  
        //
        // In other words, there is no point searching A and E (because they can't even 
        // reference the type).  And there's no point searching 'D' because it can't contribute
        // any information that would affect the result in the projects we are asked to search
        // within.

        // Finally, because we're searching metadata and source symbols, this needs to be a project
        // that actually supports compilations.
        return projectsThatCouldReferenceType.Intersect(allProjectsThatTheseProjectsDependOn)
                                             .Select(solution.GetRequiredProject)
                                             .ToImmutableArray();
    }

    private static bool TypeHasBaseTypeInSet(INamedTypeSymbol type, SymbolSet set)
    {
        var baseType = type.BaseType?.OriginalDefinition;
        return baseType != null && set.Contains(baseType);
    }

    private static bool TypeHasInterfaceInSet(INamedTypeSymbol type, SymbolSet set)
    {
        foreach (var interfaceType in type.Interfaces)
        {
            if (set.Contains(interfaceType.OriginalDefinition))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Moves all the types in <paramref name="tempBuffer"/> to <paramref name="result"/>.  If these are types we
    /// haven't seen before, and the caller says we <paramref name="shouldContinueSearching"/> on them, then add
    /// them to <paramref name="typesToSearchFor"/> for the next round of searching.
    /// </summary>
    private static void PropagateTemporaryResults(
        SymbolSet result,
        SymbolSet typesToSearchFor,
        SymbolSet tempBuffer,
        bool transitive,
        Func<INamedTypeSymbol, bool> shouldContinueSearching)
    {
        // Clear out the information about the types we're looking for.  We'll
        // fill these in if we discover any more types that we need to keep searching
        // for.
        typesToSearchFor.Clear();

        foreach (var derivedType in tempBuffer)
        {
            // See if it's a type we've never seen before.
            if (result.Add(derivedType))
            {
                // If we should keep going, add it to the next batch of items we'll search for in this project.
                if (transitive && shouldContinueSearching(derivedType))
                    typesToSearchFor.Add(derivedType);
            }
        }

        tempBuffer.Clear();
    }

    public static PooledDisposer<PooledHashSet<INamedTypeSymbol>> GetSymbolSet(out SymbolSet instance)
    {
        var pooledInstance = s_symbolSetPool.Allocate();
        Debug.Assert(pooledInstance.Count == 0);
        instance = pooledInstance;
        return new PooledDisposer<PooledHashSet<INamedTypeSymbol>>(pooledInstance);
    }
}
