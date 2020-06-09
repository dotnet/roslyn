﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols.DependentProjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    using DependentProjectMap = ConcurrentDictionary<DefinitionProject, AsyncLazy<ImmutableArray<DependentProject>>>;

    /// <summary>
    /// Provides helper methods for finding dependent projects across a solution that a given symbol can be referenced within.
    /// </summary>
    internal static partial class DependentProjectsFinder
    {
        /// <summary>
        /// Dependent projects cache.
        /// For a given solution, maps from an assembly (source-project or metadata-assembly) to the set of projects referencing it.
        ///     Key: DefinitionProject, which contains the project-id for a source-project-assembly, or assembly-name for a metadata-assembly.
        ///     Value: List of DependentProjects, where each DependentProject contains a dependent project ID and a flag indicating whether the dependent project has internals access to definition project.
        /// </summary>
        private static readonly ConditionalWeakTable<Solution, DependentProjectMap> s_dependentProjectsCache =
            new ConditionalWeakTable<Solution, DependentProjectMap>();

        public static async Task<ImmutableArray<Project>> GetDependentProjectsAsync(
            Solution solution, ISymbol symbol, IImmutableSet<Project>? projects, CancellationToken cancellationToken)
        {
            if (symbol.Kind == SymbolKind.Namespace)
            {
                // namespaces are visible in all projects.
                return projects != null
                    ? projects.ToImmutableArray()
                    : solution.Projects.ToImmutableArray();
            }
            else
            {
                var dependentProjects = await GetDependentProjectsWorkerAsync(solution, symbol, cancellationToken).ConfigureAwait(false);
                return projects != null
                    ? dependentProjects.WhereAsArray(projects.Contains)
                    : dependentProjects;
            }
        }

        /// <summary>
        /// This method computes the dependent projects that need to be searched for references of the given <paramref name="symbol"/>.
        /// This computation depends on the given symbol's visibility:
        ///     1) Public: Dependent projects include the symbol definition project and all the referencing projects.
        ///     2) Internal: Dependent projects include the symbol definition project and all the referencing projects that have internals access to the definition project.
        ///     3) Private: Dependent projects include the symbol definition project and all the referencing submission projects (which are special and can reference private fields of the previous submission).
        /// 
        /// We perform this computation in two stages:
        ///     1) Compute all the dependent projects (submission + non-submission) and their InternalsVisibleTo semantics to the definition project.
        ///     2) Filter the above computed dependent projects based on symbol visibility.
        /// Dependent projects computed in stage (1) are cached to avoid recomputation.
        /// </summary>
        private static async Task<ImmutableArray<Project>> GetDependentProjectsWorkerAsync(
            Solution solution, ISymbol symbol, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var symbolOrigination = GetSymbolOrigination(solution, symbol, cancellationToken);

            // If we can't find where the symbol came from, we can't determine what projects to search for references to it.
            if (symbolOrigination.assembly == null)
                return ImmutableArray<Project>.Empty;

            // 1) Compute all the dependent projects (submission + non-submission) and their InternalsVisibleTo semantics to the definition project.

            var visibility = symbol.GetResultantVisibility();
            if (visibility == SymbolVisibility.Private)
            {
                // For private symbols, we only need the current project (and related submissions).  No need to cache
                // that, just simply compute and return the result.
                return GetProjects(solution, await ComputeDependentProjectsAsync(solution, symbolOrigination, visibility, cancellationToken).ConfigureAwait(false));
            }

            // Otherwise, for non-private symbols, we cache the dependent projects for non-private symbols to speed up
            // future calls.
            var dependentProjectsMap = s_dependentProjectsCache.GetValue(solution, _ => new DependentProjectMap());

            var asyncLazy = dependentProjectsMap.GetOrAdd(
                new DefinitionProject(symbolOrigination.sourceProject?.Id, symbolOrigination.assembly.Name.ToLower()),
                _ => AsyncLazy.Create(c => ComputeDependentProjectsAsync(solution, symbolOrigination, visibility, c), cacheResult: true));
            var dependentProjects = await asyncLazy.GetValueAsync(cancellationToken).ConfigureAwait(false);

            // 2) Filter the above computed dependent projects based on symbol visibility.
            return GetProjects(solution, visibility == SymbolVisibility.Internal
                ? dependentProjects.WhereAsArray(dp => dp.HasInternalsAccess)
                : dependentProjects);
        }

        private static ImmutableArray<Project> GetProjects(Solution solution, ImmutableArray<DependentProject> dependentProjects)
            => dependentProjects.SelectAsArray(dp => solution.GetRequiredProject(dp.ProjectId));

        /// <summary>
        /// Returns a pair of data bout where <paramref name="symbol"/> originates from.  It's <see
        /// cref="IAssemblySymbol"/> for both source and metadata symbols, and an optional <see cref="Project"/> if this
        /// was a symbol from source.
        /// </summary>
        private static (IAssemblySymbol assembly, Project? sourceProject) GetSymbolOrigination(
            Solution solution, ISymbol symbol, CancellationToken cancellationToken)
        {
            var assembly = symbol.OriginalDefinition.ContainingAssembly;
            return assembly == null ? default : (assembly, solution.GetProject(assembly, cancellationToken));
        }

        private static async Task<ImmutableArray<DependentProject>> ComputeDependentProjectsAsync(
            Solution solution,
            (IAssemblySymbol assembly, Project? sourceProject) symbolOrigination,
            SymbolVisibility visibility,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dependentProjects = new HashSet<DependentProject>();

            // If a symbol was defined in source, then it is always visible to the project it
            // was defined in.
            if (symbolOrigination.sourceProject != null)
                dependentProjects.Add(new DependentProject(symbolOrigination.sourceProject.Id, hasInternalsAccess: true));

            // If it's not private, then we need to find possible references.
            if (visibility != SymbolVisibility.Private)
                AddNonSubmissionDependentProjects(solution, symbolOrigination, dependentProjects, cancellationToken);

            // submission projects are special here. The fields generated inside the Script object is private, but
            // further submissions can bind to them.
            await AddSubmissionDependentProjectsAsync(solution, symbolOrigination.sourceProject, dependentProjects, cancellationToken).ConfigureAwait(false);

            return dependentProjects.ToImmutableArray();
        }

        private static async Task AddSubmissionDependentProjectsAsync(
            Solution solution, Project? sourceProject, HashSet<DependentProject> dependentProjects, CancellationToken cancellationToken)
        {
            if (sourceProject?.IsSubmission != true)
                return;

            var projectIdsToReferencingSubmissionIds = new Dictionary<ProjectId, List<ProjectId>>();

            // search only submission project
            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetRequiredProject(projectId);
                if (project.IsSubmission && project.SupportsCompilation)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // If we are referencing another project, store the link in the other direction
                    // so we walk across it later
                    var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                    var previous = compilation.ScriptCompilationInfo?.PreviousScriptCompilation;

                    if (previous != null)
                    {
                        var referencedProject = solution.GetProject(previous.Assembly, cancellationToken);
                        if (referencedProject != null)
                        {
                            if (!projectIdsToReferencingSubmissionIds.TryGetValue(referencedProject.Id, out var referencingSubmissions))
                            {
                                referencingSubmissions = new List<ProjectId>();
                                projectIdsToReferencingSubmissionIds.Add(referencedProject.Id, referencingSubmissions);
                            }

                            referencingSubmissions.Add(project.Id);
                        }
                    }
                }
            }

            // Submission compilations are special. If we have submissions 0, 1 and 2 chained in
            // the natural way, and we have a symbol in submission 0, we need to search both 1
            // and 2, even though 2 doesn't have a direct reference to 1. Hence we need to take
            // our current set of projects and find the transitive closure over backwards
            // submission previous references.
            var projectIdsToProcess = new Stack<ProjectId>(dependentProjects.Select(dp => dp.ProjectId));

            while (projectIdsToProcess.Count > 0)
            {
                var toProcess = projectIdsToProcess.Pop();

                if (projectIdsToReferencingSubmissionIds.TryGetValue(toProcess, out var submissionIds))
                {
                    foreach (var pId in submissionIds)
                    {
                        if (!dependentProjects.Any(dp => dp.ProjectId == pId))
                        {
                            dependentProjects.Add(new DependentProject(pId, hasInternalsAccess: true));
                            projectIdsToProcess.Push(pId);
                        }
                    }
                }
            }
        }

        private static bool IsInternalsVisibleToAttribute(AttributeData attr)
        {
            var attrType = attr.AttributeClass;
            return attrType?.Name == nameof(InternalsVisibleToAttribute) &&
                   attrType.ContainingNamespace?.Name == nameof(System.Runtime.CompilerServices) &&
                   attrType.ContainingNamespace.ContainingNamespace?.Name == nameof(System.Runtime) &&
                   attrType.ContainingNamespace.ContainingNamespace.ContainingNamespace?.Name == nameof(System) &&
                   attrType.ContainingNamespace.ContainingNamespace.ContainingNamespace.ContainingNamespace?.IsGlobalNamespace == true;
        }

        private static void AddNonSubmissionDependentProjects(
            Solution solution,
            (IAssemblySymbol assembly, Project? sourceProject) symbolOrigination,
            HashSet<DependentProject> dependentProjects,
            CancellationToken cancellationToken)
        {
            if (symbolOrigination.sourceProject?.IsSubmission == true)
                return;

            // Set of assembly names that `assembly` has IVT to.  Computed on demand once needed.
            HashSet<string>? internalsVisibleToSet = null;
            foreach (var project in solution.Projects)
            {
                if (!project.SupportsCompilation ||
                    !HasReferenceTo(symbolOrigination, project, cancellationToken))
                {
                    continue;
                }

                // Ok, we have some project that at least references this assembly.  Add it to the result, keeping track
                // if it can see internals or not as well.
                internalsVisibleToSet ??= GetInternalsVisibleToSet(symbolOrigination.assembly);
                var internalsVisableTo = internalsVisibleToSet.Contains(project.AssemblyName);
                dependentProjects.Add(new DependentProject(project.Id, internalsVisableTo));
            }
        }

        private static HashSet<string> GetInternalsVisibleToSet(IAssemblySymbol assembly)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var attr in assembly.GetAttributes().Where(IsInternalsVisibleToAttribute))
            {
                var typeNameConstant = attr.ConstructorArguments.FirstOrDefault();
                if (typeNameConstant.Type == null ||
                    typeNameConstant.Type.SpecialType != SpecialType.System_String ||
                    !(typeNameConstant.Value is string value))
                {
                    continue;
                }

                var commaIndex = value.IndexOf(',');
                var assemblyName = commaIndex >= 0 ? value.Substring(0, commaIndex).Trim() : value;

                set.Add(assemblyName);
            }

            return set;
        }

        private static bool HasReferenceTo(
            (IAssemblySymbol assembly, Project? sourceProject) symbolOrigination,
            Project project,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(symbolOrigination.assembly);
            Contract.ThrowIfNull(project);
            Contract.ThrowIfFalse(project.SupportsCompilation);

            // If our symbol was from a project, then just check if this current project has a direct reference to it.
            if (symbolOrigination.sourceProject != null)
                return project.ProjectReferences.Any(p => p.ProjectId == symbolOrigination.sourceProject.Id);

            // Otherwise, if the symbol is from metadata, see if the project's compilation references that metadata assembly.
            return HasReferenceToAssembly(project, symbolOrigination.assembly.Name, cancellationToken);
        }

        private static bool HasReferenceToAssembly(Project project, string assemblyName, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(project.SupportsCompilation);

            if (!project.TryGetCompilation(out var compilation))
            {
                // WORKAROUND:
                // perf check metadata reference using newly created empty compilation with only metadata references.
                compilation = project.LanguageServices.CompilationFactory!.CreateCompilation(
                    project.AssemblyName, project.CompilationOptions!);

                compilation = compilation.AddReferences(project.MetadataReferences);
            }

            foreach (var reference in project.MetadataReferences)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol symbol &&
                    symbol.Name == assemblyName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
