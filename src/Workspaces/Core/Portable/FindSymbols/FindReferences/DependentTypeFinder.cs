// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
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
        private static Func<Location, bool> s_isInMetadata = loc => loc.IsInMetadata;
        private static Func<Location, bool> s_isInSource = loc => loc.IsInSource;

        private static Func<INamedTypeSymbol, bool> s_isNonSealedClass = 
            t => t?.TypeKind == TypeKind.Class && !t.IsSealed;

        private static readonly Func<INamedTypeSymbol, bool> s_isInterfaceOrNonSealedClass =
            t => t.TypeKind == TypeKind.Interface || s_isNonSealedClass(t);

        /// <summary>
        /// For a given <see cref="Compilation"/>, stores a flat list of all the accessible metadata types
        /// within the compilation.
        /// </summary>
        private static readonly ConditionalWeakTable<Compilation, List<INamedTypeSymbol>> s_compilationAllAccessibleMetadataTypesTable =
            new ConditionalWeakTable<Compilation, List<INamedTypeSymbol>>();

        /// <summary>
        /// Used for implementing the Inherited-By relation for progression.
        /// </summary>
        internal static Task<IEnumerable<INamedTypeSymbol>> FindImmediatelyDerivedClassesAsync(
            INamedTypeSymbol type,
            Solution solution,
            CancellationToken cancellationToken)
        {
            return FindDerivedClassesAsync(type, solution, projects: null,
                transitive: false, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// This is an internal implementation of <see cref="SymbolFinder.FindDerivedClassesAsync"/>, which is a publically callable method.
        /// </summary>
        public static Task<IEnumerable<INamedTypeSymbol>> FindTransitivelyDerivedClassesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            return FindDerivedClassesAsync(type, solution, projects,
                transitive: true, cancellationToken: cancellationToken);
        }

        private static Task<IEnumerable<INamedTypeSymbol>> FindDerivedClassesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects, 
            bool transitive,
            CancellationToken cancellationToken)
        {
            if (s_isNonSealedClass(type))
            {
                Func<HashSet<INamedTypeSymbol>, INamedTypeSymbol, bool> metadataTypeMatches =
                    (set, metadataType) => TypeDerivesFrom(set, metadataType, transitive);

                Func<HashSet<INamedTypeSymbol>, INamedTypeSymbol, bool> sourceTypeImmediatelyMatches =
                    (set, metadataType) => set.Contains(metadataType.BaseType?.OriginalDefinition);

                return FindTypesAsync(type, solution, projects,
                    metadataTypeMatches: metadataTypeMatches,
                    sourceTypeImmediatelyMatches: sourceTypeImmediatelyMatches,
                    shouldContinueSearching: s_isNonSealedClass,
                    transitive: transitive,
                    cancellationToken: cancellationToken);
            }

            return SpecializedTasks.EmptyEnumerable<INamedTypeSymbol>();
        }

        /// <summary>
        /// Implementation of <see cref="SymbolFinder.FindImplementationsAsync"/> for 
        /// <see cref="INamedTypeSymbol"/>s
        /// </summary>
        public static async Task<IEnumerable<INamedTypeSymbol>> FindTransitivelyImplementingTypesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            var derivedAndImplementingTypes = await FindDerivedAndImplementingTypesAsync(
                type, solution, projects,
                transitive: true, cancellationToken: cancellationToken).ConfigureAwait(false);

            // We only want implementing types here, not derived interfaces.
            return derivedAndImplementingTypes.Where(t => t.TypeKind == TypeKind.Class).ToList();
        }

        /// <summary>
        /// Used for implementing the Inherited-By relation for progression.
        /// </summary>
        internal static Task<IEnumerable<INamedTypeSymbol>> FindImmediatelyDerivedAndImplementingTypesAsync(
            INamedTypeSymbol type,
            Solution solution,
            CancellationToken cancellationToken)
        {
            return FindDerivedAndImplementingTypesAsync(
                type, solution, projects: null,
                transitive: false, cancellationToken: cancellationToken);
        }

        private static Task<IEnumerable<INamedTypeSymbol>> FindDerivedAndImplementingTypesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            bool transitive, 
            CancellationToken cancellationToken)
        {
            // Only an interface can be implemented.
            if (type?.TypeKind == TypeKind.Interface)
            {
                Func<HashSet<INamedTypeSymbol>, INamedTypeSymbol, bool> metadataTypeMatches =
                    (s, t) => TypeDerivesFrom(s, t, transitive) ||
                              TypeImplementsFrom(s, t, transitive);

                return FindTypesAsync(type, solution, projects,
                    metadataTypeMatches: metadataTypeMatches,
                    sourceTypeImmediatelyMatches: ImmediatelyDerivesOrImplementsFrom,
                    shouldContinueSearching: s_isInterfaceOrNonSealedClass,
                    transitive: transitive,
                    cancellationToken: cancellationToken);
           }

            return SpecializedTasks.EmptyEnumerable<INamedTypeSymbol>();
        }

        private static async Task<IEnumerable<INamedTypeSymbol>> FindTypesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            Func<HashSet<INamedTypeSymbol>, INamedTypeSymbol, bool> metadataTypeMatches,
            Func<HashSet<INamedTypeSymbol>, INamedTypeSymbol, bool> sourceTypeImmediatelyMatches,
            Func<INamedTypeSymbol, bool> shouldContinueSearching,
            bool transitive,
            CancellationToken cancellationToken)
        {
            type = type.OriginalDefinition;
            projects = projects ?? ImmutableHashSet.Create(solution.Projects.ToArray());
            var searchInMetadata = type.Locations.Any(s_isInMetadata);

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
                solution, projects, projectsThatCouldReferenceType);

            var currentMetadataTypes = new HashSet<INamedTypeSymbol>(SymbolEquivalenceComparer.Instance);
            var currentSourceAndMetadataTypes = new HashSet<INamedTypeSymbol>(SymbolEquivalenceComparer.Instance);

            currentSourceAndMetadataTypes.Add(type);
            if (searchInMetadata)
            {
                currentMetadataTypes.Add(type);
            }

            var result = new HashSet<INamedTypeSymbol>(SymbolEquivalenceComparer.Instance);

            // Now walk the projects from left to right seeing what our type cascades to. Once we 
            // reach a fixed point in that project, take all the types we've found and move to the
            // next project.  Continue this until we've exhausted all projects.
            //
            // Because there is a data-dependency between the projects, we cannot process them in
            // parallel.  (Processing linearly is also probably preferable to limit the amount of
            // cache churn we could cause creating all those compilations.
            foreach (var project in orderedProjectsToExamine)
            {
                await FindTypesInProjectAsync(
                    searchInMetadata, result,
                    currentMetadataTypes, currentSourceAndMetadataTypes,
                    project,
                    metadataTypeMatches,
                    sourceTypeImmediatelyMatches,
                    shouldContinueSearching,
                    transitive, cancellationToken).ConfigureAwait(false);
            }

            return result;
        }

        private static async Task FindTypesInProjectAsync(
            bool searchInMetadata,
            HashSet<INamedTypeSymbol> result,
            HashSet<INamedTypeSymbol> currentMetadataTypes,
            HashSet<INamedTypeSymbol> currentSourceAndMetadataTypes,
            Project project,
            Func<HashSet<INamedTypeSymbol>, INamedTypeSymbol, bool> metadataTypeMatches,
            Func<HashSet<INamedTypeSymbol>, INamedTypeSymbol, bool> sourceTypeImmediatelyMatches,
            Func<INamedTypeSymbol, bool> shouldContinueSearching,
            bool transitive,
            CancellationToken cancellationToken)
        {
            // First see what derived metadata types we might find in this project.
            // This is only necessary if we started with a metadata type.
            if (searchInMetadata)
            {
                var foundMetadataTypes = await FindMetadataTypesInProjectAsync(
                    currentMetadataTypes, project, metadataTypeMatches, cancellationToken).ConfigureAwait(false);

                foreach (var foundType in foundMetadataTypes)
                {
                    Debug.Assert(foundType.Locations.Any(s_isInMetadata));

                    // Add to the result list.
                    result.Add(foundType);

                    if (transitive && shouldContinueSearching(foundType))
                    {
                        currentMetadataTypes.Add(foundType);
                        currentSourceAndMetadataTypes.Add(foundType);
                    }
                }
            }

            // Now search the project and see what source types we can find.
            var foundSourceTypes = await FindSourceTypesInProjectAsync(
                currentSourceAndMetadataTypes, project, 
                sourceTypeImmediatelyMatches,
                shouldContinueSearching,
                transitive, cancellationToken).ConfigureAwait(false);

            foreach (var foundType in foundSourceTypes)
            {
                Debug.Assert(foundType.Locations.All(s_isInSource));

                // Add to the result list.
                result.Add(foundType);

                if (transitive && shouldContinueSearching(foundType))
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
            IEnumerable<ProjectId> projectsThatCouldReferenceType)
        {
            var projectsToExamine = GetProjectsToExamineWorker(
                solution, projects, projectsThatCouldReferenceType);

            // Ensure the projects we're going to examine are ordered topologically.
            // That way we can just sweep over them from left to right as no project
            // could affect a previous project in the sweep.
            return OrderTopologically(solution, projectsToExamine);
        }

        private static List<Project> OrderTopologically(
            Solution solution, IEnumerable<Project> projectsToExamine)
        {
            var order = new Dictionary<ProjectId, int>(capacity: solution.ProjectIds.Count);

            int index = 0;

            var dependencyGraph = solution.GetProjectDependencyGraph();
            foreach (var projectId in dependencyGraph.GetTopologicallySortedProjects())
            {
                order.Add(projectId, index);
                index++;
            }

            return projectsToExamine.OrderBy((p1, p2) => order[p1.Id] - order[p2.Id]).ToList();
        }

        private static IEnumerable<Project> GetProjectsToExamineWorker(
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
            // and we're passed in 'B, C, E' as hte project to search, then this set 
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

            return projectsThatCouldReferenceType.Intersect(allProjectsThatTheseProjectsDependOn)
                                                 .Select(solution.GetProject)
                                                 .ToList();
        }

        private static async Task<IEnumerable<INamedTypeSymbol>> FindMetadataTypesInProjectAsync(
            HashSet<INamedTypeSymbol> metadataTypes,
            Project project,
            Func<HashSet<INamedTypeSymbol>, INamedTypeSymbol, bool> metadataTypeMatches,
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
                if (metadataTypeMatches(metadataTypes, type))
                {
                    result.Add(type);
                }
            }

            return result;
        }

        private static bool TypeDerivesFrom(
            HashSet<INamedTypeSymbol> metadataTypes, INamedTypeSymbol type, bool transitive)
        {
            if (transitive)
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
            else
            {
                return metadataTypes.Contains(type.BaseType?.OriginalDefinition);
            }
        }

        private static bool TypeImplementsFrom(
            HashSet<INamedTypeSymbol> metadataTypes, INamedTypeSymbol type, bool transitive)
        {
            var interfaces = transitive ? type.AllInterfaces : type.Interfaces;

            foreach (var interfaceType in interfaces)
            {
                if (metadataTypes.Contains(interfaceType.OriginalDefinition))
                {
                    return true;
                }
            }

            return false;
        }

        private static async Task<IEnumerable<INamedTypeSymbol>> FindSourceTypesInProjectAsync(
            HashSet<INamedTypeSymbol> sourceAndMetadataTypes,
            Project project,
            Func<HashSet<INamedTypeSymbol>, INamedTypeSymbol, bool> sourceTypeImmediatelyMatches,
            Func<INamedTypeSymbol, bool> shouldContinueSearching,
            bool transitive,
            CancellationToken cancellationToken)
        {
            // We're going to be sweeping over this project over and over until we reach a 
            // fixed point.  In order to limit GC and excess work, we cache all the sematic
            // models and DeclaredSymbolInfo for hte documents we look at.
            // Because we're only processing a project at a time, this is not an issue.
            var cachedModels = new ConcurrentSet<SemanticModel>();
            var cachedInfos = new ConcurrentSet<IDeclarationInfo>();

            var finalResult = new HashSet<INamedTypeSymbol>(SymbolEquivalenceComparer.Instance);

            var typesToSearchFor = new HashSet<INamedTypeSymbol>(SymbolEquivalenceComparer.Instance);
            typesToSearchFor.AddAll(sourceAndMetadataTypes);

            var inheritanceQuery = new InheritanceQuery(sourceAndMetadataTypes);

            // As long as there are new types to search for, keep looping.
            while (typesToSearchFor.Count > 0)
            {
                // Compute the set of names to look for in the base/interface lists.
                inheritanceQuery.TypeNames.AddRange(typesToSearchFor.Select(c => c.Name));

                // Search all the documents of this project in parallel.
                var tasks = project.Documents.Select(d => FindImmediatelyInheritingTypesInDocumentAsync(
                    d, typesToSearchFor, inheritanceQuery,
                    cachedModels, cachedInfos, 
                    sourceTypeImmediatelyMatches, cancellationToken)).ToArray();

                await Task.WhenAll(tasks).ConfigureAwait(false);

                // Clear out the information about the types we're looking for.  We'll
                // fill these in if we discover any more types that we need to keep searching
                // for.
                typesToSearchFor.Clear();
                inheritanceQuery.TypeNames.Clear();

                foreach (var task in tasks)
                {
                    if (task.Result != null)
                    {
                        foreach (var derivedType in task.Result)
                        {
                            if (finalResult.Add(derivedType))
                            {
                                if (transitive && shouldContinueSearching(derivedType))
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

        private static bool ImmediatelyDerivesOrImplementsFrom(
            HashSet<INamedTypeSymbol> typesToSearchFor, INamedTypeSymbol type)
        {
            if (typesToSearchFor.Contains(type.BaseType?.OriginalDefinition))
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

        private static async Task<IEnumerable<INamedTypeSymbol>> FindImmediatelyInheritingTypesInDocumentAsync(
            Document document,
            HashSet<INamedTypeSymbol> typesToSearchFor,
            InheritanceQuery inheritanceQuery,
            ConcurrentSet<SemanticModel> cachedModels, 
            ConcurrentSet<IDeclarationInfo> cachedInfos, 
            Func<HashSet<INamedTypeSymbol>, INamedTypeSymbol, bool> typeImmediatelyMatches,
            CancellationToken cancellationToken)
        {
            var declarationInfo = await document.GetDeclarationInfoAsync(cancellationToken).ConfigureAwait(false);
            cachedInfos.Add(declarationInfo);

            HashSet<INamedTypeSymbol> result = null;
            foreach (var symbolInfo in declarationInfo.DeclaredSymbolInfos)
            {
                result = await ProcessSymbolInfo(
                    document, symbolInfo,
                    typesToSearchFor,
                    inheritanceQuery, cachedModels,
                    typeImmediatelyMatches, result, cancellationToken).ConfigureAwait(false);
            }

            return result;
        }

        private static async Task<HashSet<INamedTypeSymbol>> ProcessSymbolInfo(
            Document document,
            DeclaredSymbolInfo info,
            HashSet<INamedTypeSymbol> typesToSearchFor,
            InheritanceQuery inheritanceQuery,
            ConcurrentSet<SemanticModel> cachedModels,
            Func<HashSet<INamedTypeSymbol>, INamedTypeSymbol, bool> typeImmediatelyMatches,
            HashSet<INamedTypeSymbol> result,
            CancellationToken cancellationToken)
        {
            // If we're searching for enums/structs/delegates, then we can just look at the kind of
            // the info to see if we have a match.
            if ((inheritanceQuery.DerivesFromSystemEnum && info.Kind == DeclaredSymbolInfoKind.Enum) ||
                (inheritanceQuery.DerivesFromSystemValueType && info.Kind == DeclaredSymbolInfoKind.Struct) ||
                (inheritanceQuery.DerivesFromSystemMulticastDelegate && info.Kind == DeclaredSymbolInfoKind.Delegate))
            {
                var symbol = await ResolveAsync(document, info, cachedModels, cancellationToken).ConfigureAwait(false) as INamedTypeSymbol;
                if (symbol != null)
                {
                    result = result ?? new HashSet<INamedTypeSymbol>();
                    result.Add(symbol);
                }
            }
            else if (inheritanceQuery.DerivesFromSystemObject && info.Kind == DeclaredSymbolInfoKind.Class)
            {
                // Searching for types derived from 'Object' needs to be handled specially.
                // There may be no indication in source what the type actually derives from.
                // Also, we can't just look for an empty inheritance list.  We may have 
                // something like: "class C : IFoo".  This type derives from object, despite
                // having a non-empty list.
                var symbol = await ResolveAsync(document, info, cachedModels, cancellationToken).ConfigureAwait(false) as INamedTypeSymbol;
                if (symbol?.BaseType?.SpecialType == SpecialType.System_Object)
                {
                    result = result ?? new HashSet<INamedTypeSymbol>();
                    result.Add(symbol);
                }
            }
            else if (AnyInheritanceNamesMatch(info, inheritanceQuery.TypeNames))
            {
                // Looks like we have a potential match.  Actually check if the symbol is viable.
                var symbol = await ResolveAsync(document, info, cachedModels, cancellationToken).ConfigureAwait(false) as INamedTypeSymbol;
                if (symbol != null)
                {
                    if (typeImmediatelyMatches(typesToSearchFor, symbol))
                    {
                        result = result ?? new HashSet<INamedTypeSymbol>();
                        result.Add(symbol);
                    }
                }
            }

            return result;
        }

        private static bool AnyInheritanceNamesMatch(
            DeclaredSymbolInfo info, HashSet<string> typeNamesToSearchFor)
        {
            foreach (var name in info.InheritanceNames)
            {
                if (typeNamesToSearchFor.Contains(name))
                {
                    return true;
                }
            }

            return false;
        }

        private static async Task<ISymbol> ResolveAsync(
            Document document, DeclaredSymbolInfo info, ConcurrentSet<SemanticModel> cachedModels, 
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            cachedModels.Add(semanticModel);
            return info.Resolve(semanticModel, cancellationToken);
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

        private class InheritanceQuery
        {
            public readonly bool DerivesFromSystemObject;
            public readonly bool DerivesFromSystemValueType;
            public readonly bool DerivesFromSystemEnum;
            public readonly bool DerivesFromSystemMulticastDelegate;

            public readonly HashSet<string> TypeNames;

            public InheritanceQuery(HashSet<INamedTypeSymbol> sourceAndMetadataTypes)
            {
                DerivesFromSystemObject = sourceAndMetadataTypes.Any(t => t.SpecialType == SpecialType.System_Object);
                DerivesFromSystemValueType = sourceAndMetadataTypes.Any(t => t.SpecialType == SpecialType.System_ValueType);
                DerivesFromSystemEnum = sourceAndMetadataTypes.Any(t => t.SpecialType == SpecialType.System_Enum);
                DerivesFromSystemMulticastDelegate = sourceAndMetadataTypes.Any(t => t.SpecialType == SpecialType.System_MulticastDelegate);
                TypeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}