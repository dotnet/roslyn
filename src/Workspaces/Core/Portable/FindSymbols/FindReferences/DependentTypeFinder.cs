// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    using TypeMatches = Func<INamedTypeSymbol, Project, CancellationToken, bool>;
    using SourceInfoMatches = Func<Document, DeclaredSymbolInfo, ConcurrentSet<SemanticModel>, CancellationToken, Task<ISymbol>>;
    internal delegate Task SearchProjectAsync(
        INamedTypeSymbol type, Project project, bool isInMetadata,
        ConcurrentSet<ISymbol> results, ConcurrentSet<SemanticModel> cachedModels, 
        CancellationToken cancellationToken);

    internal delegate Task<IEnumerable<INamedTypeSymbol>> SearchDocumentAsync(
        HashSet<INamedTypeSymbol> classesToSearchFor,
        HashSet<string> classNamesToSearchFor,
        Document document,
        HashSet<SemanticModel> cachedModels,
        HashSet<DeclaredSymbolInfo> cachedInfos,
        CancellationToken cancellationToken);

    internal delegate Task FindTypesCallbackAsync(bool searchInMetadata, HashSet<INamedTypeSymbol> result, HashSet<INamedTypeSymbol> currentMetadataTypes, HashSet<INamedTypeSymbol> currentSourceAndMetadataTypes, Project project, CancellationToken cancellationToken);
    internal delegate Task<IEnumerable<INamedTypeSymbol>> SearchProject(HashSet<INamedTypeSymbol> sourceAndMetadataTypes, Project project, CancellationToken cancellationToken);

    /// <summary>
    /// Provides helper methods for finding dependent types (derivations, implementations, etc.) across a solution.
    /// </summary>
    /// <remarks>
    /// This type makes heavy use of <see cref="ConditionalWeakTable{TKey, TValue}"/> for caching purposes. When
    /// modifying these caches, care must be taken to avoid introducing memory leaks. Instances of <see cref="Compilation"/>
    /// are used as the keys in these caches, so in general only symbols or other data from that same compilation should be stored
    /// in the associated value.
    /// </remarks>
    internal static class DependentTypeFinder
    {
        // Cache certain delegates so we don't need to create them over and over again.
        private static SourceInfoMatches s_infoMatchesDelegate = GetSourceInfoMatchesFunction(DeclaredSymbolInfoKind.Delegate);
        private static SourceInfoMatches s_infoMatchesEnum = GetSourceInfoMatchesFunction(DeclaredSymbolInfoKind.Enum);
        private static SourceInfoMatches s_infoMatchesStruct = GetSourceInfoMatchesFunction(DeclaredSymbolInfoKind.Struct);

        private static TypeMatches s_isDelegateType = (t, p, c) => t.IsDelegateType();
        private static TypeMatches s_isEnumType = (t, p, c) => t.IsEnumType();
        private static TypeMatches s_isStructType = (t, p, c) => t.IsStructType();

        private static TypeMatches s_derivesFromObject = (t, p, c) => t.BaseType?.SpecialType == SpecialType.System_Object;

        private static Func<Location, bool> s_isInMetadata = loc => loc.IsInMetadata;

        /// <summary>
        /// For a given <see cref="Compilation"/>, stores a flat list of all the accessible metadata types
        /// within the compilation.
        /// </summary>
        private static readonly ConditionalWeakTable<Compilation, List<INamedTypeSymbol>> s_compilationAllAccessibleMetadataTypesTable =
            new ConditionalWeakTable<Compilation, List<INamedTypeSymbol>>();

        /// <summary>
        /// This is an internal implementation of <see cref="SymbolFinder.FindDerivedClassesAsync"/>, which is a publically callable method.
        /// </summary>
        public static Task<IEnumerable<INamedTypeSymbol>> FindDerivedClassesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            if (type?.TypeKind != TypeKind.Class ||
                type.IsSealed)
            {
                return SpecializedTasks.EmptyEnumerable<INamedTypeSymbol>();
            }

            return FindTypesAsync(type, solution, projects, 
                findTypesCallback: FindDerivedClassesInProjectAsync,
                cancellationToken: cancellationToken);
        }

        public static async Task<IEnumerable<INamedTypeSymbol>> FindImplementingTypesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            // Only an interface can be implemented.
            if (type?.TypeKind != TypeKind.Interface)
            {
                return SpecializedCollections.EmptyEnumerable<INamedTypeSymbol>();
            }

            var derivedAndImplementingTypes = await FindTypesAsync(type, solution, projects,
                findTypesCallback: FindDerivedAndImplementingTypesInProjectAsync,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            // Because of how we search, we're going to find both derived interfaces and 
            // implementing classes. We need to find the derived interfaces so we can find all 
            // possible derived classes.  But we don't want to return derived interfaces in
            // our result.
            return derivedAndImplementingTypes.Where(t => t.TypeKind == TypeKind.Class).ToList();

#if false
            var cachedModels = new ConcurrentSet<SemanticModel>();

            // Search outwards by looking for immediately implementing classes of the interface 
            // passed in, as well as immediately derived interfaces of the interface passed
            // in.
            //
            // For every implementing class we find, find all derived classes of that class as well.
            // After all, if the base class implements the interface, then all derived classes
            // do as well.
            //
            // For every extending interface we find, we also recurse and do this search as well.
            // After all, if a type implements a derived inteface then it certainly implements
            // the base interface as well.
            //
            // We do this by keeping a queue of interface types to search for (starting with the 
            // initial interface we were given).  As long as we keep discovering new types, we
            // keep searching outwards.  Once no new types are discovered, we're done.
            var finalResult = new HashSet<INamedTypeSymbol>(SymbolEquivalenceComparer.Instance);

            var interfaceQueue = new HashSet<INamedTypeSymbol>(SymbolEquivalenceComparer.Instance);
            interfaceQueue.Add(type);

            var classQueue = new HashSet<INamedTypeSymbol>(SymbolEquivalenceComparer.Instance);

            while (interfaceQueue.Count > 0 || classQueue.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Search for all the workqueue types in parallel.
                if (interfaceQueue.Count > 0)
                {
                    var interfaceTasks = interfaceQueue.Select(t => GetTypesImmediatelyDerivedFromInterfacesAsync(
                        t, solution, projects, cachedModels, cancellationToken)).ToArray();
                    await Task.WhenAll(interfaceTasks).ConfigureAwait(false);

                    interfaceQueue.Clear();
                    foreach (var task in interfaceTasks)
                    {
                        foreach (var derivedType in task.Result)
                        {
                            if (finalResult.Add(derivedType))
                            {
                                // Seeing this type for the first time.  If it is a class or
                                // interface, then add it to the list of symbols to keep 
                                // searching for.  If it's an interface, keep looking for 
                                // derived types of that interface.  If it's a class, just add
                                // in the entire derived class chain of that class.
                                if (derivedType.TypeKind == TypeKind.Interface)
                                {
                                    interfaceQueue.Add(derivedType);
                                }
                                else if (derivedType.TypeKind == TypeKind.Class)
                                {
                                    classQueue.Add(derivedType);
                                }
                            }
                        }
                    }
                }
                else
                {
                    Debug.Assert(classQueue.Count > 0);

                    var firstClass = classQueue.First();
                    classQueue.Remove(firstClass);

                    var derivedClasses = await FindDerivedClassesAsync(
                        firstClass, solution, projects, cachedModels, cancellationToken).ConfigureAwait(false);

                    finalResult.AddRange(derivedClasses);

                    // It's possible that one of the derived classes we discovered was also in 
                    // classQueue. Remove them so we don't do excess work finding types we've
                    // already found.
                    classQueue.RemoveAll(derivedClasses);
                }
            }

            return finalResult;
#endif
        }

        private static async Task<IEnumerable<INamedTypeSymbol>> FindTypesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            FindTypesCallbackAsync findTypesCallback,
            CancellationToken cancellationToken)
        {
            type = type.OriginalDefinition;
            projects = projects ?? ImmutableHashSet.Create(solution.Projects.ToArray());
            var searchInMetadata = type.Locations.Any(loc => loc.IsInMetadata);

            // Note: it is not sufficient to just walk the list of projects passed in,
            // searching only those for derived types.
            //
            // Say we have projects: A <- B <- C, but only projects A and C are passed in.
            // We might miss a derived type in C if there's an intermediate derived type
            // in B.
            //
            // However, say we have projects A <- B <- C <- D, only only projects A and C
            // are passed in.  There is no need to check D as there's no way it could
            // contribute an intermediate type that affects A or C.  We only need to check
            // A, B and C

            var dependencyGraph = solution.GetProjectDependencyGraph();

            // First find all the projects that could potentially reference this type.
            var projectsThatCouldReferenceType = await GetProjectsThatCouldReferenceTypeAsync(
                type, solution, searchInMetadata, cancellationToken).ConfigureAwait(false);

            // Now, based on the list of projects that could actually reference the type,
            // and the list of projects the caller wants to search, find the actual list of
            // projects we need to search through.
            //
            // This list of projects is properly topologicaly ordered.  Because of this we
            // can just process them in order from first to last because we know no project
            // in this list could affect a prior project.
            var orderedProjectsToExamine = GetOrderedProjectsToExamine(
                solution, projects, dependencyGraph, projectsThatCouldReferenceType);

            // Now walk the projects from left to right seeing what our type cascades to. Once we 
            // reach a fixed point in that project, take all the types we've found and move to the
            // next project.  Continue this until we've exhausted all projects.
            //
            // Because there is a data-dependency between the projects, we cannot process them in
            // parallel.  (Processing linearly is also probably preferable to limit the amount of
            // cache churn we could cause creating all those compilations.

            var currentMetadataTypes = new HashSet<INamedTypeSymbol>(SymbolEquivalenceComparer.Instance);
            var currentSourceAndMetadataTypes = new HashSet<INamedTypeSymbol>(SymbolEquivalenceComparer.Instance);

            currentSourceAndMetadataTypes.Add(type);
            if (searchInMetadata)
            {
                currentMetadataTypes.Add(type);
            }

            var result = new HashSet<INamedTypeSymbol>(SymbolEquivalenceComparer.Instance);

            // There is a data dependency when searching projects.  Namely, the derived types we
            // find in one project will be used when searching successive projects.  So we cannot
            // searchthese projects in parallel.
            foreach (var project in orderedProjectsToExamine)
            {
                await findTypesCallback(
                    searchInMetadata, result,
                    currentMetadataTypes, currentSourceAndMetadataTypes,
                    project, cancellationToken).ConfigureAwait(false);
            }

            return result;

        }

        private static Task FindDerivedClassesInProjectAsync(
            bool searchInMetadata,
            HashSet<INamedTypeSymbol> result,
            HashSet<INamedTypeSymbol> currentMetadataTypes,
            HashSet<INamedTypeSymbol> currentSourceAndMetadataTypes,
            Project project,
            CancellationToken cancellationToken)
        {
            return FindTypesInProjectAsync(searchInMetadata, result,
                currentMetadataTypes, currentSourceAndMetadataTypes,
                project,
                findMetadataTypesAsync: FindDerivedMetadataClassesInProjectAsync,
                findSourceTypesAsync: FindDerivedSourceClassesInProjectAsync,
                cancellationToken: cancellationToken);
        }

        private static Task FindDerivedAndImplementingTypesInProjectAsync(
            bool searchInMetadata,
            HashSet<INamedTypeSymbol> result,
            HashSet<INamedTypeSymbol> currentMetadataTypes,
            HashSet<INamedTypeSymbol> currentSourceAndMetadataTypes,
            Project project,
            CancellationToken cancellationToken)
        {
            return FindTypesInProjectAsync(searchInMetadata, result,
                currentMetadataTypes, currentSourceAndMetadataTypes,
                project,
                findMetadataTypesAsync: FindDerivedAndImplementingMetadataTypesInProjectAsync,
                findSourceTypesAsync: FindDerivedAndImplementingSourceTypesInProjectAsync,
                cancellationToken: cancellationToken);
        }

        private static async Task FindTypesInProjectAsync(
            bool searchInMetadata,
            HashSet<INamedTypeSymbol> result,
            HashSet<INamedTypeSymbol> currentMetadataTypes,
            HashSet<INamedTypeSymbol> currentSourceAndMetadataTypes,
            Project project,
            SearchProject findMetadataTypesAsync,
            SearchProject findSourceTypesAsync,
            CancellationToken cancellationToken)
        {
            // First see what derived metadata types we might find in this project.
            // This is only necessary if we started with a metadata type.
            if (searchInMetadata)
            {
                var foundMetadataTypes = await findMetadataTypesAsync(
                    currentMetadataTypes, project, cancellationToken).ConfigureAwait(false);

                foreach (var foundType in foundMetadataTypes)
                {
                    Debug.Assert(foundType.Locations.Any(loc => loc.IsInMetadata));

                    // Add to the result list.
                    result.Add(foundType);

                    // If the derived type isn't sealed, then also add it to the list of types
                    // to search for in subsequent projects.
                    if (!foundType.IsSealed)
                    {
                        currentMetadataTypes.Add(foundType);
                        currentSourceAndMetadataTypes.Add(foundType);
                    }
                }
            }

            // Now search the project and see what source types we can find.
            var foundSourceTypes = await findSourceTypesAsync(
                currentSourceAndMetadataTypes, project, cancellationToken).ConfigureAwait(false);

            foreach (var foundType in foundSourceTypes)
            {
                Debug.Assert(foundType.Locations.All(loc => loc.IsInSource));

                // Add to the result list.
                result.Add(foundType);

                // If the derived type isn't sealed, then also add it to the list of types
                // to search for in subsequent projects.
                if (!foundType.IsSealed)
                {
                    currentSourceAndMetadataTypes.Add(foundType);
                }
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
                        type, solution, projects: null, cancellationToken: cancellationToken).ConfigureAwait(false);

                // Now collect all the dependent projects as well.
                var projectsThatCouldReferenceType =
                    projectsThatReferenceMetadataAssembly.SelectMany(
                        p => GetProjectsThatCouldReferenceType(dependencyGraph, p)).ToSet();

                return projectsThatCouldReferenceType;
            }
            else
            {
                // For a source project, find the project that that type was defined in.
                var sourceProject = solution.GetProject(type.ContainingAssembly);
                if (sourceProject == null)
                {
                    return SpecializedCollections.EmptySet<ProjectId>();
                }

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

        private static List<Project> GetOrderedProjectsToExamine(
            Solution solution,
            IImmutableSet<Project> projects,
            ProjectDependencyGraph dependencyGraph,
            IEnumerable<ProjectId> projectsThatCouldReferenceType)
        {
            var projectsToExamine = GetProjectsToExamineWorker(
                solution, projects, dependencyGraph, projectsThatCouldReferenceType);

            // Ensure the projects we're going to examine are ordered topologically.
            // That way we can just sweep over them from left to right as no project
            // could affect a previous project in the sweep.
            return OrderTopologically(dependencyGraph, projectsToExamine);
        }

        private static List<Project> OrderTopologically(
            ProjectDependencyGraph dependencyGraph, IEnumerable<Project> projectsToExamine)
        {
            var order = new Dictionary<ProjectId, int>();

            int index = 0;
            foreach (var projectId in dependencyGraph.GetTopologicallySortedProjects())
            {
                order.Add(projectId, index);
                index++;
            }

            return projectsToExamine.OrderBy((p1, p2) => order[p1.Id] - order[p1.Id]).ToList();
        }

        private static IEnumerable<Project> GetProjectsToExamineWorker(
            Solution solution,
            IImmutableSet<Project> projects,
            ProjectDependencyGraph dependencyGraph,
            IEnumerable<ProjectId> projectsThatCouldReferenceType)
        {
            // We need to search all projects that could reference the type *and* which are 
            // referenced by some project in the project set.
            foreach (var projectThatCouldReferenceType in projectsThatCouldReferenceType)
            {
                if (projects.Any(p => p.Id == projectThatCouldReferenceType))
                {
                    // We were explicitly asked to search this project, and it's a project that 
                    // could be referencing the type we care about.  Definitely search this one.
                    yield return solution.GetProject(projectThatCouldReferenceType);
                }
                else if (AnyProjectDependsOn(projects, dependencyGraph, projectThatCouldReferenceType))
                {
                    // While we were not explicitly asked to search 'projectThatCouldReferenceType',
                    // we do have some project we care about that depends on that project.  Because of
                    // this we need to search 'projectThatCouldReferenceType' in case it introduces 
                    // intermediate types that affect the projects we care about.
                    yield return solution.GetProject(projectThatCouldReferenceType);
                }
            }

            // No point searching a project if it wasn't a project that could have referenced that
            // type in the first place.
        }

        private static bool AnyProjectDependsOn(
            IImmutableSet<Project> projects, ProjectDependencyGraph dependencyGraph, 
            ProjectId projectThatCouldReferenceType)
        {
            foreach (var project in projects)
            {
                var projectsThisProjectDependsOn = dependencyGraph.GetProjectsThatThisProjectTransitivelyDependsOn(project.Id);
                if (projectsThisProjectDependsOn.Contains(projectThatCouldReferenceType))
                {
                    return true;
                }
            }

            return false;
        }

        private static async Task<IEnumerable<INamedTypeSymbol>> FindDerivedMetadataClassesInProjectAsync(
            HashSet<INamedTypeSymbol> metadataTypes,
            Project project,
            CancellationToken cancellationToken)
        {
            if (metadataTypes.Count == 0)
            {
                return SpecializedCollections.EmptyEnumerable<INamedTypeSymbol>();
            }

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var typesInCompilation = GetAllAccessibleMetadataTypesInCompilation(compilation, cancellationToken);

            var result = new HashSet<INamedTypeSymbol>(SymbolEquivalenceComparer.Instance);

            foreach (var type in typesInCompilation)
            {
                if (TypeDerivesFrom(metadataTypes, type))
                {
                    result.Add(type);
                }
            }

            return result;
        }

        private static bool TypeDerivesFrom(
            HashSet<INamedTypeSymbol> metadataTypes, INamedTypeSymbol type)
        {
            for (var current = type.BaseType; current != null; current = current.BaseType)
            {
                if (metadataTypes.Contains(current.OriginalDefinition))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TypeImplementsFrom(
            HashSet<INamedTypeSymbol> metadataTypes, INamedTypeSymbol type)
        {
            foreach (var interfaceType in type.AllInterfaces)
            {
                if (metadataTypes.Contains(interfaceType.OriginalDefinition))
                {
                    return true;
                }
            }

            return false;
        }

        private static async Task<IEnumerable<INamedTypeSymbol>> FindDerivedAndImplementingMetadataTypesInProjectAsync(
            HashSet<INamedTypeSymbol> metadataTypes,
            Project project,
            CancellationToken cancellationToken)
        {
            if (metadataTypes.Count == 0)
            {
                return SpecializedCollections.EmptyEnumerable<INamedTypeSymbol>();
            }

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var typesInCompilation = GetAllAccessibleMetadataTypesInCompilation(compilation, cancellationToken);

            var result = new HashSet<INamedTypeSymbol>(SymbolEquivalenceComparer.Instance);

            foreach (var type in typesInCompilation)
            {
                if (TypeDerivesFrom(metadataTypes, type) ||
                    TypeImplementsFrom(metadataTypes, type))
                {
                    result.Add(type);
                }
            }

            return result;
        }

        private static Task<IEnumerable<INamedTypeSymbol>> FindDerivedAndImplementingSourceTypesInProjectAsync(
            HashSet<INamedTypeSymbol> sourceAndMetadataTypes,
            Project project,
            CancellationToken cancellationToken)
        {
            // Found a new type.  If its an interface or a non-sealed class, add it to the list of 
            // types for us to continue searching for.
            Func<INamedTypeSymbol, bool> shouldContinueSearching = 
                t => t.TypeKind == TypeKind.Interface || (t.TypeKind == TypeKind.Class && !t.IsSealed);

            return FindTypesInProjectAsync(
                sourceAndMetadataTypes,
                project,
                findTypesInDocumentAsync: FindDerivedAndImplementingTypesInDocumentAsync,
                shouldContinueSearching: shouldContinueSearching,
                cancellationToken: cancellationToken);
        }

        private static Task<IEnumerable<INamedTypeSymbol>> FindDerivedSourceClassesInProjectAsync(
            HashSet<INamedTypeSymbol> sourceAndMetadataTypes,
            Project project,
            CancellationToken cancellationToken)
        {
            Debug.Assert(sourceAndMetadataTypes.All(c => c.TypeKind == TypeKind.Class));
            Debug.Assert(sourceAndMetadataTypes.All(c => !c.IsSealed));

            // Found a new type.  If it isn't sealed, add it to the list of 
            // types for us to search for more derived classes of.
            Func<INamedTypeSymbol, bool> shouldContinueSearching = t => !t.IsSealed;

            return FindTypesInProjectAsync(
                sourceAndMetadataTypes,
                project,
                findTypesInDocumentAsync: FindDerivedClassesInDocumentAsync,
                shouldContinueSearching: shouldContinueSearching,
                cancellationToken: cancellationToken);
        }

        private static async Task<IEnumerable<INamedTypeSymbol>> FindTypesInProjectAsync(
            HashSet<INamedTypeSymbol> sourceAndMetadataTypes,
            Project project,
            SearchDocumentAsync findTypesInDocumentAsync,
            Func<INamedTypeSymbol, bool> shouldContinueSearching,
            CancellationToken cancellationToken)
        {
            // We're going to be sweeping over this project over and over until we reach a 
            // fixed point.  In order to limit GC and excess work, we cache all the sematic
            // models and DeclaredSymbolInfo for hte documents we look at.
            // Because we're only processing a project at a time, this is not an issue.
            var cachedModels = new HashSet<SemanticModel>();
            var cachedInfos = new HashSet<DeclaredSymbolInfo>();

            var finalResult = new HashSet<INamedTypeSymbol>(SymbolEquivalenceComparer.Instance);

            var typesToSearchFor = new HashSet<INamedTypeSymbol>(SymbolEquivalenceComparer.Instance);
            typesToSearchFor.AddAll(sourceAndMetadataTypes);

            var typeNamesToSearchFor = new HashSet<string>();

            while (typesToSearchFor.Count > 0)
            {
                // Only bother searching for derived types of non-sealed classes we're passed in.
                typeNamesToSearchFor.AddRange(typesToSearchFor.Select(c => c.Name));

                // Search all the documents of this project in parallel.
                var tasks = project.Documents.Select(d => findTypesInDocumentAsync(
                    typesToSearchFor, typeNamesToSearchFor, d, cachedModels, cachedInfos, cancellationToken)).ToArray();
                await Task.WhenAll(tasks).ConfigureAwait(false);

                typesToSearchFor.Clear();
                typeNamesToSearchFor.Clear();

                foreach (var task in tasks)
                {
                    if (task.Result != null)
                    {
                        foreach (var derivedType in task.Result)
                        {
                            if (finalResult.Add(derivedType))
                            {
                                if (shouldContinueSearching(derivedType))
                                {
                                    typesToSearchFor.Add(derivedType);
                                }
                            }
                        }
                    }
                }
            }

            return finalResult;
        }

        private static async Task<IEnumerable<INamedTypeSymbol>> FindImplementingSourceTypesInProjectAsync(
            HashSet<INamedTypeSymbol> sourceAndMetadataTypes,
            Project project,
            CancellationToken cancellationToken)
        {
            Debug.Assert(sourceAndMetadataTypes.All(c => c.TypeKind == TypeKind.Interface));

            // We're going to be sweeping over this project over and over until we reach a 
            // fixed point.  In order to limit GC and excess work, we cache all the sematic
            // models and DeclaredSymbolInfo for hte documents we look at.
            // Because we're only processing a project at a time, this is not an issue.
            var cachedModels = new HashSet<SemanticModel>();
            var cachedInfos = new HashSet<DeclaredSymbolInfo>();

            var finalResult = new HashSet<INamedTypeSymbol>(SymbolEquivalenceComparer.Instance);

            var classesToSearchFor = new HashSet<INamedTypeSymbol>(SymbolEquivalenceComparer.Instance);
            classesToSearchFor.AddAll(sourceAndMetadataTypes);

            var classNamesToSearchFor = new HashSet<string>();

            while (classesToSearchFor.Count > 0)
            {
                // Only bother searching for derived types of non-sealed classes we're passed in.
                classNamesToSearchFor.AddRange(classesToSearchFor.Select(c => c.Name));

                // Search all the documents of this project in parallel.
                var tasks = project.Documents.Select(d => FindDerivedClassesInDocumentAsync(
                    classesToSearchFor, classNamesToSearchFor, d, cachedModels, cachedInfos, cancellationToken)).ToArray();
                await Task.WhenAll(tasks).ConfigureAwait(false);

                classesToSearchFor.Clear();
                classNamesToSearchFor.Clear();

                foreach (var task in tasks)
                {
                    if (task.Result != null)
                    {
                        foreach (var derivedType in task.Result)
                        {
                            if (finalResult.Add(derivedType))
                            {
                                // Found a new type.  If it isn't sealed, add it to the list of 
                                // types for us to search for more derived classes of.
                                if (!derivedType.IsSealed)
                                {
                                    classesToSearchFor.Add(derivedType);
                                }
                            }
                        }
                    }
                }
            }

            return finalResult;
        }

        private static Task<IEnumerable<INamedTypeSymbol>> FindDerivedAndImplementingTypesInDocumentAsync(
            HashSet<INamedTypeSymbol> typesToSearchFor,
            HashSet<string> typeNamesToSearchFor,
            Document document,
            HashSet<SemanticModel> cachedModels,
            HashSet<DeclaredSymbolInfo> cachedInfos,
            CancellationToken cancellationToken)
        {
            Func<INamedTypeSymbol, bool> typeMatches = t => DerivesOrImplementsFrom(typesToSearchFor, t);

            return FindTypesInDocumentAsync(
                typeNamesToSearchFor, document,
                cachedModels, cachedInfos, typeMatches,
                cancellationToken);
        }

        private static bool DerivesOrImplementsFrom(
            HashSet<INamedTypeSymbol> typesToSearchFor, INamedTypeSymbol type)
        {
            if (typesToSearchFor.Contains(type.BaseType.OriginalDefinition))
            {
                return true;
            }

            foreach (var interfaceType in type.Interfaces)
            {
                if (typesToSearchFor.Contains(interfaceType.OriginalDefinition))
                {
                    return true;
                }
            }

            return false;
        }

        private static Task<IEnumerable<INamedTypeSymbol>> FindDerivedClassesInDocumentAsync(
            ISet<INamedTypeSymbol> classesToSearchFor,
            ISet<string> classNamesToSearchFor,
            Document document,
            HashSet<SemanticModel> cachedModels,
            HashSet<DeclaredSymbolInfo> cachedInfos,
            CancellationToken cancellationToken)
        {
            Func<INamedTypeSymbol, bool> typeMatches = t => classesToSearchFor.Contains(t.BaseType.OriginalDefinition);
            return FindTypesInDocumentAsync(
                classNamesToSearchFor, document,
                cachedModels, cachedInfos, typeMatches,
                cancellationToken);
        }

        private static async Task<IEnumerable<INamedTypeSymbol>> FindTypesInDocumentAsync(
            ISet<string> classNamesToSearchFor,
            Document document, 
            HashSet<SemanticModel> cachedModels, 
            HashSet<DeclaredSymbolInfo> cachedInfos, 
            Func<INamedTypeSymbol, bool> typeMatches,
            CancellationToken cancellationToken)
        {
            var infos = await document.GetDeclaredSymbolInfosAsync(cancellationToken).ConfigureAwait(false);
            cachedInfos.AddRange(infos);

            HashSet<INamedTypeSymbol> result = null;
            foreach (var info in infos)
            {
                if (AnyInheritanceNamesMatch(info, classNamesToSearchFor))
                {
                    // Looks like we have a potential match.  Actually check if the symbol is viable.
                    var symbol = await ResolveAsync(document, info, cachedModels, cancellationToken).ConfigureAwait(false) as INamedTypeSymbol;
                    if (symbol != null)
                    {
                        if (typeMatches(symbol))
                        {
                            result = result ?? new HashSet<INamedTypeSymbol>();
                            result.Add(symbol);
                        }
                    }
                }
            }

            return result;
        }

        private static bool AnyInheritanceNamesMatch(
            DeclaredSymbolInfo info, ISet<string> classNamesToSearchFor)
        {
            foreach (var name in info.InheritanceNames)
            {
                if (classNamesToSearchFor.Contains(name))
                {
                    return true;
                }
            }

            return false;
        }

        public static Task<IEnumerable<INamedTypeSymbol>> GetTypesImmediatelyDerivedFromInterfacesAsync(
            INamedTypeSymbol type,
            Solution solution,
            CancellationToken cancellationToken)
        {
            return GetTypesImmediatelyDerivedFromInterfacesAsync(
                type, solution, projects: null,
                cachedModels: new ConcurrentSet<SemanticModel>(),
                cancellationToken: cancellationToken);
        }

        private static Task<IEnumerable<INamedTypeSymbol>> GetTypesImmediatelyDerivedFromInterfacesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            ConcurrentSet<SemanticModel> cachedModels,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (type?.TypeKind == TypeKind.Interface)
            {
                type = type.OriginalDefinition;
                return GetTypesAsync(
                    type, solution, projects,
                    GetTypesImmediatelyDerivedFromInterfacesAsync,
                    cachedModels, cancellationToken);
            }

            return SpecializedTasks.EmptyEnumerable<INamedTypeSymbol>();
        }

        private static Task GetTypesImmediatelyDerivedFromInterfacesAsync(
            INamedTypeSymbol baseInterface, Project project, bool locationsInMetadata,
            ConcurrentSet<ISymbol> results, ConcurrentSet<SemanticModel> cachedModels,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Debug.Assert(baseInterface.TypeKind == TypeKind.Interface);
            if (locationsInMetadata)
            {
                // For an interface from metadata, we have to find any metadata types that might
                // derive from this interface.
                return AddMatchingSourceAndMetadataTypesAsync(project, results,
                    GetTypeImmediatelyDerivesFromInterfaceFunction(baseInterface),
                    GetSourceInfoImmediatelyDerivesFromInterfaceFunction(baseInterface),
                    cachedModels, cancellationToken);
            }

            // Check for source symbols that could derive from this type. Look for 
            // DeclaredSymbolInfos in this project that state they derive from a type 
            // with our name.
            return AddMatchingSourceTypesAsync(project, results,
                GetSourceInfoImmediatelyDerivesFromInterfaceFunction(baseInterface),
                cachedModels, cancellationToken);
        }

        private static TypeMatches GetTypeImmediatelyDerivesFromInterfaceFunction(INamedTypeSymbol baseInterface)
        {
            return (t, p, c) => t.Interfaces.Any(i => SymbolEquivalenceComparer.Instance.Equals(i.OriginalDefinition, baseInterface));
        }

        private static SourceInfoMatches GetSourceInfoImmediatelyDerivesFromInterfaceFunction(
            INamedTypeSymbol baseInterface)
        {
            var typeTestFunction = GetTypeImmediatelyDerivesFromInterfaceFunction(baseInterface);

            return async (document, info, cachedModels, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Interfaces, Classes, and Structs can derive from an interface.
                if (info.Kind == DeclaredSymbolInfoKind.Class ||
                    info.Kind == DeclaredSymbolInfoKind.Struct ||
                    info.Kind == DeclaredSymbolInfoKind.Interface)
                {
                    // If one types derives from an interface, then we would expect to see the
                    // interface name in the inheritance list.  Note: the inheritance name also
                    // include mapped aliases if aliases are used in the file it is contained in.
                    foreach (var inheritanceName in info.InheritanceNames)
                    {
                        if (string.Equals(baseInterface.Name, inheritanceName, StringComparison.OrdinalIgnoreCase))
                        {
                            // Looks like a good candidate.  Get the actual symbol for it and see
                            // if the symbol matches the criteria.
                            var candidate = await ResolveAsync(document, info, cachedModels, cancellationToken).ConfigureAwait(false) as INamedTypeSymbol;
                            if (candidate != null && typeTestFunction(candidate, document.Project, cancellationToken))
                            {
                                return candidate;
                            }
                        }
                    }
                }

                return null;
            };
        }

        public static Task<IEnumerable<INamedTypeSymbol>> GetTypesImmediatelyDerivedFromClassesAsync(
            INamedTypeSymbol type,
            Solution solution,
            CancellationToken cancellationToken)
        {
            return GetTypesImmediatelyDerivedFromClassesAsync(
                type, solution, projects: null,
                cachedModels:  new ConcurrentSet<SemanticModel>(),
                cancellationToken: cancellationToken);
        }

        private static Task<IEnumerable<INamedTypeSymbol>> GetTypesImmediatelyDerivedFromClassesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            ConcurrentSet<SemanticModel> cachedModels,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (type?.TypeKind == TypeKind.Class &&
                !type.IsSealed)
            {
                return GetTypesAsync(
                    type, solution, projects,
                    GetTypesImmediatelyDerivedFromClassesAsync,
                    cachedModels, cancellationToken);
            }

            return SpecializedTasks.EmptyEnumerable<INamedTypeSymbol>();
        }

        private static async Task<IEnumerable<INamedTypeSymbol>> GetTypesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            SearchProjectAsync searchProjectAsync,
            ConcurrentSet<SemanticModel> cachedModels,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dependentProjects = await DependentProjectsFinder.GetDependentProjectsAsync(
                type, solution, projects, cancellationToken).ConfigureAwait(false);

            // If it's a type from source, then only other types from source could derive from
            // it.  If it's a type from metadata then unfortunately anything could derive from
            // it.
            var locationsInMetadata = type.Locations.Any(loc => loc.IsInMetadata);
            var results = new ConcurrentSet<ISymbol>(SymbolEquivalenceComparer.Instance);

            cancellationToken.ThrowIfCancellationRequested();

            var projectTasks = new List<Task>();
            foreach (var project in dependentProjects)
            {
                projectTasks.Add(searchProjectAsync(
                    type, project, locationsInMetadata, results, cachedModels, cancellationToken));
            }

            // Search all projects in parallel.
            await Task.WhenAll(projectTasks).ConfigureAwait(false);

            if (results.Any())
            {
                return results.OfType<INamedTypeSymbol>().ToList();
            }

            return SpecializedCollections.EmptyEnumerable<INamedTypeSymbol>();
        }

        private static SourceInfoMatches GetSourceInfoMatchesFunction(DeclaredSymbolInfoKind kind)
        {
            return async (document, info, cachedModels, cancellationToken) =>
            {
                if (info.Kind != kind)
                {
                    return null;
                }

                return await ResolveAsync(document, info, cachedModels, cancellationToken).ConfigureAwait(false);
            };
        }

        private static Task GetTypesImmediatelyDerivedFromClassesAsync(
            INamedTypeSymbol baseType, Project project, bool locationsInMetadata,
            ConcurrentSet<ISymbol> results, ConcurrentSet<SemanticModel> cachedModels,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Debug.Assert(baseType.TypeKind == TypeKind.Class);
            if (locationsInMetadata)
            {
                switch (baseType.SpecialType)
                {
                    // Object needs to be handled specially as many types may derive directly from it,
                    // while having no source indication that that's the case.
                    case SpecialType.System_Object:
                        return GetAllTypesThatDeriveDirectlyFromObjectAsync(
                            project, results, cachedModels, cancellationToken);

                    // Delegates derive from System.MulticastDelegate
                    case SpecialType.System_MulticastDelegate:
                        return AddAllDelegatesAsync(project, results, cachedModels, cancellationToken);

                    // Structs derive from System.System.ValueType
                    case SpecialType.System_ValueType:
                        return AddAllStructsAsync(project, results, cachedModels, cancellationToken);

                    // Enums derive from System.Enum
                    case SpecialType.System_Enum:
                        return AddAllEnumsAsync(project, results, cachedModels, cancellationToken);

                    // A normal class from metadata.
                    default:
                        // Have to search metadata to see if we have any derived types
                        return AddMatchingSourceAndMetadataTypesAsync(project, results,
                            GetTypeImmediatelyDerivesFromBaseTypeFunction(baseType),
                            GetSourceInfoImmediatelyDerivesFromBaseTypeFunction(baseType),
                            cachedModels, cancellationToken);
                }
            }

            // Check for source symbols that could derive from this type. Look for 
            // DeclaredSymbolInfos in this project that state they derive from a type 
            // with our name.
            return AddMatchingSourceTypesAsync(project, results,
                GetSourceInfoImmediatelyDerivesFromBaseTypeFunction(baseType),
                cachedModels, cancellationToken);
        }

        private static Func<INamedTypeSymbol, Project, CancellationToken, bool> GetTypeImmediatelyDerivesFromBaseTypeFunction(
            INamedTypeSymbol baseType)
        {
            return (t, p, c) => OriginalSymbolsMatch(t.BaseType, baseType, p.Solution, c);
        }

        private static Task GetAllTypesThatDeriveDirectlyFromObjectAsync(
            Project project, ConcurrentSet<ISymbol> results,
            ConcurrentSet<SemanticModel> cachedModels,
            CancellationToken cancellationToken)
        {
            // Or source predicate needs to find all classes in the project that derive from 
            // object. Unfortunately, we have to consider all classes. There's no way to 
            // tell syntactically (for C# at least) if a type inherits from object or not.  
            // i.e.  if you have:
            //
            //      class F : IEnumerable
            //
            // Then this derives from object.  We can't use the presence of an 
            // inheritance list to make any determinations.  Note: we could tell
            // for VB.  It may be a good optimization to add later. 

            SourceInfoMatches sourceInfoMatches = async (doc, info, models, c) =>
            {
                if (info.Kind == DeclaredSymbolInfoKind.Class)
                {
                    var symbol = await ResolveAsync(doc, info, models, c).ConfigureAwait(false) as INamedTypeSymbol;
                    if (symbol?.BaseType?.SpecialType == SpecialType.System_Object)
                    {
                        return symbol;
                    }
                }

                return null;
            };

            return AddMatchingSourceAndMetadataTypesAsync(project, results,
                s_derivesFromObject, sourceInfoMatches, cachedModels, 
                cancellationToken);
        }

        private static async Task<ISymbol> ResolveAsync(
            Document doc, DeclaredSymbolInfo info, ICollection<SemanticModel> models, 
            CancellationToken cancellationToken)
        {
            var semanticModel = await doc.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            models.Add(semanticModel);
            return info.Resolve(semanticModel, cancellationToken);
        }

        private static Task AddAllDelegatesAsync(
            Project project, ConcurrentSet<ISymbol> results, ConcurrentSet<SemanticModel> cachedModels,
            CancellationToken cancellationToken)
        {
            return AddMatchingSourceAndMetadataTypesAsync(
                project, results, s_isDelegateType, s_infoMatchesDelegate, cachedModels, cancellationToken);
        }

        private static Task AddAllEnumsAsync(
            Project project, ConcurrentSet<ISymbol> results, ConcurrentSet<SemanticModel> cachedModels,
            CancellationToken cancellationToken)
        {
            return AddMatchingSourceAndMetadataTypesAsync(
                project, results, s_isEnumType, s_infoMatchesEnum, cachedModels, cancellationToken);
        }

        private static Task AddAllStructsAsync(
            Project project, ConcurrentSet<ISymbol> results, ConcurrentSet<SemanticModel> cachedModels,
            CancellationToken cancellationToken)
        {
            return AddMatchingSourceAndMetadataTypesAsync(
                project, results, s_isStructType, s_infoMatchesStruct, cachedModels, cancellationToken);
        }

        private static Task AddMatchingSourceAndMetadataTypesAsync(
            Project project, ConcurrentSet<ISymbol> results,
            TypeMatches metadataTypeMatches,
            SourceInfoMatches sourceInfoMatches,
            ConcurrentSet<SemanticModel> cachedModels,
            CancellationToken cancellationToken)
        {
            var metadataTask = AddMatchingMetadataTypesAsync(project, results, metadataTypeMatches, cancellationToken);
            var sourceTask = AddMatchingSourceTypesAsync(project, results, sourceInfoMatches, cachedModels, cancellationToken);

            // Search source and metadata in parallel.
            return Task.WhenAll(metadataTask, sourceTask);
        }

        private static async Task AddMatchingMetadataTypesAsync(
            Project project, ConcurrentSet<ISymbol> results, 
            TypeMatches metadataTypeMatches, CancellationToken cancellationToken)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var metadataTypes = GetAllAccessibleMetadataTypesInCompilation(compilation, cancellationToken);

            // And add the matching ones to the result set.
            results.AddRange(metadataTypes.Where(t => metadataTypeMatches(t, project, cancellationToken)));
        }

        private static Task AddMatchingSourceTypesAsync(
            Project project,
            ConcurrentSet<ISymbol> results,
            SourceInfoMatches sourceInfoMatches, 
            ConcurrentSet<SemanticModel> models,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Process all documents in the project in parallel.
            var tasks = project.Documents.Select(d =>
                AddMatchingSourceTypesAsync(d, results, sourceInfoMatches, models, cancellationToken)).ToArray();
            return Task.WhenAll(tasks);
        }

        private static async Task AddMatchingSourceTypesAsync(
            Document document, ConcurrentSet<ISymbol> results,
            SourceInfoMatches sourceInfoMatches,
            ConcurrentSet<SemanticModel> cachedModels,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var symbolInfos = await document.GetDeclaredSymbolInfosAsync(cancellationToken).ConfigureAwait(false);
            foreach (var info in symbolInfos)
            {
                var matchingSymbol = await sourceInfoMatches(document, info, cachedModels, cancellationToken).ConfigureAwait(false);
                if (matchingSymbol != null)
                {
                    results.Add(matchingSymbol);
                }
            }
        }

        private static List<INamedTypeSymbol> GetAllAccessibleMetadataTypesInCompilation(
            Compilation compilation, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Note that we are checking the GlobalNamespace of the compilation (which includes all types).
            return s_compilationAllAccessibleMetadataTypesTable.GetValue(compilation, c =>
                compilation.GlobalNamespace.GetAllTypes(cancellationToken)
                                           .Where(t => t.Locations.All(s_isInMetadata) &&
                                                       t.DeclaredAccessibility != Accessibility.Private &&
                                                       t.IsAccessibleWithin(c.Assembly)).ToList());
        }

        private static SourceInfoMatches GetSourceInfoImmediatelyDerivesFromBaseTypeFunction(
            INamedTypeSymbol baseType)
        {
            var typeTestFunction = GetTypeImmediatelyDerivesFromBaseTypeFunction(baseType);

            return async (document, info, models, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Only a class can derive from another class.
                if (info.Kind == DeclaredSymbolInfoKind.Class)
                {
                    // If one class derives from another class, then we would expect to see the
                    // base class name in the inheritance list.  Note: the inheritance name also
                    // include mapped aliases if aliases are used in the file it is contained in.
                    foreach (var inheritanceName in info.InheritanceNames)
                    {
                        // See if we have a type that looks like it could potentially derive from this class.
                        if (string.Equals(baseType.Name, inheritanceName, StringComparison.OrdinalIgnoreCase))
                        {
                            // Looks like a good candidate.  Get the symbol for it and see if it
                            // actually matches.
                            var candidate = await ResolveAsync(document, info, models, cancellationToken).ConfigureAwait(false) as INamedTypeSymbol;
                            if (typeTestFunction(candidate, document.Project, cancellationToken))
                            {
                                return candidate;
                            }
                        }
                    }
                }

                return null;
            };
        }

        internal static bool OriginalSymbolsMatch(
            ISymbol searchSymbol,
            ISymbol symbolToMatch,
            Solution solution,
            CancellationToken cancellationToken)
        {
            if (ReferenceEquals(searchSymbol, symbolToMatch))
            {
                return true;
            }

            if (searchSymbol == null || symbolToMatch == null)
            {
                return false;
            }

            Compilation symbolToMatchCompilation = null;
            if (!TryGetCompilation(symbolToMatch, solution, out symbolToMatchCompilation, cancellationToken))
            {
                return false;
            }

            return OriginalSymbolsMatch(searchSymbol, symbolToMatch, solution, null, symbolToMatchCompilation, cancellationToken);
        }

        internal static bool OriginalSymbolsMatch(
            ISymbol searchSymbol,
            ISymbol symbolToMatch,
            Solution solution,
            Compilation searchSymbolCompilation,
            Compilation symbolToMatchCompilation,
            CancellationToken cancellationToken)
        {
            if (symbolToMatch == null)
            {
                return false;
            }

            if (OriginalSymbolsMatchCore(searchSymbol, symbolToMatch, solution, searchSymbolCompilation, symbolToMatchCompilation, cancellationToken))
            {
                return true;
            }

            if (searchSymbol.Kind == SymbolKind.Namespace && symbolToMatch.Kind == SymbolKind.Namespace)
            {
                // if one of them is a merged namespace symbol and other one is its constituent namespace symbol, they are equivalent.
                var namespace1 = (INamespaceSymbol)searchSymbol;
                var namespace2 = (INamespaceSymbol)symbolToMatch;
                var namespace1Count = namespace1.ConstituentNamespaces.Length;
                var namespace2Count = namespace2.ConstituentNamespaces.Length;
                if (namespace1Count != namespace2Count)
                {
                    if ((namespace1Count > 1 &&
                         namespace1.ConstituentNamespaces.Any(n => NamespaceSymbolsMatch(n, namespace2, solution, cancellationToken))) ||
                        (namespace2Count > 1 &&
                         namespace2.ConstituentNamespaces.Any(n2 => NamespaceSymbolsMatch(namespace1, n2, solution, cancellationToken))))
                    {
                        return true;
                    }
                }
            }

            if (searchSymbol.Kind == SymbolKind.NamedType && symbolToMatch.IsConstructor())
            {
                return OriginalSymbolsMatch(searchSymbol, symbolToMatch.ContainingType, solution, searchSymbolCompilation, symbolToMatchCompilation, cancellationToken);
            }

            return false;
        }

        private static bool OriginalSymbolsMatchCore(
            ISymbol searchSymbol,
            ISymbol symbolToMatch,
            Solution solution,
            Compilation searchSymbolCompilation,
            Compilation symbolToMatchCompilation,
            CancellationToken cancellationToken)
        {
            if (searchSymbol == null || symbolToMatch == null)
            {
                return false;
            }

            searchSymbol = searchSymbol.GetOriginalUnreducedDefinition();
            symbolToMatch = symbolToMatch.GetOriginalUnreducedDefinition();

            // We compare the given searchSymbol and symbolToMatch for equivalence using SymbolEquivalenceComparer
            // as follows:
            //  1)  We compare the given symbols using the SymbolEquivalenceComparer.IgnoreAssembliesInstance,
            //      which ignores the containing assemblies for named types equivalence checks. This is required
            //      to handle equivalent named types which are forwarded to completely different assemblies.
            //  2)  If the symbols are NOT equivalent ignoring assemblies, then they cannot be equivalent.
            //  3)  Otherwise, if the symbols ARE equivalent ignoring assemblies, they may or may not be equivalent
            //      if containing assemblies are NOT ignored. We need to perform additional checks to ensure they
            //      are indeed equivalent:
            //
            //      (a) If IgnoreAssembliesInstance.Equals equivalence visitor encountered any pair of non-nested 
            //          named types which were equivalent in all aspects, except that they resided in different 
            //          assemblies, we need to ensure that all such pairs are indeed equivalent types. Such a pair
            //          of named types is equivalent if and only if one of them is a type defined in either 
            //          searchSymbolCompilation(C1) or symbolToMatchCompilation(C2), say defined in reference assembly
            //          A (version v1) in compilation C1, and the other type is a forwarded type, such that it is 
            //          forwarded from reference assembly A (version v2) to assembly B in compilation C2.
            //      (b) Otherwise, if no such named type pairs were encountered, symbols ARE equivalent.

            using (var equivalentTypesWithDifferingAssemblies = SharedPools.Default<Dictionary<INamedTypeSymbol, INamedTypeSymbol>>().GetPooledObject())
            {
                // 1) Compare searchSymbol and symbolToMatch using SymbolEquivalenceComparer.IgnoreAssembliesInstance
                if (!SymbolEquivalenceComparer.IgnoreAssembliesInstance.Equals(searchSymbol, symbolToMatch, equivalentTypesWithDifferingAssemblies.Object))
                {
                    // 2) If the symbols are NOT equivalent ignoring assemblies, then they cannot be equivalent.
                    return false;
                }

                // 3) If the symbols ARE equivalent ignoring assemblies, they may or may not be equivalent if containing assemblies are NOT ignored.
                if (equivalentTypesWithDifferingAssemblies.Object.Count > 0)
                {
                    // Step 3a) Ensure that all pairs of named types in equivalentTypesWithDifferingAssemblies are indeed equivalent types.
                    return VerifyForwardedTypes(equivalentTypesWithDifferingAssemblies.Object, searchSymbol, symbolToMatch,
                        solution, searchSymbolCompilation, symbolToMatchCompilation, cancellationToken);
                }

                // 3b) If no such named type pairs were encountered, symbols ARE equivalent.
                return true;
            }
        }

        private static bool NamespaceSymbolsMatch(
            INamespaceSymbol namespace1,
            INamespaceSymbol namespace2,
            Solution solution,
            CancellationToken cancellationToken)
        {
            return OriginalSymbolsMatch(namespace1, namespace2, solution, cancellationToken);
        }

        // Verifies that all pairs of named types in equivalentTypesWithDifferingAssemblies are equivalent forwarded types.
        private static bool VerifyForwardedTypes(
            Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies,
            ISymbol searchSymbol,
            ISymbol symbolToMatch,
            Solution solution,
            Compilation searchSymbolCompilation,
            Compilation symbolToMatchCompilation,
            CancellationToken cancellationToken)
        {
            var verifiedKeys = new HashSet<INamedTypeSymbol>();
            var count = equivalentTypesWithDifferingAssemblies.Count;
            int verifiedCount = 0;

            // First check forwarded types in searchSymbolCompilation.
            if (searchSymbolCompilation != null || TryGetCompilation(searchSymbol, solution, out searchSymbolCompilation, cancellationToken))
            {
                verifiedCount = VerifyForwardedTypes(equivalentTypesWithDifferingAssemblies, searchSymbolCompilation, verifiedKeys, isSearchSymbolCompilation: true);
                if (verifiedCount == count)
                {
                    // All equivalent types verified.
                    return true;
                }
            }

            // Now check forwarded types in symbolToMatchCompilation.
            verifiedCount += VerifyForwardedTypes(equivalentTypesWithDifferingAssemblies, symbolToMatchCompilation, verifiedKeys, isSearchSymbolCompilation: false);
            return verifiedCount == count;
        }

        private static int VerifyForwardedTypes(
            Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies,
            Compilation compilation,
            HashSet<INamedTypeSymbol> verifiedKeys,
            bool isSearchSymbolCompilation)
        {
            Contract.ThrowIfNull(compilation);
            Contract.ThrowIfNull(equivalentTypesWithDifferingAssemblies);
            Contract.ThrowIfTrue(!equivalentTypesWithDifferingAssemblies.Any());

            // Must contain equivalents named types residing in different assemblies.
            Contract.ThrowIfFalse(equivalentTypesWithDifferingAssemblies.All(kvp => !SymbolEquivalenceComparer.Instance.Equals(kvp.Key.ContainingAssembly, kvp.Value.ContainingAssembly)));

            // Must contain non-nested named types.
            Contract.ThrowIfFalse(equivalentTypesWithDifferingAssemblies.All(kvp => kvp.Key.ContainingType == null));
            Contract.ThrowIfFalse(equivalentTypesWithDifferingAssemblies.All(kvp => kvp.Value.ContainingType == null));

            var referencedAssemblies = new MultiDictionary<string, IAssemblySymbol>();
            foreach (var assembly in compilation.GetReferencedAssemblySymbols())
            {
                referencedAssemblies.Add(assembly.Name, assembly);
            }

            int verifiedCount = 0;
            foreach (var kvp in equivalentTypesWithDifferingAssemblies)
            {
                if (!verifiedKeys.Contains(kvp.Key))
                {
                    INamedTypeSymbol originalType, expectedForwardedType;
                    if (isSearchSymbolCompilation)
                    {
                        originalType = kvp.Value.OriginalDefinition;
                        expectedForwardedType = kvp.Key.OriginalDefinition;
                    }
                    else
                    {
                        originalType = kvp.Key.OriginalDefinition;
                        expectedForwardedType = kvp.Value.OriginalDefinition;
                    }

                    foreach (var referencedAssembly in referencedAssemblies[originalType.ContainingAssembly.Name])
                    {
                        var fullyQualifiedTypeName = originalType.MetadataName;
                        if (originalType.ContainingNamespace != null)
                        {
                            fullyQualifiedTypeName = originalType.ContainingNamespace.ToDisplayString(SymbolDisplayFormats.SignatureFormat) +
                                "." + fullyQualifiedTypeName;
                        }

                        // Resolve forwarded type and verify that the types from different assembly are indeed equivalent.
                        var forwardedType = referencedAssembly.ResolveForwardedType(fullyQualifiedTypeName);
                        if (forwardedType == expectedForwardedType)
                        {
                            verifiedKeys.Add(kvp.Key);
                            verifiedCount++;
                        }
                    }
                }
            }

            return verifiedCount;
        }

        private static bool TryGetCompilation(
            ISymbol symbol,
            Solution solution,
            out Compilation definingCompilation,
            CancellationToken cancellationToken)
        {
            var definitionProject = solution.GetProject(symbol.ContainingAssembly, cancellationToken);
            if (definitionProject == null)
            {
                definingCompilation = null;
                return false;
            }

            // compilation from definition project must already exist.
            if (!definitionProject.TryGetCompilation(out definingCompilation))
            {
                Contract.Requires(false, "How can compilation not exist?");
                return false;
            }

            return true;
        }
    }
}
